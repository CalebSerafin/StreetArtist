using System.Diagnostics.CodeAnalysis;

namespace StreetArtist.Core.Infrastructure.StronglyTypedIds;

/// <summary> Marks that the type is a strongly typed id. </summary>
public interface IStrongId {
    /// <summary>Get the value contained in this instance converted to string using the <see><see cref="global::System.Globalization.CultureInfo.InvariantCulture" /></see>.</summary>
    /// <return>The value converted to string using the <see><see cref="global::System.Globalization.CultureInfo.InvariantCulture" /></see>.</return>
    public string ValueAsString { get; }

    /// <summary> Get the value contained in this instance. </summary>
    /// <return> The value contained in this instance. </return>
    public int Value { get; }
}

/// <summary>
/// Ensures that the type implements a factory for producing from an integer.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public interface IStrongId<TSelf> : IStrongId where TSelf : IStrongId<TSelf> {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    static abstract TSelf FromInt32(int value);
}