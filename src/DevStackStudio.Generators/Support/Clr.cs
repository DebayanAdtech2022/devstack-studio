using DevStackStudio.Core.Models;
using DevStackStudio.Core.Providers;

namespace DevStackStudio.Generators.Support;

internal static class Clr
{
    public static string TypeName(ColumnDefinition column, IDatabaseProvider provider)
    {
        var type = provider.GetType(column.LogicalType) ?? throw new InvalidOperationException(column.LogicalType);
        var name = type.ClrTypeName;
        if (column.IsNullable && name is not "string" and not "byte[]")
        {
            return name + "?";
        }

        if (column.IsNullable && name == "string")
        {
            return "string?";
        }

        return name;
    }

    public static string RequiredKeyword(ColumnDefinition column) =>
        column.IsNullable || column.LogicalType is "string" or "text" or "json" or "binary" ? string.Empty : "required ";
}
