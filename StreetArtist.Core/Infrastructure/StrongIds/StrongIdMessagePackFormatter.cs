using MessagePack.Formatters;
using MessagePack;
using System.Text;

namespace StreetArtist.Core.Infrastructure.StronglyTypedIds;


/// <summary>
/// Serialise strongly typed ids as string.
/// </summary>
/// <typeparam name="T"></typeparam>
public class StrongIdMessagePackFormatter<T> : IMessagePackFormatter<T> where T : IStrongId<T> {
    /// <inheritdoc/>
    public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options) {
        writer.WriteString(Encoding.UTF8.GetBytes(value.ValueAsString));
    }

    /// <inheritdoc/>
    public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        options.Security.DepthStep(ref reader);

        int idInt = reader.ReadInt32();

        reader.Depth--;
        return T.FromInt32(idInt);
    }
}