using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Roma.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizationService()
        {
            // Try to load saved language, default to English
            var savedLanguage = SettingsService.GetSavedLanguage();
            _currentCulture = string.IsNullOrEmpty(savedLanguage) 
                ? new CultureInfo("en-US") 
                : new CultureInfo(savedLanguage);
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture.Name != value.Name)
                {
                    _currentCulture = value;
                    OnPropertyChanged();
                    // Refresh all string properties
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        // UI Strings
        public string AppTitle => GetString("AppTitle");
        public string Search => GetString("Search");
        public string Filters => GetString("Filters");
        public string Refresh => GetString("Refresh");
        public string Settings => GetString("Settings");
        public string Server => GetString("Server");
        public string Players => GetString("Players");
        public string Language => GetString("Language");
        public string Connection => GetString("Connection");
        public string Connect => GetString("Connect");
        public string LoadingServers => GetString("LoadingServers");
        public string FailedToLoadServers => GetString("FailedToLoadServers");
        public string ConnectionFailed => GetString("ConnectionFailed");
        public string RageMpNotFound => GetString("RageMpNotFound");
        public string PleaseInstallRageMP => GetString("PleaseInstallRageMP");
        public string FailedToLaunchRageMP => GetString("FailedToLaunchRageMP");
        public string LanguageFilter => GetString("LanguageFilter");
        public string AllLanguages => GetString("AllLanguages");
        public string GamemodeFilter => GetString("GamemodeFilter");
        public string AllGamemodes => GetString("AllGamemodes");
        public string HideEmptyServers => GetString("HideEmptyServers");
        public string AutoCloseOnConnect => GetString("AutoCloseOnConnect");
        public string RageMpPath => GetString("RageMpPath");
        public string RefreshServerList => GetString("RefreshServerList");
        public string AppLanguage => GetString("AppLanguage");
        public string Close => GetString("Close");
        public string OK => GetString("OK");
        public string Flag => GetString("Flag");
        public string Roleplay => GetString("Roleplay");
        public string Freeroam => GetString("Freeroam");
        public string Racing => GetString("Racing");
        public string Deathmatch => GetString("Deathmatch");
        public string Drift => GetString("Drift");

        private string GetString(string key)
        {
            // Return localized string based on current culture
            return Strings.ResourceManager.GetString(key, _currentCulture) ?? key;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static List<LanguageOption> GetAvailableLanguages()
        {
            return new List<LanguageOption>
            {
                new("en-US", "English", "\U0001F1EC\U0001F1E7"),
                new("es-ES", "Español", "\U0001F1EA\U0001F1F8"),
                new("zh-CN", "中文", "\U0001F1E8\U0001F1F3"),
                new("hi-IN", "हिन्दी", "\U0001F1EE\U0001F1F3"),
                new("ar-SA", "العربية", "\U0001F1F8\U0001F1E6"),
                new("pt-BR", "Português", "\U0001F1E7\U0001F1F7"),
                new("ru-RU", "Русский", "\U0001F1F7\U0001F1FA"),
                new("ja-JP", "日本語", "\U0001F1EF\U0001F1F5"),
                new("de-DE", "Deutsch", "\U0001F1E9\U0001F1EA"),
                new("fr-FR", "Français", "\U0001F1EB\U0001F1F7"),
                new("ko-KR", "한국어", "\U0001F1F0\U0001F1F7"),
                new("it-IT", "Italiano", "\U0001F1EE\U0001F1F9"),
                new("tr-TR", "Türkçe", "\U0001F1F9\U0001F1F7"),
                new("pl-PL", "Polski", "\U0001F1F5\U0001F1F1"),
                new("nl-NL", "Nederlands", "\U0001F1F3\U0001F1F1"),
                new("th-TH", "ไทย", "\U0001F1F9\U0001F1ED"),
                new("vi-VN", "Tiếng Việt", "\U0001F1FB\U0001F1F3"),
                new("id-ID", "Bahasa Indonesia", "\U0001F1EE\U0001F1E9"),
                new("uk-UA", "Українська", "\U0001F1FA\U0001F1E6"),
                new("ro-RO", "Română", "\U0001F1F7\U0001F1F4")
            };
        }
    }

    public record LanguageOption(string Code, string Name, string Flag);
}
