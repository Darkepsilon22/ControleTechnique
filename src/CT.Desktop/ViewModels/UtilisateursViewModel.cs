using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public partial class UtilisateursViewModel(ApiClient api) : ViewModelBase
{
    [ObservableProperty] private UtilisateurDto? _utilisateurSelectionne;
    [ObservableProperty] private Guid? _editionId;
    [ObservableProperty] private string _nomComplet = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private Role _role = Role.Inspecteur;
    [ObservableProperty] private bool _actif = true;

    public string MotDePasse { get; set; } = "";
    public event Action? MotDePasseEfface;

    public ObservableCollection<UtilisateurDto> Utilisateurs { get; } = [];
    public IReadOnlyList<Role> Roles { get; } = Enum.GetValues<Role>();
    public string TitreFormulaire => EditionId is null ? "Nouveau compte" : "Modifier le compte";
    public string AideMotDePasse => EditionId is null
        ? "Mot de passe initial (8 caractères minimum)"
        : "Nouveau mot de passe (laisser vide pour ne pas le changer)";

    public override Task ChargerAsync() => ExecuterAsync(async () =>
    {
        Utilisateurs.Clear();
        foreach (var u in await api.ListerUtilisateursAsync())
            Utilisateurs.Add(u);
    });

    partial void OnUtilisateurSelectionneChanged(UtilisateurDto? value)
    {
        if (value is null)
            return;
        EditionId = value.Id;
        NomComplet = value.NomComplet;
        Email = value.Email;
        Role = value.Role;
        Actif = value.Actif;
        EffacerMotDePasse();
    }

    partial void OnEditionIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(TitreFormulaire));
        OnPropertyChanged(nameof(AideMotDePasse));
    }

    [RelayCommand]
    private void Nouveau()
    {
        UtilisateurSelectionne = null;
        EditionId = null;
        NomComplet = Email = "";
        Role = Role.Inspecteur;
        Actif = true;
        EffacerMotDePasse();
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        var ok = await ExecuterAsync(async () =>
        {
            if (EditionId is { } id)
                await api.ModifierUtilisateurAsync(id, new ModifierUtilisateurRequete(
                    NomComplet, Email, Role, Actif, string.IsNullOrEmpty(MotDePasse) ? null : MotDePasse));
            else
                await api.CreerUtilisateurAsync(new CreerUtilisateurRequete(NomComplet, Email, MotDePasse, Role));
        });

        if (ok)
        {
            Nouveau();
            await ChargerAsync();
            Message = "Compte enregistré.";
        }
    }

    private void EffacerMotDePasse()
    {
        MotDePasse = "";
        MotDePasseEfface?.Invoke();
    }
}
