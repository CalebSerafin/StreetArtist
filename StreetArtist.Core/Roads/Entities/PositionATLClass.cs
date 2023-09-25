namespace StreetArtist.Core.Roads.Entities;
internal record class PositionATLClass(float X, float Y, float Z) {
    public PositionATL ToValueType() => new PositionATL(X, Y, Z);
    public static PositionATLClass FromValueType(PositionATL position) => new PositionATLClass(position.X, position.Y, position.Z);
};
