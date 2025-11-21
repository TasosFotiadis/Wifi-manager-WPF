using System.Collections.Generic;
using System.Data;

namespace WifiManagerWPF
{
    internal static class AppLanguage
    {
        // --- INDICES ---
        public const int AutoRefreshedAt = 0;
        public const int WifiNA = 1;
        public const int WifiOn = 2;
        public const int WifiOff = 3;
        public const int ConnectNoNetwork = 4;
        public const int DisconnectNoNetwork = 5;
        public const int ConnectingTo = 6;
        public const int TryingToConnectTo = 7;
        public const int ConnectedTo = 8;
        public const int CouldNotConnectTo = 9;
        public const int ConnectingDots = 10;
        public const int Connected = 11;
        public const int ConnectedExcl = 12;
        public const int WrongPasswordStatus = 13;
        public const int WrongPasswordPopup = 14; // Fixed formatting
        public const int ConnectFailedFor = 15;
        public const int DisconnectingFrom = 16;
        public const int DisconnectedAndForgot = 17;
        public const int DisconnectedButNotForgot = 18;
        public const int DisconnectedFrom = 19;
        public const int FailedToDisconnectFrom = 20;
        public const int TogglingWifi = 21;
        public const int WifiStateChanged = 22;
        public const int ToggleFailed = 23;
        public const int NoSuitableInterface = 24;
        public const int ToggleErrorPrefix = 25;
        public const int AutoConnectingTo = 26;
        public const int AutoConnectFailedFor = 27;
        public const int EnterPasswordFor = 28;

        // General UI
        public const int Show = 29;
        public const int Hide = 30;
        public const int Clear = 31;

        // Password Errors
        public const int PasswordWrongShort = 32;
        public const int PasswordWrongBang = 33;

        // Buttons
        public const int ShowPassword = 34;
        public const int HidePassword = 35;
        public const int DisconnectPrompt = 36;
        public const int StatusCouldNotConnect = 37;      // Short status
        public const int SecurityUnknown = 38;            // "Unknown" security
        public const int BtnConnect = 39;                 // Button "Connect"
        public const int BtnForget = 40;                  // Button "Forget"
        public const int ConnectErrorEnterPwd = 41;       // "Could not connect to {0}. Enter password."
        public const int disconnectButton = 42;           // Button "Disconnect"
        public const int disconnectForgetButton = 43;     // Button "Disconnect and Forget
        public const int BtnCardDisconnect = 44;          // Button "Disconnect" of the network card
        public const int DisconnectOptions = 45;          // Label of the ForegetNetworkDialog
        public const int Password = 46;                   // password label

        public static string BtnCardDisconnectLabel => T(BtnCardDisconnect);

        // --- ENGLISH ---
        private static readonly List<string> _en = new()
        {
            "Auto-refreshed at {0}",                                           // 0
            "Wi-Fi N/A",                                                       // 1
            "Wi-Fi ON",                                                        // 2
            "Wi-Fi OFF",                                                       // 3
            "Connect: no network selected.",                                   // 4
            "Disconnect: no network selected.",                                // 5
            "Connecting to {0}...",                                            // 6
            "Trying to connect to {0}...",                                     // 7
            "Connected to {0}.",                                               // 8
            "Could not connect to {0}.",                                       // 9
            "Connecting...",                                                   // 10
            "Connected",                                                       // 11
            "Connected!",                                                      // 12
            "Wrong password",                                                  // 13
            "Wrong password. Please try again.",                               // 14 (Added newline)
            "Connect failed for {0}.",                                         // 15
            "Disconnecting from {0}...",                                       // 16
            "Disconnected and forgot {0}.",                                    // 17
            "Disconnected from {0}, but failed to forget saved profile.",      // 18
            "Disconnected from {0}.",                                          // 19
            "Failed to disconnect from {0}.",                                  // 20
            "Toggling Wi-Fi...",                                               // 21
            "Wi-Fi state changed.",                                            // 22
            "Toggle failed (access denied or operation error).",               // 23
            "No suitable Wi-Fi interface found or hardware radio off.",        // 24
            "Toggle error: {0}",                                               // 25
            "Auto-connecting to {0}...",                                       // 26
            "Auto-connect failed for {0}.",                                    // 27
            "Enter password for {0}.",                                         // 28
            "Show",                                                            // 29
            "Hide",                                                            // 30
            "Clear",                                                           // 31
            "Wrong Password",                                                  // 32
            "Wrong Password!",                                                 // 33
            "Show",                                                            // 34
            "Hide",                                                            // 35
            "Choose how you want to disconnect from \"{0}\"",                  // 36
            "Could not connect",                                               // 37
            "Unknown",                                                         // 38
            "Connect",                                                         // 39                                                               
            "Forget",                                                          // 40
            "Could not connect to {0}. Enter password.",                       // 41
            "Disconnect",                                                      // 42
            "Disconnect and Forget",                                           // 43
            "Disconnect",                                                      // 44
            "Disconnect options",                                              // 45
            "Password"                                                         // 46
        };

        // --- GREEK ---
        private static readonly List<string> _el = new()
        {
            "Αυτόματη ανανέωση στις {0}",                                      // 0
            "Wi-Fi Μ/Δ",                                                       // 1 (Changed N/A to Μ/Δ)
            "Wi-Fi ΕΝΕΡΓΟ",                                                    // 2
            "Wi-Fi ΑΝΕΝΕΡΓΟ",                                                  // 3
            "Σύνδεση: δεν έχει επιλεγεί δίκτυο.",                              // 4
            "Αποσύνδεση: δεν έχει επιλεγεί δίκτυο.",                           // 5
            "Σύνδεση σε {0}...",                                               // 6
            "Προσπάθεια σύνδεσης με {0}...",                                   // 7
            "Συνδεδεμένο με {0}.",                                             // 8
            "Δεν ήταν δυνατή η σύνδεση με {0}.",                               // 9
            "Σύνδεση...",                                                      // 10
            "Συνδεδεμένο",                                                     // 11
            "Συνδεδεμένο!",                                                    // 12
            "Λάθος κωδικός",                                                   // 13
            "Λάθος κωδικός. Προσπαθήστε ξανά.",                                // 14 (Added newline)
            "Αποτυχία σύνδεσης με {0}.",                                       // 15
            "Αποσύνδεση από {0}...",                                           // 16
            "Αποσυνδέθηκε και διαγράφηκε το {0}.",                             // 17 (Changed "forgot" to "deleted")
            "Αποσύνδεση από {0}, αλλά απέτυχε η διαγραφή του προφίλ.",         // 18
            "Αποσυνδέθηκε από {0}.",                                           // 19
            "Αποτυχία αποσύνδεσης από {0}.",                                   // 20
            "Εναλλαγή Wi-Fi...",                                               // 21
            "Η κατάσταση του Wi-Fi άλλαξε.",                                   // 22
            "Η εναλλαγή απέτυχε (μη εξουσιοδοτημένη πρόσβαση ή σφάλμα).",      // 23
            "Δεν βρέθηκε κατάλληλη διεπαφή Wi-Fi ή ο διακόπτης είναι κλειστός.",// 24
            "Σφάλμα εναλλαγής: {0}",                                           // 25
            "Αυτόματη σύνδεση με {0}...",                                      // 26
            "Η αυτόματη σύνδεση απέτυχε για {0}.",                             // 27
            "Εισαγάγετε τον κωδικό πρόσβασης για {0}.",                        // 28
            "Εμφάνιση",                                                        // 29
            "Απόκρυψη",                                                        // 30
            "Εκκαθάριση",                                                      // 31
            "Λάθος κωδικός",                                                   // 32
            "Λάθος κωδικός!",                                                  // 33
            "Εμφάνιση",                                                        // 34
            "Απόκρυψη",                                                        // 35
            "Επιλέξτε πώς θέλετε να αποσυνδεθείτε από \"{0}\"",                // 36
            "Αδυναμία σύνδεσης",                                               // 37
            "Άγνωστο",                                                         // 38
            "Σύνδεση",                                                         // 39
            "Διαγραφή",                                                        // 40 (Using "Delete" context as established earlier)
            "Αδυναμία σύνδεσης στο {0}. Εισαγάγετε κωδικό.",                   // 41
            "Αποσύνδεση",                                                      // 42
            "Αποσύνδεση και Διαγραφή",                                         // 43
            "Αποσύνδεση",                                                      // 44
            "Επιλογές αποσύνδεσης",                                            // 45
            "Κωδικός πρόσβασης"                                                // 46
        };

        // --- ITALIAN ---
        private static readonly List<string> _it = new()
        {
            "Aggiornato autom. alle {0}",                                        // 0
            "Wi-Fi N/D",                                                         // 1
            "Wi-Fi ATTIVO",                                                      // 2
            "Wi-Fi SPENTO",                                                      // 3
            "Connetti: nessuna rete selezionata.",                               // 4
            "Disconnetti: nessuna rete selezionata.",                            // 5
            "Connessione a {0}...",                                              // 6
            "Tentativo di connessione a {0}...",                                 // 7
            "Connesso a {0}.",                                                   // 8
            "Impossibile connettersi a {0}.",                                    // 9
            "Connessione in corso...",                                           // 10
            "Connesso",                                                          // 11
            "Connesso!",                                                         // 12
            "Password errata",                                                   // 13
            "Password errata. Riprova.",                                         // 14
            "Connessione non riuscita per {0}.",                                 // 15
            "Disconnessione da {0}...",                                          // 16
            "Disconnesso e rete {0} rimossa.",                                   // 17
            "Disconnesso da {0}, ma impossibile rimuovere il profilo.",          // 18
            "Disconnesso da {0}.",                                               // 19
            "Impossibile disconnettersi da {0}.",                                // 20
            "Cambio stato Wi-Fi...",                                             // 21 (Changed from Commutazione)
            "Stato Wi-Fi modificato.",                                           // 22
            "Operazione fallita (accesso negato o errore).",                     // 23
            "Nessuna interfaccia Wi-Fi trovata o radio spenta.",                 // 24
            "Errore cambio stato: {0}",                                          // 25
            "Connessione auto a {0}...",                                         // 26
            "Connessione auto fallita per {0}.",                                 // 27
            "Inserire password per {0}.",                                        // 28
            "Mostra",                                                            // 29
            "Nascondi",                                                          // 30
            "Pulisci",                                                           // 31 (Clear text)
            "Password errata",                                                   // 32
            "Password errata!",                                                  // 33
            "Mostra",                                                            // 34
            "Nascondi",                                                          // 35
            "Scegli come disconnetterti da \"{0}\"",                             // 36
            "Impossibile connettersi",                                           // 37
            "Sconosciuto",                                                       // 38
            "Connetti",                                                          // 39
            "Dimentica",                                                         // 40
            "Impossibile connettersi a {0}. Inserire password.",                 // 41
            "Disconnetti",                                                       // 42
            "Disconnetti e Dimentica",                                           // 43
            "Disconnetti",                                                       // 44
            "Opzioni di disconnessione",                                         // 45
            "Password"                                                           // 46
        };

        private static List<string> _current = _en;

        public static void SetLanguage(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                _current = _en;
                return;
            }

            lang = lang.ToLowerInvariant();

            // Covers "el", "el-GR"
            if (lang.StartsWith("el"))
                _current = _el;
            // Covers "it", "it-IT"
            else if (lang.StartsWith("it"))
                _current = _it;
            else
                _current = _en;
        }

        public static string T(int index)
        {
            if (index < 0 || index >= _current.Count) return "ERR_LANG_INDEX";
            return _current[index];
        }
    }
}