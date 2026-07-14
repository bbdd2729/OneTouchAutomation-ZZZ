using OneTouchAutomation.Services.Vision;
using System.Text;

namespace Test.Automation;

public sealed class ScreenRecognitionServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"OneTouchAutomationTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task RecognizeAsync_ReturnsScreenIdWhenRequiredElementsMatchAndExcludedElementsDoNot()
    {
        Directory.CreateDirectory(_directory);
        var requiredPath = CreateTemplate("required.png", "required");
        var excludedPath = CreateTemplate("excluded.png", "excluded");
        var service = new ScreenRecognitionService(new StubVisionService(new HashSet<string> { "required" }));

        var result = await service.RecognizeAsync(
            [1, 2, 3],
            new ScreenDefinition
            {
                Id = "main-menu",
                Name = "Main Menu",
                RequiredElements = [new ScreenElementDefinition { TemplatePath = requiredPath }],
                ExcludedElements = [new ScreenElementDefinition { TemplatePath = excludedPath }]
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsMatch);
        Assert.Equal("main-menu", result.StatusCode);
    }

    [Fact]
    public async Task RecognizeAsync_ReturnsNotScreenIdWhenAnExcludedElementMatches()
    {
        Directory.CreateDirectory(_directory);
        var requiredPath = CreateTemplate("required.png", "required");
        var excludedPath = CreateTemplate("excluded.png", "excluded");
        var service = new ScreenRecognitionService(new StubVisionService(new HashSet<string> { "required", "excluded" }));

        var result = await service.RecognizeAsync(
            [1],
            new ScreenDefinition
            {
                Id = "main-menu",
                Name = "Main Menu",
                RequiredElements = [new ScreenElementDefinition { TemplatePath = requiredPath }],
                ExcludedElements = [new ScreenElementDefinition { TemplatePath = excludedPath }]
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsMatch);
        Assert.Equal("not-main-menu", result.StatusCode);
    }

    public void Dispose()
    {
        if(Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private string CreateTemplate(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed class StubVisionService : IVisionDebugService
    {
        private readonly IReadOnlySet<string> _matchingTemplates;

        public StubVisionService(IReadOnlySet<string> matchingTemplates)
        {
            _matchingTemplates = matchingTemplates;
        }

        public Task<TemplateMatchResult> MatchTemplateAsync(TemplateMatchRequest request, CancellationToken cancellationToken = default)
        {
            var content = Encoding.UTF8.GetString(request.TemplateBytes);
            return Task.FromResult(new TemplateMatchResult { IsMatch = _matchingTemplates.Contains(content), Message = "stub" });
        }
    }
}
