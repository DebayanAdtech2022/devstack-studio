using DevStackStudio.Core.Environment;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Security;
using DevStackStudio.Core.Validation;

namespace DevStackStudio.Core.Tests;

public class IdentifierAndPathTests
{
    [Theory]
    [InlineData("CatalogDemo")]
    [InlineData("dev-stack")]
    [InlineData("App_1")]
    public void Accepts_safe_project_names(string name) => Assert.True(PathSafety.IsSafeProjectName(name));

    [Theory]
    [InlineData("")]
    [InlineData("../evil")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("has space")]
    public void Rejects_unsafe_project_names(string name) => Assert.False(PathSafety.IsSafeProjectName(name));

    [Fact]
    public void CombineUnderRoot_blocks_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "devstack-root");
        Directory.CreateDirectory(root);
        Assert.Throws<InvalidOperationException>(() => PathSafety.CombineUnderRoot(root, "..", "etc", "passwd"));
        Assert.Throws<InvalidOperationException>(() => PathSafety.CombineUnderRoot(root, "ok/../../outside"));
    }

    [Fact]
    public void CombineUnderRoot_allows_nested_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "devstack-root2");
        Directory.CreateDirectory(root);
        var path = PathSafety.CombineUnderRoot(root, "backend", "Api", "Program.cs");
        Assert.StartsWith(Path.GetFullPath(root), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_sql_injection_shaped_defaults()
    {
        Assert.False(IdentifierRules.IsSafeDefaultValue("1; DROP TABLE Users", "int"));
        Assert.False(IdentifierRules.IsSafeCheckConstraint("1=1; DROP TABLE Users"));
        Assert.True(IdentifierRules.IsSafeCheckConstraint("Price >= 0"));
    }

    [Fact]
    public void Reserved_words_are_detected()
    {
        Assert.True(IdentifierRules.IsReserved("select", DatabaseEngine.PostgreSql));
        Assert.True(IdentifierRules.IsReserved("class", DatabaseEngine.PostgreSql));
        Assert.True(IdentifierRules.IsValidIdentifier("Product"));
        Assert.False(IdentifierRules.IsValidIdentifier("1Bad"));
    }
}

public class PlatformMapperTests
{
    [Theory]
    [InlineData(OperatingSystemFamily.Windows, CpuArchitectureKind.X64, "win-x64")]
    [InlineData(OperatingSystemFamily.Linux, CpuArchitectureKind.Arm64, "linux-arm64")]
    [InlineData(OperatingSystemFamily.MacOs, CpuArchitectureKind.Arm64, "osx-arm64")]
    public void Maps_rids(OperatingSystemFamily os, CpuArchitectureKind arch, string rid)
    {
        Assert.Equal(rid, RuntimePlatformMapper.ToRid(os, arch));
        Assert.True(RuntimePlatformMapper.IsSupportedRid(rid));
    }

    [Fact]
    public void Flags_unsupported_platforms()
    {
        var issue = RuntimePlatformMapper.UnsupportedPlatformIssue("linux-mips64");
        Assert.NotNull(issue);
        Assert.Equal(ValidationSeverity.Error, issue!.Severity);
    }
}
