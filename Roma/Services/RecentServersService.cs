using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Roma.Models;
using Windows.Storage;

namespace Roma.Services
{
    public class RecentServersService
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
        private const string RECENT_KEY = "RecentServers";
        private const int MAX_RECENT = 10;
        private List<RecentServer> _recentServers;

        public RecentServersService()
        {
            LoadRecent();
        }

        public void AddRecent(string ip, string port, string name, string displayName)
        {
            // Remove if already exists
            _recentServers.RemoveAll(r => r.Ip == ip && r.Port == port);

            // Add to front
            _recentServers.Insert(0, new RecentServer
            {
                Ip = ip,
                Port = port,
                Name = name,
                DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName,
                LastConnected = DateTime.Now
            });

            // Keep only MAX_RECENT
            if (_recentServers.Count > MAX_RECENT)
            {
                _recentServers = _recentServers.Take(MAX_RECENT).ToList();
            }

            SaveRecent();
        }

        public List<RecentServer> GetRecent()
        {
            return new List<RecentServer>(_recentServers);
        }

        public void ClearRecent()
        {
            _recentServers.Clear();
            SaveRecent();
        }

        private void LoadRecent()
        {
            try
            {
                if (LocalSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LocalSettings is null in RecentServersService");
                    _recentServers = new List<RecentServer>();
                    return;
                }

                if (LocalSettings.Values.TryGetValue(RECENT_KEY, out var data))
                {
                    try
                    {
                        var json = data?.ToString();
                        if (json != null)
                        {
                            _recentServers = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListRecentServer) ?? new List<RecentServer>();
                        }
                        else
                        {
                            _recentServers = new List<RecentServer>();
                        }
                    }
                    catch
                    {
                        _recentServers = new List<RecentServer>();
                    }
                }
                else
                {
                    _recentServers = new List<RecentServer>();
                }
            }
            catch (Exception ex)
            {
                // If we can't access LocalSettings, initialize with empty list
                System.Diagnostics.Debug.WriteLine($"Failed to load recent servers: {ex.Message}");
                _recentServers = new List<RecentServer>();
            }
        }

        private void SaveRecent()
        {
            try
            {
                if (LocalSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LocalSettings is null, cannot save recent servers");
                    return;
                }

                var json = JsonSerializer.Serialize(_recentServers, AppJsonContext.Default.ListRecentServer);
                LocalSettings.Values[RECENT_KEY] = json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save recent servers: {ex.Message}");
            }
        }
    }
}
