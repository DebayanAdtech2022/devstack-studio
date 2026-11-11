using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

internal static class TypeRegistry
{
    public static DataTypeDefinition Type(
        string id,
        string display,
        string provider,
        string clr,
        string ef,
        bool length = false,
        bool requiredLength = false,
        bool precision = false,
        bool identity = false,
        IdentityStrategy defaultIdentity = IdentityStrategy.None,
        int? defaultLength = null,
        int? defaultPrecision = null,
        int? defaultScale = null) => new()
    {
        LogicalId = id,
        DisplayName = display,
        ProviderTypeName = provider,
        ClrTypeName = clr,
        EfStoreType = ef,
        SupportsLength = length,
        RequiresLength = requiredLength,
        SupportsPrecision = precision,
        SupportsScale = precision,
        SupportsIdentity = identity,
        DefaultIdentity = defaultIdentity,
        DefaultLength = defaultLength,
        DefaultPrecision = defaultPrecision,
        DefaultScale = defaultScale
    };
}
