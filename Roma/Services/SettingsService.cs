using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Roma.Services
{
    public class SettingsService : INotifyPropertyChanged
    {
        private static ApplicationDataContainer? LocalSettings
        {
            get
            {
                try
                {
                    return ApplicationData.Current?.LocalSettings;
                }
                catch
                {
                    return null;
                }
            }
        }

        private bool _autoCloseOnConnect;
        private bool _hideEmptyServers;
        private string _selectedLanguage = "ALL";
        private string _appLanguage = "en-US";
        private string _customRageMpPath = string.Empty;
        private bool _reduceAnimations = false;
        private string _themeMode = "Dark";
        private string _accentColor = "#FFFFFF";
        private string _connectionMethod = "Rage"; // Default to Rage method
        private string _serverListSource = "Community"; // Default to Community

        public SettingsService()
        {
            // Load saved settings
            LoadSettings();
        }

        private static void SaveSetting(string key, object value)
        {
            try
            {
                if (LocalSettings != null)
                {
                    LocalSettings.Values[key] = value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save setting {key}: {ex.Message}");
            }
        }

        public bool AutoCloseOnConnect
        {
            get => _autoCloseOnConnect;
            set
            {
                if (_autoCloseOnConnect != value)
                {
                    _autoCloseOnConnect = value;
                    SaveSetting(nameof(AutoCloseOnConnect), value);
                    OnPropertyChanged();
                }
            }
        }

        public bool HideEmptyServers
        {
            get => _hideEmptyServers;
            set
            {
                if (_hideEmptyServers != value)
                {
                    _hideEmptyServers = value;
                    SaveSetting(nameof(HideEmptyServers), value);
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    SaveSetting(nameof(SelectedLanguage), value);
                    OnPropertyChanged();
                }
            }
        }

        public string AppLanguage
        {
            get => _appLanguage;
            set
            {
                if (_appLanguage != value)
                {
                    _appLanguage = value;
                    SaveSetting(nameof(AppLanguage), value);
                    OnPropertyChanged();
                }
            }
        }

        public string CustomRageMpPath
        {
            get => _customRageMpPath;
            set
            {
                if (_customRageMpPath != value)
                {
                    _customRageMpPath = value;
                    SaveSetting(nameof(CustomRageMpPath), value);
                    OnPropertyChanged();
                }
            }
        }

        public bool ReduceAnimations
        {
            get => _reduceAnimations;
            set
            {
                if (_reduceAnimations != value)
                {
                    _reduceAnimations = value;
                    SaveSetting(nameof(ReduceAnimations), value);
                    OnPropertyChanged();
                }
            }
        }

        public string ThemeMode
        {
            get => _themeMode;
            set
            {
                if (_themeMode != value)
                {
                    _themeMode = value;
                    SaveSetting(nameof(ThemeMode), value);
                    OnPropertyChanged();
                }
            }
        }

        public string AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor != value)
                {
                    _accentColor = value;
                    SaveSetting(nameof(AccentColor), value);
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionMethod
        {
            get => _connectionMethod;
            set
            {
                if (_connectionMethod != value)
                {
                    _connectionMethod = value;
                    SaveSetting(nameof(ConnectionMethod), value);
                    OnPropertyChanged();
                }
            }
        }

        public string ServerListSource
        {
            get => _serverListSource;
            set
            {
                if (_serverListSource != value)
                {
                    _serverListSource = value;
                    SaveSetting(nameof(ServerListSource), value);
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadSettings()
        {
            try
            {
                if (LocalSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LocalSettings is null, using defaults");
                    return;
                }

                if (LocalSettings.Values.TryGetValue(nameof(AutoCloseOnConnect), out var autoClose))
            {
                _autoCloseOnConnect = (bool)autoClose;
            }

            if (LocalSettings.Values.TryGetValue(nameof(HideEmptyServers), out var hideEmpty))
            {
                _hideEmptyServers = (bool)hideEmpty;
            }

            if (LocalSettings.Values.TryGetValue(nameof(SelectedLanguage), out var lang))
            {
                _selectedLanguage = lang?.ToString() ?? "ALL";
            }

            if (LocalSettings.Values.TryGetValue(nameof(AppLanguage), out var appLang))
            {
                _appLanguage = appLang?.ToString() ?? "en-US";
            }

            if (LocalSettings.Values.TryGetValue(nameof(CustomRageMpPath), out var path))
            {
                _customRageMpPath = path?.ToString() ?? string.Empty;
            }

            if (LocalSettings.Values.TryGetValue(nameof(ReduceAnimations), out var reduce))
            {
                _reduceAnimations = (bool)reduce;
            }

            if (LocalSettings.Values.TryGetValue(nameof(ThemeMode), out var theme))
            {
                _themeMode = theme?.ToString() ?? "Dark";
            }

            if (LocalSettings.Values.TryGetValue(nameof(AccentColor), out var accent))
            {
                _accentColor = accent?.ToString() ?? "#FFFFFF";
            }

            if (LocalSettings.Values.TryGetValue(nameof(ConnectionMethod), out var connMethod))
            {
                _connectionMethod = connMethod?.ToString() ?? "Rage";
            }

            if (LocalSettings.Values.TryGetValue(nameof(ServerListSource), out var serverSource))
            {
                _serverListSource = serverSource?.ToString() ?? "Community";
            }
            }
            catch (Exception ex)
            {
                // If we can't load settings, use defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        public static string GetSavedLanguage()
        {
            try
            {
                if (LocalSettings == null)
                    return "en-US";

                if (LocalSettings.Values.TryGetValue(nameof(AppLanguage), out var lang))
                {
                    return lang?.ToString() ?? "en-US";
                }
            }
            catch
            {
                // If we can't access settings, return default
            }
            return "en-US";
        }
    }
}
