using System.Text.Json.Serialization;

namespace StreetArtist.Core.A3SA.Storage.Models;
internal class StreetArtistConfig {
    [JsonPropertyName("_flatMaxDrift")]
    public float FlatMaxDrift { get; set; }

    [JsonPropertyName("_juncMergeDistance")]
    public float JunctionMergeDistance { get; set; }

    [JsonPropertyName("_humanEdited")]
    public bool HumanEdited { get; set; }
}
