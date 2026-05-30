using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace Roma.Services
{
    public class FavoritesService
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
        private const string FAVORITES_KEY = "FavoriteServers";
        private HashSet<string> _favorites;

        public FavoritesService()
        {
            LoadFavorites();
        }

        public bool IsFavorite(string ip, string port)
        {
            var key = $"{ip}:{port}";
            return _favorites.Contains(key);
        }

        public void AddFavorite(string ip, string port)
        {
            var key = $"{ip}:{port}";
            if (_favorites.Add(key))
            {
                SaveFavorites();
            }
        }

        public void RemoveFavorite(string ip, string port)
        {
            var key = $"{ip}:{port}";
            if (_favorites.Remove(key))
            {
                SaveFavorites();
            }
        }

        public void ToggleFavorite(string ip, string port)
        {
            if (IsFavorite(ip, port))
                RemoveFavorite(ip, port);
            else
                AddFavorite(ip, port);
        }

        public HashSet<string> GetAllFavorites()
        {
            return new HashSet<string>(_favorites);
        }

        private void LoadFavorites()
        {
            try
            {
                if (LocalSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LocalSettings is null in FavoritesService");
                    _favorites = new HashSet<string>();
                    return;
                }

                if (LocalSettings.Values.TryGetValue(FAVORITES_KEY, out var data))
                {
                    try
                    {
                        var json = data?.ToString();
                        if (json != null)
                        {
                            var list = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListString);
                            _favorites = list != null ? new HashSet<string>(list) : new HashSet<string>();
                        }
                        else
                        {
                            _favorites = new HashSet<string>();
                        }
                    }
                    catch
                    {
                        _favorites = new HashSet<string>();
                    }
                }
                else
                {
                    _favorites = new HashSet<string>();
                }
            }
            catch (Exception ex)
            {
                // If we can't access LocalSettings, initialize with empty set
                System.Diagnostics.Debug.WriteLine($"Failed to load favorites: {ex.Message}");
                _favorites = new HashSet<string>();
            }
        }

        private void SaveFavorites()
        {
            try
            {
                if (LocalSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LocalSettings is null, cannot save favorites");
                    return;
                }

                var json = JsonSerializer.Serialize(_favorites.ToList(), AppJsonContext.Default.ListString);
                LocalSettings.Values[FAVORITES_KEY] = json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save favorites: {ex.Message}");
            }
        }
    }
}
