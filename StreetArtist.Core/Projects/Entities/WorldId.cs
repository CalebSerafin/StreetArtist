using StreetArtist.Core.Infrastructure.StronglyTypedIds;
using StreetArtist.Core.Roads.Entities;

namespace StreetArtist.Core.Projects;

[StronglyTypedId(typeof(int), generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct WorldId : IStrongId<WorldId> {
    /// <summary>Initializes a new instance of the <see cref="RoadId" /> using the specified value.</summary>
    /// <param name="value">The value to create the instance.</param>
    public WorldId(int value) {
        _value = value;
    }

    /// <summary> Returns an uninitialized instance. </summary>
    public static readonly RoadId Empty = default;

    /// <summary> Returns true if uninitialized instance. </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static bool IsEmpty(RoadId userId) => userId == Empty;

    /// <summary> Returns true if null nullable or uninitialized instance. </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(RoadId? userId) => userId is null || userId.Value == Empty;

    /// <inheritdoc />
    public override string ToString() => ValueAsString;
}