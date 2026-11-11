using System.Text.RegularExpressions;
using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Validation;

public static partial class IdentifierRules
{
    private static readonly HashSet<string> CSharpReserved = new(StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue",
        "decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally",
        "fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params","private","protected",
        "public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string",
        "struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
        "using","virtual","void","volatile","while","record","required","file","scoped"
    };

    private static readonly HashSet<string> SqlReserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "select","insert","update","delete","from","where","join","inner","outer","left","right","full","on",
        "group","by","order","having","union","all","distinct","create","alter","drop","table","index","view",
        "primary","key","foreign","references","constraint","unique","check","default","null","not","and","or",
        "in","is","like","between","exists","case","when","then","else","end","role","grant","revoke",
        "truncate","into","values","set","as","asc","desc","limit","offset","with","over","partition","window",
        "trigger","procedure","function","database","schema","column","row","rows","current","session","system"
    };

    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            return false;
        }

        return IdentifierPattern().IsMatch(name);
    }

    public static bool IsReserved(string name, DatabaseEngine engine)
    {
        if (CSharpReserved.Contains(name) || SqlReserved.Contains(name))
        {
            return true;
        }

        return engine switch
        {
            DatabaseEngine.PostgreSql => string.Equals(name, "pg_catalog", StringComparison.OrdinalIgnoreCase),
            DatabaseEngine.SqlServer => string.Equals(name, "sys", StringComparison.OrdinalIgnoreCase),
            DatabaseEngine.Oracle => string.Equals(name, "DUAL", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public static bool IsSafeDefaultValue(string? value, string logicalType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Contains(';', StringComparison.Ordinal) ||
            value.Contains("--", StringComparison.Ordinal) ||
            value.Contains("/*", StringComparison.Ordinal) ||
            value.Contains("*/", StringComparison.Ordinal))
        {
            return false;
        }

        return logicalType switch
        {
            "int" or "bigint" => long.TryParse(value, out _),
            "decimal" => decimal.TryParse(value, out _),
            "boolean" => value is "true" or "false" or "0" or "1",
            "uuid" => Guid.TryParse(value, out _),
            "string" or "text" => value.Length <= 256 && !value.Contains('\n'),
            "timestamp" or "date" or "datetime" => true,
            _ => value.Length <= 128 && DefaultLiteral().IsMatch(value)
        };
    }

    public static bool IsSafeCheckConstraint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return SimpleCheck().IsMatch(value);
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_+\-.:@/ ]+$")]
    private static partial Regex DefaultLiteral();

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]*\s*(=|<>|!=|<|>|<=|>=)\s*[A-Za-z0-9_.'\-]+$")]
    private static partial Regex SimpleCheck();
}
