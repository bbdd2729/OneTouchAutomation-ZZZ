using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Input;

public interface IWindowActivationService
{
    Task ActivateAsync(IntPtr windowHandle, CancellationToken cancellationToken = default);
}
