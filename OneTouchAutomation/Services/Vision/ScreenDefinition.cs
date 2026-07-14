using System.Collections.Generic;

namespace OneTouchAutomation.Services.Vision;

public sealed class ScreenDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<ScreenElementDefinition> RequiredElements { get; init; } = [];

    public IReadOnlyList<ScreenElementDefinition> ExcludedElements { get; init; } = [];
}
