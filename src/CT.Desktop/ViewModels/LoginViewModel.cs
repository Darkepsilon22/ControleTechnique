using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;

namespace CT.Desktop.ViewModels;

public partial class LoginViewModel(ApiClient api, Session session) : ViewModelBase
{
    [ObservableProperty] private string _email = "";

    public string MotDePasse { get; set; } = "";

    public event Action? ConnexionReussie;

    [RelayCommand]
    private async Task ConnecterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(MotDePasse))
        {
            Erreur = "Saisissez votre e-mail et votre mot de passe.";
            return;
        }

        var connecte = await ExecuterAsync(async () => session.Ouvrir(await api.ConnecterAsync(Email.Trim(), MotDePasse)));
        if (connecte)
            ConnexionReussie?.Invoke();
    }
}
