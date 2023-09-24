using System.Text.Json.Serialization;

namespace StreetArtist.Core.A3SA.Storage.Models;
internal class MetaInformation {
    [JsonPropertyName("systemTimeUCT_G")]
    public string? SystemTimeUTC_G { get; set; }

    [JsonPropertyName("worldName")]
    public string? WorldName { get; set; }

    [JsonPropertyName("StreetArtist_Config")]
    public StreetArtistConfig? StreetArtistConfig { get; set; }
}
