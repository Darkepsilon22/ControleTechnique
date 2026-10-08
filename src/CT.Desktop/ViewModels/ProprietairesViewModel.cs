using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;

namespace CT.Desktop.ViewModels;

public partial class ProprietairesViewModel(ApiClient api) : ViewModelBase
{
    [ObservableProperty] private string? _recherche;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _nombrePages;
    [ObservableProperty] private int _total;
    [ObservableProperty] private ProprietaireDto? _proprietaireSelectionne;

    [ObservableProperty] private Guid? _editionId;
    [ObservableProperty] private string _nom = "";
    [ObservableProperty] private string _telephone = "";
    [ObservableProperty] private string? _adresse;

    public ObservableCollection<ProprietaireDto> Proprietaires { get; } = [];
    public string TitreFormulaire => EditionId is null ? "Nouveau propriétaire" : "Modifier le propriétaire";

    public override Task ChargerAsync() => RechercherAsync();

    [RelayCommand]
    private Task RechercherAsync()
    {
        Page = 1;
        return ChargerPageAsync();
    }

    [RelayCommand]
    private Task PagePrecedenteAsync()
    {
        if (Page <= 1) return Task.CompletedTask;
        Page--;
        return ChargerPageAsync();
    }

    [RelayCommand]
    private Task PageSuivanteAsync()
    {
        if (Page >= NombrePages) return Task.CompletedTask;
        Page++;
        return ChargerPageAsync();
    }

    private Task ChargerPageAsync() => ExecuterAsync(async () =>
    {
        var resultat = await api.RechercherProprietairesAsync(Recherche, Page);
        Proprietaires.Clear();
        foreach (var p in resultat.Elements)
            Proprietaires.Add(p);
        Total = resultat.Total;
        NombrePages = Math.Max(1, resultat.NombrePages);
    });

    partial void OnProprietaireSelectionneChanged(ProprietaireDto? value)
    {
        if (value is null)
            return;
        EditionId = value.Id;
        Nom = value.Nom;
        Telephone = value.Telephone;
        Adresse = value.Adresse;
    }

    partial void OnEditionIdChanged(Guid? value) => OnPropertyChanged(nameof(TitreFormulaire));

    [RelayCommand]
    private void Nouveau()
    {
        ProprietaireSelectionne = null;
        EditionId = null;
        Nom = Telephone = "";
        Adresse = null;
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        var requete = new ProprietaireRequete(Nom, Telephone, Adresse);
        ProprietaireDto? enregistre = null;
        var ok = await ExecuterAsync(async () =>
            enregistre = EditionId is { } id
                ? await api.ModifierProprietaireAsync(id, requete)
                : await api.CreerProprietaireAsync(requete));

        if (ok && enregistre is not null)
        {
            Recherche = enregistre.Nom;
            await ChargerPageAsync();
            ProprietaireSelectionne = Proprietaires.FirstOrDefault(p => p.Id == enregistre.Id);
            Message = "Propriétaire enregistré.";
        }
    }
}
