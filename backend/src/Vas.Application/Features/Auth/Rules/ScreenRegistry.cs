using Vas.Shared.Constants;

namespace Vas.Application.Features.Auth.Rules;

public sealed record ScreenDefinition(string Code, string Title, string Route, int Order, bool IsComingSoon);

public static class ScreenRegistry
{
    public static IReadOnlyList<ScreenDefinition> MenuScreens { get; } =
    [
        new(Screens.ExecutiveSummary, "Executive Summary", "/executive-summary", 1, IsComingSoon: false),
        new(Screens.Vas, "VAS", "/vas", 2, IsComingSoon: false),
        new(Screens.Operations, "Operations", "/operations", 3, IsComingSoon: true),
        new(Screens.Finance, "Finance", "/finance", 4, IsComingSoon: true),
        new(Screens.Alfit, "Alfit", "/alfit", 5, IsComingSoon: true),
        new(Screens.Csat, "CSAT", "/csat", 6, IsComingSoon: true),
    ];
}
