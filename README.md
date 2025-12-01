# **Wi-Fi Manager WPf**
A modern, fully-localized Wi-Fi management application built in C# / .NET WPF, featuring real-time scanning, profile handling, connection workflows, and a custom animated UI.

## **Features**
* **Real-time Wi-Fi scanning**
    * Updates every 2 seconds using a DispatcherTimer
    * Shows SSID, signal quality, authentication type, and saved profile status
* **Connect to networks, supporting:**
    * Open networks
    * Secured networks (password prompt with custom on-screen keyboard)
    * Known networks (auto-connect using existing profiles)
    * Password entry dialog with Caps Lock, Symbols, and live feedback
* **Disconnect from networks**
    * Disconnect button appears dynamically based on connection state
    * "Disconnect only" or "Disconnect + Forget profile"
    * Localized dialog texts using `AppLanguage`
* **Auto-connect known networks**
    * Finds strongest known profile when Wi-Fi is on
    * Automatically attempts connection
* **Full Localization system, supporting:**
    * US (English)
    * GR (Greek)
    * IT (Italian)
    * Language strings are stored in a structured list with indices and accessed via:
      `AppLanguage.T(AppLanguage.SomeKey);`
      Dynamic parts (e.g SSID) are handled using `String.Format`.
* **Modern UI/UX**
    * Custom button styles (DangerButton, PrimaryButton, etc.)
    * Dynamic Wi-Fi icons using `SignalStrengthToWifiIconResourceConverter`
    * Smooth animations and clean card layout
    * Password reveal / hide toggle
    * Custom virtual keyboard
* **Architecture**
```
  /WifiManagerWPF
│
├── MainWindow.xaml + .cs     → Main UI, network list, localization setup
├── NetworkInfo.cs            → Data model for networks
├── AppLanguage.cs            → Localization system
├── SettingsStorage.cs        → Language persistence (SQLite)
├── ConnectToNetwork.xaml.cs  → Password prompt window
├── ForgetNetworkDialog.cs    → Disconnect / Forget dialog
└── Converters/               → Visibility + Icon converters
```
* **Technologies Used**
```
| Component     | Technology                          |

| ------------- | ----------------------------------- |
| UI            | **WPF / XAML**                      |
| Logic         | **C# .NET 8**                     |
| Wi-Fi backend | **ManagedNativeWifi**, **netsh**    |
| Persistence   | **SQLite**                          |
| Icons         | **MahApps.Metro.IconPacks**         |
| Localization  | Custom `AppLanguage` system         |
| UX            | Custom styles, converters, keyboard |
```
* **Installation & Build**
  **Prerequisites**
  * Windows 10 or later
  * .NET 6.0 SDK or higher (8.0 optimal)
  * Visual Studio 2022 (recmommended)

* **Changing Language**
  Language preference is saved in SQLite and loaded on startup
  To change programmatically:
  `AppLanguage.SetLanguage("el-GR");   // Greek`
  
  `AppLanguage.SetLanguage("it-IT");   // Italian`
  
  `AppLanguage.SetLanguage("en");      // English`
* **Known Limitations**
  * WPF `ItemTemplates` do not expose named controls at design time
  * Windows Wi-Fi events are polled every 2 seconds (netsh limitation)
  * Connecting to hidden networks must be done manually

* **Credits**
  
  Developed with care using:
  * C#
  * WPF
  * SQLite
  * ManagedNativeWifi
