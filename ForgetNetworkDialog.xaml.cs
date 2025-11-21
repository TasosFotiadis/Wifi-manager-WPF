using System.Windows;

namespace WifiManagerWPF
{
    public partial class ForgetNetworkDialog : Window
    {
        public bool ForgetProfile { get; private set; }

        public ForgetNetworkDialog(string ssid)
        {
            InitializeComponent();
            if (disconnectButton == null)
            {
                TxtMessage.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectPrompt), ssid);
            }
            TxtTitle.Text = string.Format(AppLanguage.T(AppLanguage.DisconnectOptions));
            if (disconnectButton != null)
            {
                disconnectButton.Content = AppLanguage.T(AppLanguage.disconnectButton);             // ID 42
            }
            if (disconnectForgetButton != null)
            {
                disconnectForgetButton.Content = AppLanguage.T(AppLanguage.disconnectForgetButton); // ID 43
            }
        }

        // DISCONNECT ONLY (no forget)
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            ForgetProfile = false;     // tell caller: just disconnect
            DialogResult = true;
            Close();
        }

        // FORGET + DISCONNECT
        private void Forget_Click(object sender, RoutedEventArgs e)
        {
            ForgetProfile = true;      // tell caller: disconnect + forget profile
            DialogResult = true;
            Close();
        }

        // X button (top-right)
        private void CloseX_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;      // treated as cancel
            Close();
        }

        // Cancel button
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;      // treated as cancel
            Close();
        }
    }
}
