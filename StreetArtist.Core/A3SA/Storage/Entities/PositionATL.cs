namespace StreetArtist.Core.A3SA.Storage.Models;
internal struct PositionATL {
    public PositionATL(float x, float y, float z) {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
