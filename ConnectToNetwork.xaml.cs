using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WifiManagerWPF
{
    public partial class ConnectToNetwork : Window
    {
        private readonly NetworkInfo _network;
        private readonly List<Button> _letterButtons;

        public ConnectToNetwork(NetworkInfo info)
        {
            InitializeComponent();

            _network = info;
            TitleTextBlock.Text = info.Ssid;

            enterPasswordLabel.Text = string.Format(
                AppLanguage.T(AppLanguage.EnterPasswordFor),
                info.Ssid
            );
            passwordLabel.Text = AppLanguage.T(AppLanguage.Password);
            // Ensure buttons have the correct language on startup
            if (BtnConnect != null) BtnConnect.Content = AppLanguage.T(AppLanguage.BtnConnect);         // "Connect"
            if (BtnTogglePwd != null) BtnTogglePwd.Content = AppLanguage.T(AppLanguage.ShowPassword);   // "Show"

            if (this.FindName("BtnClear") is Button btnClear)
            {
                btnClear.Content = AppLanguage.T(AppLanguage.Clear); // "Clear"
            }

            _letterButtons = new List<Button>
            {
                BtnQ, BtnW, BtnE, BtnR, BtnT, BtnY, BtnU, BtnI, BtnO, BtnP,
                BtnA, BtnS, BtnD, BtnF, BtnG, BtnH, BtnJ, BtnK, BtnL,
                BtnZ, BtnX, BtnC, BtnV, BtnB, BtnN, BtnM
            };

            BtnCaps.IsChecked = false;
            UpdateLetterCase(false);

            KeyboardGrid.Visibility = Visibility.Visible;
            BtnConnect.Visibility = Visibility.Visible;
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // user pressed "back" -> just cancel the dialog
            this.DialogResult = false;
            this.Close();
        }


        private void PasswordInput_GotFocus(object sender, RoutedEventArgs e)
        {
            TxtPopupStatus.Text = string.Empty;
            TxtPopupStatus.Visibility = Visibility.Collapsed;
        }


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

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string password = PwdTextBox.Visibility == Visibility.Visible
                ? PwdTextBox.Text
                : PwdBox.Password;

            TxtPopupStatus.Visibility = Visibility.Collapsed;
            TxtPopupStatus.Text = string.Empty;

            // Show loader + disable connect and back buttons
            TitleLoaderIcon.Visibility = Visibility.Visible;
            BtnConnect.IsEnabled = false;
            BtnBack.IsEnabled = false;

            try
            {
                if (Owner is MainWindow main)
                {
                    await main.ConnectFromPopupAsync(_network, password, this);
                }
            }
            finally
            {
                // Hide loader + re-enable Connect and Back buttons
                TitleLoaderIcon.Visibility = Visibility.Collapsed;
                BtnConnect.IsEnabled = true;
                BtnBack.IsEnabled = true;
            }
        }


        // Keyboard Handlers
        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Content is string s && s.Length > 0)
            {
                string toInsert = s;

                if (s.Length == 1 && char.IsLetter(s[0]))
                {
                    bool capsOn = BtnCaps.IsChecked == true;
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
            }
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
                    pb.Password = pb.Password[..^1];
            }
            else
            {
                if (PwdTextBox.Visibility == Visibility.Visible)
                {
                    if (PwdTextBox.Text.Length > 0)
                        PwdTextBox.Text = PwdTextBox.Text[..^1];
                }
                else
                {
                    if (PwdBox.Password.Length > 0)
                        PwdBox.Password = PwdBox.Password[..^1];
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
        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Hide error whenever user changes the password
            TxtPopupStatus.Visibility = Visibility.Collapsed;
            TxtPopupStatus.Text = string.Empty;
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

            // Safely update via Dispatcher if called from background (but events are on UI thread)
            Dispatcher.Invoke(() =>
            {
                BtnNum1.Content = labels[0];
                BtnNum2.Content = labels[1];
                BtnNum3.Content = labels[2];
                BtnNum4.Content = labels[3];
                BtnNum5.Content = labels[4];
                BtnNum6.Content = labels[5];
                BtnNum7.Content = labels[6];
                BtnNum8.Content = labels[7];
                BtnNum9.Content = labels[8];
                BtnNum0.Content = labels[9];

            });
        }

        private void UpdateQRowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowQSymbols : _rowQLabels;

            // Safely update via Dispatcher if called from background (but events are on UI thread)
            Dispatcher.Invoke(() =>
            {
                BtnQ.Content = labels[0];
                BtnW.Content = labels[1];
                BtnE.Content = labels[2];
                BtnR.Content = labels[3];
                BtnT.Content = labels[4];
                BtnY.Content = labels[5];
                BtnU.Content = labels[6];
                BtnI.Content = labels[7];
                BtnO.Content = labels[8];
                BtnP.Content = labels[9];

            });
        }
        private void UpdateARowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowASymbols : _rowALabels;

            // Safely update via Dispatcher if called from background (but events are on UI thread)
            Dispatcher.Invoke(() =>
            {
                BtnA.Content = labels[0];
                BtnS.Content = labels[1];
                BtnD.Content = labels[2];
                BtnF.Content = labels[3];
                BtnG.Content = labels[4];
                BtnH.Content = labels[5];
                BtnJ.Content = labels[6];
                BtnK.Content = labels[7];
                BtnL.Content = labels[8];
            });
        }
        private void UpdateZRowButtons(bool shifted)
        {
            var labels = shifted ? _shiftedRowZSymbols : _rowZLabels;

            // Safely update via Dispatcher if called from background (but events are on UI thread)
            Dispatcher.Invoke(() =>
            {
                BtnZ.Content = labels[0];
                BtnX.Content = labels[1];
                BtnC.Content = labels[2];
                BtnV.Content = labels[3];
                BtnB.Content = labels[4];
                BtnN.Content = labels[5];
                BtnM.Content = labels[6];
            });
        }

        private void Caps_Checked(object sender, RoutedEventArgs e) => UpdateLetterCase(true);
        private void Caps_Unchecked(object sender, RoutedEventArgs e) => UpdateLetterCase(false);

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
            if (_letterButtons == null) return;

            foreach (var btn in _letterButtons.Where(b => b != null))
            {
                string text = btn.Content?.ToString() ?? string.Empty;
                if (text.Length == 0) continue;

                btn.Content = isCaps ? text.ToUpper() : text.ToLower();
            }
        }

    }
}
