using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

public sealed class FrontendDescriptor
{
    public FrontendFramework Framework { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public FrontendMaturity Maturity { get; init; }
    public string Description { get; init; } = string.Empty;
}

public static class FrontendCatalog
{
    public static IReadOnlyList<FrontendDescriptor> All { get; } =
    [
        new FrontendDescriptor
        {
            Framework = FrontendFramework.NextJs,
            DisplayName = "Next.js (TypeScript)",
            Maturity = FrontendMaturity.Complete,
            Description = "Fully generated App Router frontend with typed API client and entity screens."
        },
        new FrontendDescriptor
        {
            Framework = FrontendFramework.ReactVite,
            DisplayName = "React + Vite",
            Maturity = FrontendMaturity.Planned,
            Description = "Registered for the template registry only. Generation is not implemented."
        }
    ];
}

public static class BackendCatalog
{
    public static IReadOnlyList<(BackendFramework Framework, string DisplayName, bool Complete)> All { get; } =
    [
        (BackendFramework.AspNetCoreCqrs, "ASP.NET Core CQRS (.NET 10 LTS)", true)
    ];
}
