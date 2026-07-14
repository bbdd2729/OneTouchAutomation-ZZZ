using OneTouchAutomation.Services.Automation.State;

namespace Test.Automation;

public sealed class JsonSceneConfigurationStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ota-scene-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoadAsync_PreservesSceneConditions()
    {
        var store = new JsonSceneConfigurationStore(Path.Combine(_directory, "scenes.json"));
        var scene = new AutomationSceneDefinition
        {
            Id = "confirm-dialog",
            Name = "Confirm dialog",
            WorkflowId = "dismiss-dialog",
            Priority = 100,
            Cooldown = TimeSpan.FromSeconds(3),
            Conditions = new GameStateConditionGroup
            {
                Operator = GameStateConditionOperator.All,
                Conditions =
                [
                    new GameStateCondition
                    {
                        StateId = "ConfirmDialog",
                        MinimumConfidence = 0.9,
                        MaximumAge = TimeSpan.FromSeconds(1)
                    }
                ]
            }
        };

        await store.SaveAsync([scene], TestContext.Current.CancellationToken);
        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        var loadedScene = Assert.Single(loaded);
        Assert.Equal(scene.Id, loadedScene.Id);
        Assert.Equal(scene.WorkflowId, loadedScene.WorkflowId);
        Assert.Equal(scene.Cooldown, loadedScene.Cooldown);
        Assert.Equal("ConfirmDialog", Assert.Single(loadedScene.Conditions.Conditions).StateId);
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicateSceneIds()
    {
        var store = new JsonSceneConfigurationStore(Path.Combine(_directory, "scenes.json"));
        var first = CreateScene("duplicate");
        var second = CreateScene("duplicate");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.SaveAsync([first, second], TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private static AutomationSceneDefinition CreateScene(string id) => new()
    {
        Id = id,
        Name = id,
        WorkflowId = "workflow",
    };
}
