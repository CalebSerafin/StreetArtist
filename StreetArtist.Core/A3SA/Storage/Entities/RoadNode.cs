using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreetArtist.Core.A3SA.Storage.Models;
internal class RoadNode {
    public PositionATL Position { get; set; }
    public int IslandId { get; set; }
    public bool IsJunction { get; set; }
    public required List<Connection> Connections { get; set; }
}
