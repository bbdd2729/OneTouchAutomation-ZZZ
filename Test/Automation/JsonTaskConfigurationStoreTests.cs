using OneTouchAutomation.Services.Automation.Persistence;

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
                    IsEnabled = false
                }
            ]);

            var loaded = await store.LoadAsync();

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

        var loaded = await store.LoadAsync();

        Assert.Empty(loaded);
    }
}
