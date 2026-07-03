using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Vision;

public interface IVisionDebugService
{
    Task<TemplateMatchResult> MatchTemplateAsync
    (
        TemplateMatchRequest request,
        CancellationToken cancellationToken = default);
}