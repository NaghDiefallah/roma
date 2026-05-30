using System.Collections.Generic;
using System.Text.Json.Serialization;
using Roma.Models;

namespace Roma
{
    [JsonSourceGenerationOptions(WriteIndented = false, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(List<RecentServer>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, ServerDataDto>))]
    [JsonSerializable(typeof(RecentServer))]
    [JsonSerializable(typeof(ServerDataDto))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
