using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Roma.Models;

namespace Roma.Services
{
    public class ServerDataService
    {
        private readonly HttpClient _httpClient;
        private const string CommunityUrl = "https://gist.githubusercontent.com/NaghDiefallah/6ed3b8f1ed563707f01f37e4bcdd7d89/raw";
        private const string OfficialUrl = "https://cdn.rage.mp/master/";

        public ServerDataService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<List<ServerItem>> FetchServersAsync(string source = "Community")
        {
            try
            {
                var url = source == "Official" ? OfficialUrl : CommunityUrl;
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Explicitly read as UTF-8 to handle garbled Unicode characters
                var jsonBytes = await response.Content.ReadAsByteArrayAsync();
                var jsonContent = Encoding.UTF8.GetString(jsonBytes);

                // Deserialize as Dictionary<string, ServerDataDto>
                var serverDict = JsonSerializer.Deserialize(jsonContent, AppJsonContext.Default.DictionaryStringServerDataDto);

                if (serverDict == null)
                {
                    return new List<ServerItem>();
                }

                // Convert dictionary to list, extracting IP and Port from the key
                var servers = new List<ServerItem>();
                foreach (var kvp in serverDict)
                {
                    var addressKey = kvp.Key; // Format: "IP:PORT"
                    var serverData = kvp.Value;

                    // Split the key to extract IP and Port
                    var parts = addressKey.Split(':');
                    if (parts.Length != 2)
                    {
                        continue; // Skip invalid entries
                    }

                    var ip = parts[0];
                    var port = parts[1];

                    servers.Add(new ServerItem
                    {
                        Name = serverData.Name ?? string.Empty,
                        Ip = ip,
                        Port = port,
                        Players = serverData.Players,
                        MaxPlayers = serverData.MaxPlayers,
                        Gamemode = serverData.Gamemode ?? string.Empty,
                        Language = serverData.Lang ?? string.Empty,
                        Url = serverData.Url ?? string.Empty
                    });
                }

                return servers;
            }
            catch (HttpRequestException)
            {
                // Network error - return sample data for demonstration
                return GetSampleServers();
            }
            catch (JsonException)
            {
                // JSON parsing error - return sample data
                return GetSampleServers();
            }
            catch (Exception)
            {
                // Generic fallback
                return GetSampleServers();
            }
        }

        private List<ServerItem> GetSampleServers()
        {
            var random = new Random();
            var servers = new List<ServerItem>();
            var languages = new[] { "EN", "RU", "DE", "ES", "PT", "FR" };
            var gamemodes = new[] { "Roleplay", "Freeroam", "Racing", "Deathmatch", "Drift" };

            for (int i = 1; i <= 50; i++)
            {
                servers.Add(new ServerItem
                {
                    Name = $"Sample Server {i} - {gamemodes[random.Next(gamemodes.Length)]}",
                    Ip = $"192.168.{random.Next(1, 255)}.{random.Next(1, 255)}",
                    Port = (22005 + i).ToString(),
                    Players = random.Next(0, 500),
                    MaxPlayers = 500,
                    Gamemode = gamemodes[random.Next(gamemodes.Length)],
                    Language = languages[random.Next(languages.Length)],
                    Url = ""
                });
            }

            return servers;
        }
    }
}
