using OneTouchAutomation.Services.Automation.History;

namespace Test.Automation;

public class JsonTaskRunHistoryStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadRecentAsync_RestoresExecutionReport()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var store = new JsonTaskRunHistoryStore(directory);
        var entry = new TaskRunHistoryEntry
        {
            Id = "run-1",
            StartedAt = DateTimeOffset.Parse("2026-01-01T01:02:03+00:00"),
            CompletedAt = DateTimeOffset.Parse("2026-01-01T01:02:05+00:00"),
            TargetWindowTitle = "Game Window",
            Summary = "Completed 1 task(s).",
            IsSuccess = true,
            IsCancelled = false,
            Tasks =
            [
                new TaskRunTaskHistoryEntry
                {
                    TaskId = "task-1",
                    TaskName = "Start game",
                    BehaviorId = "press-key",
                    Message = "Pressed key: Enter.",
                    IsSuccess = true,
                    AttemptCount = 1,
                    MatchScore = 0,
                    ScreenX = null,
                    ScreenY = null,
                    Duration = TimeSpan.FromMilliseconds(60)
                }
            ]
        };

        try
        {
            await store.SaveAsync(entry, TestContext.Current.CancellationToken);

            var entries = await store.LoadRecentAsync(20, TestContext.Current.CancellationToken);

            var loaded = Assert.Single(entries);
            Assert.Equal("run-1", loaded.Id);
            Assert.Equal("Game Window", loaded.TargetWindowTitle);
            Assert.True(loaded.IsSuccess);
            Assert.Equal("01-01 01:02 | Completed 1 task(s).", loaded.DisplayText);
            Assert.Equal("Pressed key: Enter.", Assert.Single(loaded.Tasks).Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task LoadRecentAsync_SkipsInvalidHistoryFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            Path.Combine(directory, "invalid.json"),
            "invalid json",
            TestContext.Current.CancellationToken);
        var store = new JsonTaskRunHistoryStore(directory);

        try
        {
            var entries = await store.LoadRecentAsync(20, TestContext.Current.CancellationToken);

            Assert.Empty(entries);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
