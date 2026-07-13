using OneTouchAutomation.Services.Automation.Persistence;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Input;

namespace Test.Automation;

public class JsonTaskConfigurationStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RestoresTaskConfiguration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "tasks.json");
        var store = new JsonTaskConfigurationStore(filePath);

        try
        {
            await store.SaveAsync(
            [
                new AutomationTaskConfiguration
                {
                    Id = "task-1",
                    Name = "Collect reward",
                    BehaviorId = "click-template",
                    TemplatePath = "C:\\templates\\reward.png",
                    Threshold = 0.9,
                    UseRegion = true,
                    RegionX = 10,
                    RegionY = 20,
                    RegionWidth = 300,
                    RegionHeight = 200,
                    TimeoutSeconds = 15,
                    FailurePolicy = TaskFailurePolicy.Retry,
                    MaxRetryCount = 2,
                    Key = AutomationKey.Space,
                    ClickOffsetX = 8,
                    ClickOffsetY = -4,
                    ClickMode = MouseClickMode.LongPress,
                    ClickRepeatCount = 3,
                    ClickIntervalMilliseconds = 120,
                    ClickHoldDurationMilliseconds = 250,
                    IsEnabled = false
                }
            ],
            TestContext.Current.CancellationToken);

            var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

            var task = Assert.Single(loaded);
            Assert.Equal("task-1", task.Id);
            Assert.Equal("Collect reward", task.Name);
            Assert.Equal("click-template", task.BehaviorId);
            Assert.Equal("C:\\templates\\reward.png", task.TemplatePath);
            Assert.Equal(0.9, task.Threshold);
            Assert.True(task.UseRegion);
            Assert.Equal(10, task.RegionX);
            Assert.Equal(20, task.RegionY);
            Assert.Equal(300, task.RegionWidth);
            Assert.Equal(200, task.RegionHeight);
            Assert.Equal(15, task.TimeoutSeconds);
            Assert.Equal(TaskFailurePolicy.Retry, task.FailurePolicy);
            Assert.Equal(2, task.MaxRetryCount);
            Assert.Equal(AutomationKey.Space, task.Key);
            Assert.Equal(8, task.ClickOffsetX);
            Assert.Equal(-4, task.ClickOffsetY);
            Assert.Equal(MouseClickMode.LongPress, task.ClickMode);
            Assert.Equal(3, task.ClickRepeatCount);
            Assert.Equal(120, task.ClickIntervalMilliseconds);
            Assert.Equal(250, task.ClickHoldDurationMilliseconds);
            Assert.False(task.IsEnabled);
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
    public async Task LoadAsync_ReturnsEmptyList_WhenConfigurationFileDoesNotExist()
    {
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "tasks.json");
        var store = new JsonTaskConfigurationStore(filePath);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task LoadAsync_ThrowsJsonException_WhenConfigurationFileIsInvalid()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "tasks.json");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, "not valid json", TestContext.Current.CancellationToken);
        var store = new JsonTaskConfigurationStore(filePath);

        try
        {
            var action = () => store.LoadAsync(TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(action);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
