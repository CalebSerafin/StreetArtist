namespace StreetArtist.Core.A3SA.Storage.Models;
internal class NavGridRoot {
    public required MetaInformation? MetaInformation { get; set; }
    public required List<RoadNode> RoadNodes { get; set; }
}
