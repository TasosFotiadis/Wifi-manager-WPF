using MahApps.Metro.IconPacks;
using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Xml;
using System.Text;

namespace WifiManagerWPF
{
     //--- Class 1.0: Converter for "Disconnect" button appearance
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    // --- CLASS 1.1: CONVERTER ---
    public class SignalStrengthToWifiIconResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int q = 0;
            if (value != null)
                int.TryParse(value.ToString(), out q);

            q = Math.Max(0, Math.Min(100, q));

            string key =
                q <= 25 ? "WifiZeroIcon" :
                q <= 50 ? "WifiLowIcon" :
                q <= 75 ? "WifiHighIcon" :
                          "WifiFullIcon";

            var original = Application.Current.TryFindResource(key) as FrameworkElement
                           ?? Application.Current.TryFindResource("WifiZeroIcon") as FrameworkElement;

            if (original == null)
                return null;

            string xaml = XamlWriter.Save(original);
            using var stringReader = new StringReader(xaml);
            using var xmlReader = XmlReader.Create(stringReader);
            return (FrameworkElement)XamlReader.Load(xmlReader);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    // --- CLASS 2: NETWORK INFO MODEL ---
    public sealed class NetworkInfo : INotifyPropertyChanged
    {
        private string _ssid = string.Empty;
        public string Ssid
        {
            get => _ssid;
            set { if (_ssid != value) { _ssid = value; OnPropertyChanged(); } }
        }

        private int _signalQuality;
        public int SignalQuality
        {
            get => _signalQuality;
            set { if (_signalQuality != value) { _signalQuality = value; OnPropertyChanged(); } }
        }

        private string _security = string.Empty;
        public string Security
        {
            get => _security;
            set { if (_security != value) { _security = value; OnPropertyChanged(); } }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { if (_isConnected != value) { _isConnected = value; OnPropertyChanged(); } }
        }

        private string _connectionStatus = string.Empty;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { if (_connectionStatus != value) { _connectionStatus = value; OnPropertyChanged(); } }
        }

        private bool _hasProfile;
        public bool HasProfile
        {
            get => _hasProfile;
            set { if (_hasProfile != value) { _hasProfile = value; OnPropertyChanged(); } }
        }

        public bool CanDisconnect => IsConnected || HasProfile;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    // --- CLASS 3: MAIN WINDOW ---
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<NetworkInfo> _networks = new();
        private readonly DispatcherTimer _refreshTimer;
        private bool _isRefreshing;
        private List<Button> _letterButtons;
        private bool _isAutoConnecting;
        private bool _suppressAutoConnect;

        // 1. DATABASE LOADER
        private void LoadLanguageFromDatabase()
        {
            try
            {
                var settings = new SettingsStorage();
                string? lang = settings.LoadValueFromDb("LastLanguage");
                AppLanguage.SetLanguage(lang); // Using AppLanguage
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading language: {ex.Message}");
                AppLanguage.SetLanguage("en"); // Fallback
            }
        }

        public MainWindow()
        {
            SQLitePCL.Batteries_V2.Init();
            // 2. Load Language on Startup
            LoadLanguageFromDatabase();
            InitializeComponent();



            _letterButtons = new List<Button>
            {
                BtnQ, BtnW, BtnE, BtnR, BtnT, BtnY, BtnU, BtnI, BtnO, BtnP,
                BtnA, BtnS, BtnD, BtnF, BtnG, BtnH, BtnJ, BtnK, BtnL,
                BtnZ, BtnX, BtnC, BtnV, BtnB, BtnN, BtnM
            };

            LvNetworks.ItemsSource = _networks;

            _ = InitializeWifiToggleAsync();

            UpdateLetterButtons(BtnCaps?.IsChecked == true);

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= RefreshTimer_Tick;
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            try
            {
                await RefreshNetworksAsync();
                // Localized Status
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.AutoRefreshedAt), DateTime.Now.ToString("T"));
            }
            finally
            {
                _refreshTimer.Start();
            }
        }

        private async Task InitializeWifiToggleAsync()
        {
            var state = await Task.Run(() => GetCurrentWifiSoftwareState());
            Dispatcher.Invoke(() =>
            {
                if (state == null)
                {
                    ChkToggleWifi.IsEnabled = false;
                    ChkToggleWifi.IsChecked = false;
                    ChkToggleWifi.Content = AppLanguage.T(AppLanguage.WifiNA);
                }
                else
                {
                    ChkToggleWifi.IsEnabled = true;
                    ChkToggleWifi.IsChecked = state.Value;
                    ChkToggleWifi.Content = state.Value ? AppLanguage.T(AppLanguage.WifiOn) : AppLanguage.T(AppLanguage.WifiOff);
                }
            });
        }

        private static HashSet<string> GetSsidsWithProfiles()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (var n in NativeWifi.EnumerateAvailableNetworks())
                {
                    var ssid = n.Ssid?.ToString();
                    if (!string.IsNullOrWhiteSpace(ssid) &&
                        !string.IsNullOrWhiteSpace(n.ProfileName))
                    {
                        result.Add(ssid);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetSsidsWithProfiles error: " + ex);
            }
            return result;
        }

        private async void NetworkCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (sender is not Border border || border.DataContext is not NetworkInfo info)
                return;

            var dep = e.OriginalSource as DependencyObject;
            while (dep != null)
            {
                if (dep is Button) return;
                dep = VisualTreeHelper.GetParent(dep);
            }

            _refreshTimer.Stop();
            try
            {
                _suppressAutoConnect = false;

                foreach (var n in _networks)
                {
                    if (!n.IsConnected && !ReferenceEquals(n, info))
                        n.ConnectionStatus = string.Empty;
                }

                LvNetworks.SelectedItem = info;

                bool isOpen = !string.IsNullOrEmpty(info.Security) &&
                              info.Security.IndexOf("open", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasProfile = info.HasProfile;

                // ===== OPEN networks =====
                if (isOpen)
                {
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

                    bool started = false;

                    var okManagedOpen = await TryConnectWithManagedNativeWifi(info.Ssid);
                    started = okManagedOpen;

                    if (!started)
                    {
                        var netshOkOpen = await Task.Run(() => ConnectWithExistingProfileViaNetsh(info.Ssid));
                        started = netshOkOpen;
                    }

                    if (!started)
                    {
                        var openOk = await Task.Run(() => ConnectOpenNetworkViaNetsh(info.Ssid));
                        started = openOk;
                    }

                    if (!started)
                    {
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.CouldNotConnectTo), info.Ssid);
                        await RefreshNetworksAsync();
                        return;
                    }

                    bool connectedOpen = await WaitForConnectionAsync(info.Ssid, TimeSpan.FromSeconds(6));
                    if (connectedOpen)
                    {
                        info.IsConnected = true;
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);
                    }
                    else
                    {
                        info.IsConnected = false;
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.CouldNotConnectTo), info.Ssid);
                    }

                    await RefreshNetworksAsync();
                    return;
                }

                // ===== SECURED networks =====

                // secured + NO profile
                if (!hasProfile)
                {
                    info.ConnectionStatus = string.Empty;
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.EnterPasswordFor), info.Ssid);

                    var win = new ConnectToNetwork(info) { Owner = this };

                    win.ShowDialog();
                    await RefreshNetworksAsync();
                    return;
                }

                // secured + saved profile
                info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

                bool okSecured = await TryConnectWithManagedNativeWifi(info.Ssid);

                if (okSecured)
                {
                    info.IsConnected = true;
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);
                    await RefreshNetworksAsync();
                    return;
                }

                // saved profile but wrong password
                info.IsConnected = false;
                info.ConnectionStatus = AppLanguage.T(AppLanguage.WrongPasswordStatus);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.EnterPasswordFor), info.Ssid);

                var popup = new ConnectToNetwork(info) { Owner = this };

                popup.ShowDialog();
                await RefreshNetworksAsync();
            }
            finally
            {
                _refreshTimer.Start();
            }
        }

        private async void CardConnect_Click(object sender, RoutedEventArgs e)
        {
            NetworkInfo? info = null;
            if (sender is FrameworkElement fe)
            {
                info = fe.DataContext as NetworkInfo;
                if (info is null && fe is Button b)
                    info = b.CommandParameter as NetworkInfo;
            }

            if (info is null)
            {
                TxtStatus.Text = AppLanguage.T(AppLanguage.ConnectNoNetwork);
                Debug.WriteLine("CardConnect_Click: NetworkInfo not found.");
                return;
            }

            LvNetworks.SelectedItem = info;
            string password = PwdTextBox.Visibility == Visibility.Visible ? PwdTextBox.Text : PwdBox.Password;
            if (PwdTextBox.Visibility == Visibility.Visible)
            {
                PwdTextBox.Focus();
                PwdTextBox.SelectionStart = PwdTextBox.Text.Length;
            }
            else
            {
                PwdBox.Focus();
            }

            info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
            TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

            if (sender is Button btn) btn.IsEnabled = false;

            _refreshTimer.Stop();

            foreach (var n in _networks)
            {
                if (!n.IsConnected && !ReferenceEquals(n, info))
                    n.ConnectionStatus = string.Empty;
            }
            try
            {
                bool attemptStarted = false;

                if (!string.IsNullOrEmpty(password))
                {
                    var netshOk = await Task.Run(() => ConnectToNetworkViaNetsh(info.Ssid, password));
                    attemptStarted = netshOk;
                }

                if (!attemptStarted)
                {
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.TryingToConnectTo), info.Ssid);
                }

                info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

                bool connected = await WaitForConnectionAsync(info.Ssid, TimeSpan.FromSeconds(10));

                if (connected)
                {
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectedExcl);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);
                }
                else
                {
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.WrongPasswordStatus);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.CouldNotConnectTo), info.Ssid);
                }

                await RefreshNetworksAsync();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Connect error: {ex.Message}";
                info.ConnectionStatus = AppLanguage.T(AppLanguage.PasswordWrongBang);
                Debug.WriteLine(ex);
            }
            finally
            {
                if (sender is Button b2) b2.IsEnabled = true;
                _refreshTimer.Start();
            }
        }

        private async void CardDisconnect_Click(object sender, RoutedEventArgs e)
        {
            NetworkInfo? info = null;
            if (sender is FrameworkElement fe)
            {
                info = fe.DataContext as NetworkInfo;
                if (info is null && fe is Button b)
                    info = b.CommandParameter as NetworkInfo;
            }

            if (info is null)
            {
                TxtStatus.Text = AppLanguage.T(AppLanguage.DisconnectNoNetwork);
                return;
            }

            var dlg = new ForgetNetworkDialog(info.Ssid) { Owner = this };
            var result = dlg.ShowDialog();

            if (result != true) return;

            bool forget = dlg.ForgetProfile;

            TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectingFrom), info.Ssid);

            try
            {
                var disconnected = await Task.Run(() => DisconnectFromWifi());
                bool forgot = false;

                if (forget)
                {
                    forgot = await Task.Run(() => ForgetWifiProfile(info.Ssid));
                }

                if (disconnected)
                {
                    _suppressAutoConnect = true;
                    if (forgot)
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectedAndForgot), info.Ssid);
                    else if (forget)
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectedButNotForgot), info.Ssid);
                    else
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectedFrom), info.Ssid);
                }
                else
                {
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.FailedToDisconnectFrom), info.Ssid);
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Disconnect error: {ex.Message}";
            }

            await RefreshNetworksAsync();
        }

        private bool ForgetWifiProfile(string ssid)
        {
            try
            {
                var (exit, output) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan delete profile name=""{ssid}"""
                );
                return exit == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ForgetWifiProfile error: " + ex);
                return false;
            }
        }

        private bool DisconnectFromWifi()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan disconnect",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                p.WaitForExit();

                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> TryConnectWithManagedNativeWifi(string ssid)
        {
            var availableNetwork = NativeWifi.EnumerateAvailableNetworks()
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.ProfileName) &&
                    string.Equals(x.Ssid?.ToString(), ssid, StringComparison.Ordinal))
                .OrderByDescending(x => x.SignalQuality)
                .FirstOrDefault();

            if (availableNetwork is not null && !string.IsNullOrWhiteSpace(availableNetwork.ProfileName))
            {
                try
                {
                    return await NativeWifi.ConnectNetworkAsync(
                        interfaceId: availableNetwork.InterfaceInfo.Id,
                        profileName: availableNetwork.ProfileName,
                        bssType: availableNetwork.BssType,
                        timeout: TimeSpan.FromSeconds(2));
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool ConnectOpenNetworkViaNetsh(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                throw new ArgumentException("SSID must not be empty.", nameof(ssid));

            string ssidXml = SecurityElement.Escape(ssid);
            string ssidCli = ssid.Replace("\"", string.Empty);
            string tmp = null;

            try
            {
                string profileXml =
                    $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{ssidXml}</name>
  <SSIDConfig>
    <SSID>
      <name>{ssidXml}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>open</authentication>
        <encryption>none</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
    </security>
  </MSM>
</WLANProfile>";

                tmp = Path.Combine(Path.GetTempPath(), $"wlan-open-{Guid.NewGuid():N}.xml");
                File.WriteAllText(tmp, profileXml, Encoding.UTF8);

                var (showCode, _) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan show profile name=""{ssidCli}""");

                if (showCode != 0)
                {
                    var (addCode, addOut) = RunProcessGetOutput(
                        "netsh",
                        $@"wlan add profile filename=""{tmp}"" user=current");

                    if (addCode != 0) return false;
                }

                var (connCode, connOut) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan connect name=""{ssidCli}"" ssid=""{ssidCli}""");

                if (connCode != 0) return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConnectOpenNetworkViaNetsh failed: " + ex);
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
        }

        private static bool ConnectToNetworkViaNetsh(string ssid, string password)
        {
            if (string.IsNullOrWhiteSpace(ssid)) return false;

            string ssidXml = SecurityElement.Escape(ssid);
            string rawPassword = password ?? string.Empty;
            string tmp = null;

            try
            {
                string profileXml =
                    $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{ssidXml}</name>
  <SSIDConfig>
    <SSID>
      <name>{ssidXml}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>WPA2PSK</authentication>
        <encryption>AES</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
      <sharedKey>
        <keyType>passPhrase</keyType>
        <protected>false</protected>
        <keyMaterial>{rawPassword}</keyMaterial>
      </sharedKey>
    </security>
  </MSM>
</WLANProfile>";

                tmp = Path.Combine(Path.GetTempPath(), $"wlan-{Guid.NewGuid():N}.xml");
                File.WriteAllText(tmp, profileXml, Encoding.UTF8);

                var (addCode, addOut) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan add profile filename=""{tmp}"" user=current");

                if (addCode != 0) return false;

                var (connCode, connOut) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan connect name=""{ssid}"" ssid=""{ssid}""");

                if (connCode != 0) return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConnectToNetworkViaNetsh failed: " + ex);
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
        }

        private static bool ConnectWithExistingProfileViaNetsh(string ssid)
        {
            try
            {
                var (code, _) = RunProcessGetOutput(
                    "netsh",
                    $@"wlan connect name=""{ssid}"" ssid=""{ssid}"""
                );
                return code == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConnectWithExistingProfileViaNetsh failed: " + ex);
                return false;
            }
        }

        private static (int ExitCode, string StdOut) RunProcessGetOutput(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return (-1, string.Empty);
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return (p.ExitCode, string.IsNullOrEmpty(stderr) ? stdout : stdout + Environment.NewLine + stderr);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RunProcessGetOutput failed: {ex}");
                return (-1, ex.Message);
            }
        }

        private async void ToggleWifi_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as CheckBox ?? ChkToggleWifi;
            try
            {
                btn.IsEnabled = false;
                TxtStatus.Text = AppLanguage.T(AppLanguage.TogglingWifi);

                var toggleResult = await Task.Run(() => ToggleWifiInternal());

                if (toggleResult == true)
                {
                    TxtStatus.Text = AppLanguage.T(AppLanguage.WifiStateChanged);
                }
                else if (toggleResult == false)
                {
                    TxtStatus.Text = AppLanguage.T(AppLanguage.ToggleFailed);
                }
                else
                {
                    TxtStatus.Text = AppLanguage.T(AppLanguage.NoSuitableInterface);
                }

                var state = await Task.Run(() => GetCurrentWifiSoftwareState());
                Dispatcher.Invoke(() =>
                {
                    if (state == null)
                    {
                        ChkToggleWifi.IsChecked = false;
                        ChkToggleWifi.Content = AppLanguage.T(AppLanguage.WifiNA);
                    }
                    else
                    {
                        ChkToggleWifi.IsChecked = state.Value;
                        ChkToggleWifi.Content = state.Value ? AppLanguage.T(AppLanguage.WifiOn) : AppLanguage.T(AppLanguage.WifiOff);
                    }
                });
            }
            catch (Exception ex)
            {
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ToggleErrorPrefix), ex.Message);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private static bool? GetCurrentWifiSoftwareState()
        {
            var iface = NativeWifi.EnumerateInterfaces()
                .FirstOrDefault(x =>
                {
                    var radioState = NativeWifi.GetRadio(x.Id)?.RadioStates.FirstOrDefault();
                    if (radioState is null) return false;
                    if (!radioState.IsHardwareOn) return false;
                    return true;
                });

            if (iface is null) return null;

            var state = NativeWifi.GetRadio(iface.Id)?.RadioStates.FirstOrDefault();
            if (state is null) return null;
            return state.IsSoftwareOn;
        }

        private static bool? ToggleWifiInternal()
        {
            var targetInterface = NativeWifi.EnumerateInterfaces()
                .FirstOrDefault(x =>
                {
                    var radioState = NativeWifi.GetRadio(x.Id)?.RadioStates.FirstOrDefault();
                    if (radioState is null) return false;
                    if (!radioState.IsHardwareOn) return false;
                    return true;
                });

            if (targetInterface is null) return null;

            var currentRadioState = NativeWifi.GetRadio(targetInterface.Id)?.RadioStates.FirstOrDefault();
            if (currentRadioState is null) return null;

            try
            {
                if (currentRadioState.IsSoftwareOn)
                    return NativeWifi.TurnOffRadio(targetInterface.Id);
                else
                    return NativeWifi.TurnOnRadio(targetInterface.Id);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task RefreshNetworksAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                var results = await Task.Run(() => ScanWithNetsh());
                var connected = await Task.Run(() => GetConnectedSsid());

                NetworkInfo? autoCandidate = null;
                bool shouldAutoConnect = false;

                Dispatcher.Invoke(() =>
                {
                    var oldStatus = _networks.ToDictionary(n => n.Ssid, n => n.ConnectionStatus);

                    _networks.Clear();
                    foreach (var r in results)
                    {
                        r.IsConnected = !string.IsNullOrEmpty(connected) &&
                                        string.Equals(r.Ssid, connected, StringComparison.Ordinal);

                        if (r.IsConnected)
                        {
                            r.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                        }
                        else if (oldStatus.TryGetValue(r.Ssid, out var s) && !string.IsNullOrEmpty(s)
                                 && s != AppLanguage.T(AppLanguage.Connected))
                        {
                            r.ConnectionStatus = s;
                        }
                        else
                        {
                            r.ConnectionStatus = string.Empty;
                        }

                        _networks.Add(r);
                    }

                    if (!string.IsNullOrEmpty(connected))
                    {
                        MainTxtConnectedStatus.Text = AppLanguage.T(AppLanguage.Connected);
                        MainTxtConnectedStatus.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MainTxtConnectedStatus.Visibility = Visibility.Collapsed;
                    }

                    bool anyConnected = _networks.Any(n => n.IsConnected);
                    bool anyConnecting = _networks.Any(n =>
                        string.Equals(n.ConnectionStatus, AppLanguage.T(AppLanguage.ConnectingDots), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(n.ConnectionStatus, AppLanguage.T(AppLanguage.ConnectedExcl), StringComparison.OrdinalIgnoreCase));
                    bool allStatusEmpty = _networks.All(n => string.IsNullOrEmpty(n.ConnectionStatus));

                    if (!_isAutoConnecting && !anyConnected && allStatusEmpty && !anyConnecting && !_suppressAutoConnect)
                    {
                        autoCandidate = _networks
                            .Where(n => n.HasProfile)
                            .OrderByDescending(n => n.SignalQuality)
                            .FirstOrDefault();

                        shouldAutoConnect = autoCandidate != null;
                    }
                });

                if (shouldAutoConnect && autoCandidate != null)
                {
                    await AutoConnectKnownNetworkAsync(autoCandidate);
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async Task<bool> WaitForConnectionAsync(string targetSsid, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < timeout)
            {
                var (ssid, state) = await Task.Run(GetCurrentWifiStatus);
                var stateLower = state?.ToLowerInvariant();

                if (stateLower == "connected")
                {
                    if (!string.IsNullOrEmpty(ssid) &&
                        string.Equals(ssid, targetSsid, StringComparison.Ordinal))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (stateLower == "disconnected") return false;

                await Task.Delay(1000);
            }
            return false;
        }

        private async Task ManualConnectKnownNetworkAsync(NetworkInfo info)
        {
            if (info == null) return;

            foreach (var n in _networks)
            {
                if (!n.IsConnected && !ReferenceEquals(n, info))
                    n.ConnectionStatus = string.Empty;
            }

            info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
            TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

            bool attemptStarted = false;

            var okManaged = await TryConnectWithManagedNativeWifi(info.Ssid);
            attemptStarted = okManaged;

            if (!attemptStarted)
            {
                var netshOk = await Task.Run(() => ConnectWithExistingProfileViaNetsh(info.Ssid));
                attemptStarted = netshOk;
            }

            if (!attemptStarted)
            {
                info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectFailedFor), info.Ssid);
                await RefreshNetworksAsync();
                return;
            }

            bool connected = await WaitForConnectionAsync(info.Ssid, TimeSpan.FromSeconds(15));

            if (connected)
            {
                info.IsConnected = true;
                info.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);
            }
            else
            {
                info.IsConnected = false;
                info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.CouldNotConnectTo), info.Ssid);
            }

            await RefreshNetworksAsync();
        }

        private async Task AutoConnectKnownNetworkAsync(NetworkInfo info)
        {
            if (info == null) return;

            if (_isAutoConnecting) return;

            _isAutoConnecting = true;
            _refreshTimer.Stop();

            try
            {
                Dispatcher.Invoke(() =>
                {
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.AutoConnectingTo), info.Ssid);
                });

                bool attemptStarted = false;

                var okManaged = await TryConnectWithManagedNativeWifi(info.Ssid);
                attemptStarted = okManaged;

                if (!attemptStarted)
                {
                    var netshOk = await Task.Run(() => ConnectWithExistingProfileViaNetsh(info.Ssid));
                    attemptStarted = netshOk;
                }

                if (!attemptStarted)
                {
                    Dispatcher.Invoke(() =>
                    {
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.AutoConnectFailedFor), info.Ssid);
                    });
                    return;
                }

                bool connected = await WaitForConnectionAsync(info.Ssid, TimeSpan.FromSeconds(20));

                Dispatcher.Invoke(() =>
                {
                    if (connected)
                    {
                        info.IsConnected = true;
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);
                    }
                    else
                    {
                        info.IsConnected = false;
                        info.ConnectionStatus = AppLanguage.T(AppLanguage.StatusCouldNotConnect);
                        TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.AutoConnectFailedFor), info.Ssid);
                    }
                });
            }
            finally
            {
                _isAutoConnecting = false;
                _refreshTimer.Start();
            }
        }

        private static (string? Ssid, string? State) GetCurrentWifiStatus()
        {
            var (exit, output) = RunProcessGetOutput("netsh", "wlan show interfaces");
            if (exit != 0 || string.IsNullOrEmpty(output))
                return (null, null);

            string? ssid = null;
            string? state = null;

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("State", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(":"))
                {
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                        state = parts[1].Trim();
                }

                if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(":"))
                {
                    if (trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                        ssid = parts[1].Trim();
                }
            }

            return (ssid, state);
        }

        private static string? GetConnectedSsid()
        {
            var (ssid, state) = GetCurrentWifiStatus();
            if (string.IsNullOrEmpty(state))
                return null;

            if (state.Equals("connected", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(ssid))
            {
                return ssid;
            }
            return null;
        }

        private static List<NetworkInfo> ScanWithNetsh()
        {
            var list = new List<NetworkInfo>();
            var ssidsWithProfiles = GetSsidsWithProfiles();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show networks mode=bssid",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return list;

                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToArray();

                NetworkInfo? current = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("SSID ", StringComparison.OrdinalIgnoreCase) && line.Contains(":"))
                    {
                        var idx = line.IndexOf(":", StringComparison.Ordinal);
                        var ssid = idx >= 0 ? line[(idx + 1)..].Trim() : string.Empty;

                        if (!string.IsNullOrEmpty(ssid))
                        {
                            current = new NetworkInfo
                            {
                                Ssid = ssid,
                                HasProfile = ssidsWithProfiles.Contains(ssid)
                            };
                            list.Add(current);
                        }
                        else
                        {
                            current = null;
                        }
                    }
                    else if (current is not null)
                    {
                        if (line.StartsWith("Signal", StringComparison.OrdinalIgnoreCase) && line.Contains(":"))
                        {
                            var idx = line.IndexOf(":", StringComparison.Ordinal);
                            var sigText = line[(idx + 1)..].Trim().TrimEnd('%');
                            if (int.TryParse(sigText, out var sigVal))
                                current.SignalQuality = sigVal;
                        }
                        else if ((line.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase) ||
                                  line.StartsWith("Auth", StringComparison.OrdinalIgnoreCase)) && line.Contains(":"))
                        {
                            var idx = line.IndexOf(":", StringComparison.Ordinal);
                            var auth = line[(idx + 1)..].Trim();
                            current.Security = string.IsNullOrEmpty(auth) ? AppLanguage.T(AppLanguage.SecurityUnknown) : auth;
                        }
                    }
                }
            }
            catch
            {
            }

            return list
                .GroupBy(n => n.Ssid)
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.SignalQuality).First();
                    if (g.Any(x => x.HasProfile))
                        best.HasProfile = true;
                    return best;
                })
                .Where(n => !string.IsNullOrWhiteSpace(n.Ssid))
                .ToList();
        }

        private void ChkToggleWifi_Click(object sender, RoutedEventArgs e)
        {
            bool isOn = ChkToggleWifi.IsChecked == true;
            ChkToggleWifi.Content = isOn ? AppLanguage.T(AppLanguage.WifiOn) : AppLanguage.T(AppLanguage.WifiOff);
            LvNetworks.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            KeyboardGrid.IsEnabled = isOn;
        }

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Content is string s && s.Length > 0)
            {
                string toInsert = s;
                if (s.Length == 1 && char.IsLetter(s[0]))
                {
                    bool capsOn = (BtnCaps != null && BtnCaps.IsChecked == true);
                    toInsert = capsOn ? s.ToUpperInvariant() : s.ToLowerInvariant();
                }

                var focused = Keyboard.FocusedElement;
                if (focused is TextBox tb)
                {
                    int selStart = tb.SelectionStart;
                    tb.Text = tb.Text.Insert(selStart, toInsert);
                    tb.SelectionStart = selStart + toInsert.Length;
                }
                else if (focused is PasswordBox pb)
                {
                    pb.Password += toInsert;
                }
                else
                {
                    if (PwdTextBox.Visibility == Visibility.Visible)
                        PwdTextBox.Text += toInsert;
                    else
                        PwdBox.Password += toInsert;
                }

                if (BtnSymbols is not null && BtnSymbols.IsChecked == true && b.Name?.StartsWith("BtnNum") == true)
                {
                    BtnSymbols.IsChecked = false;
                }
            }
        }

        private void Caps_Checked(object sender, RoutedEventArgs e) => UpdateLetterButtons(true);
        private void Caps_Unchecked(object sender, RoutedEventArgs e) => UpdateLetterButtons(false);

        private void Symbols_Checked(object sender, RoutedEventArgs e)
        {
            BtnSymbols.Content = "ABC";
            UpdateNumberButtons(shifted: true);
            UpdateARowButtons(shifted: true);
            UpdateQRowButtons(shifted: true);
            UpdateZRowButtons(shifted: true);
        }

        private void Symbols_Unchecked(object sender, RoutedEventArgs e)
        {
            BtnSymbols.Content = "!@#";
            UpdateNumberButtons(shifted: false);
            UpdateARowButtons(shifted: false);
            UpdateQRowButtons(shifted: false);
            UpdateZRowButtons(shifted: false);

            bool useCaps = BtnCaps.IsChecked == true;
            UpdateLetterCase(useCaps);
        }

        private void UpdateLetterCase(bool isCaps)
        {
            foreach (var btn in _letterButtons)
            {
                string text = btn.Content.ToString();
                btn.Content = isCaps ? text.ToUpper() : text.ToLower();
            }
        }

        private readonly string[] _numberLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private readonly string[] _rowQLabels = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        private readonly string[] _rowALabels = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        private readonly string[] _rowZLabels = { "Z", "X", "C", "V", "B", "N", "M" };
        private readonly string[] _shiftedNumberLabels = { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" };
        private readonly string[] _shiftedRowQSymbols = { "_", "-", "+", "=", "~", "`", "[", "]", "{", "}" };
        private readonly string[] _shiftedRowASymbols = { "\\", "|", ";", ":", "'", "\"", ",", ".", "?" };
        private readonly string[] _shiftedRowZSymbols = { "<", ">", "/", "?", "!", "_", "-" };

        private void UpdateNumberButtons(bool shifted)
        {
            var labels = shifted ? _shiftedNumberLabels : _numberLabels;
            Dispatcher.Invoke(() =>
            {
                BtnNum1.Content = labels[0]; BtnNum2.Content = labels[1]; BtnNum3.Content = labels[2];
                BtnNum4.Content = labels[3]; BtnNum5.Content = labels[4]; BtnNum6.Content = labels[5];
                BtnNum7.Content = labels[6]; BtnNum8.Content = labels[7]; BtnNum9.Content = labels[8];
                BtnNum0.Content = labels[9];
            });
        }
        private void UpdateQRowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowQSymbols : _rowQLabels;
            Dispatcher.Invoke(() =>
            {
                BtnQ.Content = labels[0]; BtnW.Content = labels[1]; BtnE.Content = labels[2];
                BtnR.Content = labels[3]; BtnT.Content = labels[4]; BtnY.Content = labels[5];
                BtnU.Content = labels[6]; BtnI.Content = labels[7]; BtnO.Content = labels[8];
                BtnP.Content = labels[9];
            });
        }
        private void UpdateARowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowASymbols : _rowALabels;
            Dispatcher.Invoke(() =>
            {
                BtnA.Content = labels[0]; BtnS.Content = labels[1]; BtnD.Content = labels[2];
                BtnF.Content = labels[3]; BtnG.Content = labels[4]; BtnH.Content = labels[5];
                BtnJ.Content = labels[6]; BtnK.Content = labels[7]; BtnL.Content = labels[8];
            });
        }
        private void UpdateZRowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowZSymbols : _rowZLabels;
            Dispatcher.Invoke(() =>
            {
                BtnZ.Content = labels[0]; BtnX.Content = labels[1]; BtnC.Content = labels[2];
                BtnV.Content = labels[3]; BtnB.Content = labels[4]; BtnN.Content = labels[5];
                BtnM.Content = labels[6];
            });
        }

        private void UpdateLetterButtons(bool upper)
        {
            if (KeyboardGrid == null) return;
            Dispatcher.Invoke(() =>
            {
                foreach (var btn in KeyboardGrid.Children.OfType<Button>())
                {
                    if (btn.Content is string txt && txt.Length == 1 && char.IsLetter(txt[0]))
                    {
                        btn.Content = upper ? txt.ToUpperInvariant() : txt.ToLowerInvariant();
                    }
                }
            });
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            var focused = Keyboard.FocusedElement;
            if (focused is TextBox tb)
            {
                int selStart = tb.SelectionStart;
                if (selStart > 0)
                {
                    tb.Text = tb.Text.Remove(selStart - 1, 1);
                    tb.SelectionStart = selStart - 1;
                }
            }
            else if (focused is PasswordBox pb)
            {
                if (pb.Password.Length > 0)
                    pb.Password = pb.Password.Substring(0, pb.Password.Length - 1);
            }
            else
            {
                if (PwdTextBox.Visibility == Visibility.Visible)
                {
                    if (PwdTextBox.Text.Length > 0)
                        PwdTextBox.Text = PwdTextBox.Text.Substring(0, PwdTextBox.Text.Length - 1);
                }
                else
                {
                    if (PwdBox.Password.Length > 0)
                        PwdBox.Password = PwdBox.Password.Substring(0, PwdBox.Password.Length - 1);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var focused = Keyboard.FocusedElement;
            if (focused is TextBox tb)
                tb.Clear();
            else if (focused is PasswordBox pb)
                pb.Clear();
            else
            {
                if (PwdTextBox.Visibility == Visibility.Visible)
                    PwdTextBox.Clear();
                else
                    PwdBox.Clear();
            }
        }

        public async Task ConnectFromPopupAsync(NetworkInfo info, string password, ConnectToNetwork popup)
        {
            if (info == null || popup == null) return;
            if (!popup.IsVisible) return;

            LvNetworks.SelectedItem = info;

            info.ConnectionStatus = AppLanguage.T(AppLanguage.ConnectingDots);
            TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectingTo), info.Ssid);

            _refreshTimer.Stop();

            foreach (var n in _networks)
            {
                if (!n.IsConnected && !ReferenceEquals(n, info))
                    n.ConnectionStatus = string.Empty;
            }

            try
            {
                bool attemptStarted = false;

                if (!string.IsNullOrEmpty(password))
                {
                    var netshOk = await Task.Run(() => ConnectToNetworkViaNetsh(info.Ssid, password));
                    attemptStarted = netshOk;
                }

                if (!popup.IsVisible) return;

                if (!attemptStarted)
                {
                    popup.TxtPopupStatus.Foreground = (Brush)popup.FindResource("DangerBrush");
                    popup.TxtPopupStatus.Text = AppLanguage.T(AppLanguage.WrongPasswordPopup);
                    popup.TxtPopupStatus.Visibility = Visibility.Visible;

                    info.ConnectionStatus = AppLanguage.T(AppLanguage.PasswordWrongBang);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectFailedFor), info.Ssid);

                    await RefreshNetworksAsync();
                    return;
                }

                bool connected = await WaitForConnectionAsync(info.Ssid, TimeSpan.FromSeconds(10));

                if (!popup.IsVisible) return;

                if (connected)
                {
                    popup.TxtPopupStatus.Visibility = Visibility.Collapsed;
                    info.IsConnected = true;
                    info.ConnectionStatus = AppLanguage.T(AppLanguage.Connected);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.ConnectedTo), info.Ssid);

                    await RefreshNetworksAsync();

                    if (!popup.IsVisible) return;
                    await Task.Delay(800);
                    if (!popup.IsVisible) return;

                    popup.DialogResult = true;
                    popup.Close();
                }
                else
                {
                    popup.TxtPopupStatus.Foreground = (Brush)popup.FindResource("DangerBrush");
                    popup.TxtPopupStatus.Text = AppLanguage.T(AppLanguage.WrongPasswordPopup);
                    popup.TxtPopupStatus.Visibility = Visibility.Visible;

                    info.ConnectionStatus = AppLanguage.T(AppLanguage.WrongPasswordStatus);
                    TxtStatus.Text = string.Format(AppLanguage.T(AppLanguage.CouldNotConnectTo), info.Ssid);
                    MainTxtConnectedStatus.Visibility = Visibility.Collapsed;

                    await RefreshNetworksAsync();
                }
            }
            finally
            {
                _refreshTimer.Start();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (PwdTextBox.Visibility == Visibility.Visible)
            {
                PwdBox.Password = PwdTextBox.Text;
                PwdTextBox.Visibility = Visibility.Collapsed;
                PwdBox.Visibility = Visibility.Visible;
                BtnTogglePwd.Content = AppLanguage.T(AppLanguage.ShowPassword);
                PwdBox.Focus();
            }
            else
            {
                PwdTextBox.Text = PwdBox.Password;
                PwdTextBox.Visibility = Visibility.Visible;
                PwdBox.Visibility = Visibility.Collapsed;
                BtnTogglePwd.Content = AppLanguage.T(AppLanguage.HidePassword);
                PwdTextBox.Focus();
                PwdTextBox.SelectionStart = PwdTextBox.Text.Length;
            }
        }
    }
}