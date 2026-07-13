using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Workflows;

namespace Test.Automation;

public sealed class WorkflowConfigurationStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RestoresGraphAndTypedParameters()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "workflows.json");
        var registry = new BehaviorRegistry([new TestBehavior()]);
        var validator = new WorkflowValidator(registry);
        var store = new JsonWorkflowConfigurationStore(registry, validator, filePath);

        try
        {
            await store.SaveAsync(
            [
                new WorkflowDefinition
                {
                    Id = "menu-flow",
                    Name = "Open menu",
                    StartNodeId = "wait-menu",
                    Nodes =
                    [
                        new WorkflowNodeDefinition
                        {
                            Id = "wait-menu",
                            Name = "Wait menu",
                            BehaviorId = "test",
                            Parameters = new TestParameters { Value = 42 }
                        }
                    ],
                    Transitions =
                    [
                        new WorkflowTransitionDefinition
                        {
                            FromNodeId = "wait-menu",
                            Outcome = WorkflowNodeOutcome.Success
                        }
                    ]
                }
            ],
            TestContext.Current.CancellationToken);

            var workflow = Assert.Single(await store.LoadAsync(TestContext.Current.CancellationToken));
            var node = Assert.Single(workflow.Nodes);
            var parameters = Assert.IsType<TestParameters>(node.Parameters);

            Assert.Equal("menu-flow", workflow.Id);
            Assert.Equal("wait-menu", workflow.StartNodeId);
            Assert.Equal(42, parameters.Value);
            Assert.Equal(WorkflowNodeOutcome.Success, Assert.Single(workflow.Transitions).Outcome);
        }
        finally
        {
            if(Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void Validate_ReturnsErrorsForDuplicateNodesAndMissingTransitionTarget()
    {
        var validator = new WorkflowValidator(new BehaviorRegistry([new TestBehavior()]));

        var result = validator.Validate(new WorkflowDefinition
        {
            Id = "invalid-flow",
            Name = "Invalid workflow",
            StartNodeId = "missing",
            Nodes =
            [
                CreateNode("duplicate"),
                CreateNode("duplicate")
            ],
            Transitions =
            [
                new WorkflowTransitionDefinition
                {
                    FromNodeId = "duplicate",
                    ToNodeId = "missing",
                    Outcome = WorkflowNodeOutcome.Success
                }
            ]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Duplicate workflow node ID"));
        Assert.Contains(result.Errors, error => error.Contains("Workflow start node not found"));
        Assert.Contains(result.Errors, error => error.Contains("Transition target node not found"));
    }

    private static WorkflowNodeDefinition CreateNode(string id)
    {
        return new WorkflowNodeDefinition
        {
            Id = id,
            Name = id,
            BehaviorId = "test",
            Parameters = new TestParameters()
        };
    }

    private sealed class TestParameters
    {
        public int Value { get; init; }
    }

    private sealed class TestBehavior : IAutomationBehavior
    {
        public string Id => "test";

        public string Name => "Test";

        public string Description => "Test behavior.";

        public Type ParameterType => typeof(TestParameters);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = true,
                Message = "Completed."
            });
        }
    }
}
