using OneTouchAutomation.Services.Automation.Behavior;

namespace Test.Automation;

public class BehaviorRegistryTests
{
    [Fact]
    public void Constructor_SortsBehaviorsByName()
    {
        var registry = new BehaviorRegistry(
        [
            new TestBehavior("second", "Zeta"),
            new TestBehavior("first", "Alpha")
        ]);

        Assert.Collection(
            registry.Behaviors,
            first => Assert.Equal("Alpha", first.Name),
            second => Assert.Equal("Zeta", second.Name));
    }

    [Fact]
    public void FindById_ReturnsMatchingBehavior()
    {
        var expected = new TestBehavior("click-template", "Click Template");
        var registry = new BehaviorRegistry([expected]);

        var behavior = registry.FindById("click-template");

        Assert.Same(expected, behavior);
    }

    [Fact]
    public void FindById_ReturnsNullWhenBehaviorDoesNotExist()
    {
        var registry = new BehaviorRegistry([]);

        var behavior = registry.FindById("missing");

        Assert.Null(behavior);
    }

    private sealed class TestBehavior(string id, string name) : IAutomationBehavior
    {
        public string Id { get; } = id;

        public string Name { get; } = name;

        public string Description => "Test behavior";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult
                (new BehaviorExecutionResult
                {
                        IsSuccess = true,
                        Message = "Completed."
                });
        }
    }
}
