using StreetArtist.Core.Projects;

namespace StreetArtist.Core.Roads.Entities;
public class Road {
    public RoadId Id { get; set; }

    public ProjectId ProjectId { get; set; }

    public PositionATL PositionATL { get => positionATL; set { positionATL = value; positionATLClass = null; } }
    PositionATL positionATL;

    public int IslandId { get; set; }

    public RoadType RoadType { get; set; }


    #region EFCore Navigation Properties
    public ICollection<Road> ConnectedRoads;
    #endregion

    #region EFCore Workarounds
    internal PositionATLClass PositionATLClass {
        get => positionATLClass ??= PositionATLClass.FromValueType(PositionATL);
        set => PositionATL = (positionATLClass = value).ToValueType();
    }
    PositionATLClass? positionATLClass;
    #endregion
}
