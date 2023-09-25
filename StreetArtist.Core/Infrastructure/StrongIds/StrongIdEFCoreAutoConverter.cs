using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StreetArtist.Core.Infrastructure.StronglyTypedIds;

/// <summary>
/// Has methods and helpers to auto register StrongId types.
/// </summary>
internal class StrongIdEFCoreAutoConverter {
    /// <summary>
    /// This should be called by the overridden <see cref="DbContext.OnModelCreating(ModelBuilder)"/> method.
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void OnModelCreating(ModelBuilder modelBuilder) {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            foreach (PropertyInfo prop in entityType.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                TryDynamicRegisterStrongIdConverter(modelBuilder, entityType, prop);
    }

    /// <summary> Creates a new instance of the specific <typeparamref name="TStrongId"/>. </summary>
    /// <typeparam name="TStrongId"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TStrongId CreateStrongId<TStrongId>(int value) where TStrongId : IStrongId<TStrongId> => TStrongId.FromInt32(value);

    static HashSet<Type> strongIdTypes = new();
    static readonly Type strongIdType = typeof(IStrongId);
    static readonly Type strongIdTType = typeof(IStrongId<>);
    static readonly Type thisType = typeof(StrongIdEFCoreAutoConverter);
    static readonly MethodInfo registerStrongIdConverterMethod = thisType.GetMethod(registerStrongIdConverterName, BindingFlags.Static | BindingFlags.InvokeMethod, new Type[] { typeof(ModelBuilder), typeof(string) })
        ?? throw new NullReferenceException($"Expected method with name {registerStrongIdConverterName} to exist in contained type!");
    const string registerStrongIdConverterName = nameof(RegisterStrongIdConverter);

    static StrongIdEFCoreAutoConverter() {
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            if (strongIdType.IsAssignableFrom(type) && strongIdTType.MakeGenericType(type).IsAssignableTo(type))
                strongIdTypes.Add(type);
    }

    static bool TryDynamicRegisterStrongIdConverter(ModelBuilder modelBuilder, IMutableEntityType entityType, PropertyInfo property) {
        if (!strongIdTypes.Contains(property.PropertyType))
            return false;
        registerStrongIdConverterMethod
            .MakeGenericMethod(entityType.ClrType, property.PropertyType)
            .Invoke(null, new object[] { modelBuilder, property.Name });
        return true;
    }

    static void RegisterStrongIdConverter<TEntity, TStrongId>(ModelBuilder modelBuilder, string propertyName)
        where TEntity : class
        where TStrongId : IStrongId<TStrongId> {
        Type type = typeof(TStrongId);
        modelBuilder.Entity<TEntity>().Property<TStrongId>(propertyName).HasConversion(new ValueConverter<TStrongId, int>(
            id => id.Value,
            value => CreateStrongId<TStrongId>(value)
        ));
    }
}
