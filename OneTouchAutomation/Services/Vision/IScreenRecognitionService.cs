using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Vision;

public interface IScreenRecognitionService
{
    Task<ScreenRecognitionResult> RecognizeAsync(
        byte[] sourceBytes,
        ScreenDefinition screen,
        CancellationToken cancellationToken = default);
}
