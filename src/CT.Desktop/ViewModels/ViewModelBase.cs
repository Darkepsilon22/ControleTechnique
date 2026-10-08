using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CT.Desktop.Services;

namespace CT.Desktop.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _occupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    public virtual Task ChargerAsync() => Task.CompletedTask;

    protected async Task<bool> ExecuterAsync(Func<Task> action, string? messageSucces = null)
    {
        Erreur = null;
        Message = null;
        Occupe = true;
        try
        {
            await action();
            Message = messageSucces;
            return true;
        }
        catch (ApiException ex)
        {
            Erreur = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            Occupe = false;
        }
        return false;
    }
}
