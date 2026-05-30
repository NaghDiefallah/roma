using System.Text.Json.Serialization;

namespace Roma.Models
{
    public class ServerDataDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("gamemode")]
        public string? Gamemode { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("lang")]
        public string? Lang { get; set; }

        [JsonPropertyName("players")]
        public int Players { get; set; }

        [JsonPropertyName("peak")]
        public int Peak { get; set; }

        [JsonPropertyName("maxplayers")]
        public int MaxPlayers { get; set; }
    }
}
