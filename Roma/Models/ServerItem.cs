using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Roma.Services;

namespace Roma.Models
{
    public class ServerItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _ping = -1;
        private bool _isFavorite = false;

        public string Name 
        { 
            get => _name;
            set
            {
                _name = value;
                ProcessName();
            }
        }

        public string Ip { get; set; } = string.Empty;

        public string Port { get; set; } = string.Empty;

        public int Players { get; set; }

        public int MaxPlayers { get; set; }

        public string Gamemode { get; set; } = string.Empty;

        public string Language { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int Ping 
        { 
            get => _ping; 
            set 
            { 
                if (_ping != value) 
                { 
                    _ping = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(PingText));
                } 
            } 
        }

        public bool IsFavorite 
        { 
            get => _isFavorite; 
            set 
            { 
                if (_isFavorite != value) 
                { 
                    _isFavorite = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(FavoriteVisibility));
                } 
            } 
        }

        public Microsoft.UI.Xaml.Visibility FavoriteVisibility => IsFavorite ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        // Processed name without version tags and brackets
        public string DisplayName { get; private set; } = string.Empty;

        // Extracted tags from server name
        public List<string> Tags { get; private set; } = new List<string>();

        // Computed properties for UI
        public string CapacityText => $"{Players:N0} / {MaxPlayers:N0}";

        public string AddressText => string.IsNullOrEmpty(Port) ? Ip : $"{Ip}:{Port}";

        public bool IsEmpty => Players == 0;

        public string LanguageDisplay => string.IsNullOrEmpty(Language) ? "EN" : Language.ToUpperInvariant();

        // Flag URL from flagcdn.com
        public string FlagUrl => FlagService.GetFlagUrl(Language);

        public string PingText
        {
            get
            {
                if (Ping == -1) return "...";
                if (Ping == 0) return "...";
                return $"{Ping}ms";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProcessName()
        {
            if (string.IsNullOrEmpty(_name))
            {
                DisplayName = string.Empty;
                Tags.Clear();
                return;
            }

            // Extract tags from brackets like [NEW], [VOICE], [ROLEPLAY], [1.1], etc.
            var tagPattern = @"\[([^\]]+)\]";
            var matches = Regex.Matches(_name, tagPattern);

            Tags.Clear();
            foreach (Match match in matches)
            {
                var tag = match.Groups[1].Value.Trim().ToUpperInvariant();

                // Filter out version numbers like [1.1], [2.0], etc.
                if (!Regex.IsMatch(tag, @"^\d+\.?\d*$"))
                {
                    Tags.Add(tag);
                }
            }

            // Remove all bracketed content and clean up the name
            DisplayName = Regex.Replace(_name, tagPattern, "").Trim();

            // Clean up multiple spaces
            DisplayName = Regex.Replace(DisplayName, @"\s+", " ");
        }
    }
}
