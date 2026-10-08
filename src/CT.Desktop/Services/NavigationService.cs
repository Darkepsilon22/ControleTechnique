using CT.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CT.Desktop.Services;

public class NavigationService(IServiceProvider services)
{
    public event Action<ViewModelBase>? PageChangee;

    public async Task NaviguerAsync<TViewModel>(Func<TViewModel, Task>? initialiser = null) where TViewModel : ViewModelBase
    {
        var viewModel = services.GetRequiredService<TViewModel>();
        PageChangee?.Invoke(viewModel);
        if (initialiser is not null)
            await initialiser(viewModel);
        else
            await viewModel.ChargerAsync();
    }
}
