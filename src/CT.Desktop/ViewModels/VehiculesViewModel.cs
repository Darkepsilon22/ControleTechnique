using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public partial class VehiculesViewModel(ApiClient api, Session session, NavigationService navigation, IDialogService dialogues)
    : ViewModelBase
{
    [ObservableProperty] private string? _recherche;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _nombrePages;
    [ObservableProperty] private int _total;
    [ObservableProperty] private VehiculeDto? _vehiculeSelectionne;
    [ObservableProperty] private ControleResumeDto? _controleSelectionne;

    [ObservableProperty] private Guid? _editionId;
    [ObservableProperty] private string _immatriculation = "";
    [ObservableProperty] private string _numeroChassis = "";
    [ObservableProperty] private string _marque = "";
    [ObservableProperty] private string _modele = "";
    [ObservableProperty] private int _annee = DateTime.Today.Year;
    [ObservableProperty] private TypeVehicule _typeVehicule;
    [ObservableProperty] private Energie _energie;
    [ObservableProperty] private string? _rechercheProprietaire;
    [ObservableProperty] private ProprietaireDto? _proprietaireSelectionne;
    [ObservableProperty] private int? _kilometrage;

    public ObservableCollection<VehiculeDto> Vehicules { get; } = [];
    public ObservableCollection<ControleResumeDto> Historique { get; } = [];
    public ObservableCollection<ProprietaireDto> Proprietaires { get; } = [];
    public IReadOnlyList<TypeVehicule> Types { get; } = Enum.GetValues<TypeVehicule>();
    public IReadOnlyList<Energie> Energies { get; } = Enum.GetValues<Energie>();

    public bool PeutModifier => session.PeutGererVehicules;
    public bool PeutOuvrirControle => session.EstInspecteur;
    public string TitreFormulaire => EditionId is null ? "Nouveau véhicule" : $"Modifier {Immatriculation}";

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
        var resultat = await api.RechercherVehiculesAsync(Recherche, Page);
        Vehicules.Clear();
        foreach (var v in resultat.Elements)
            Vehicules.Add(v);
        Total = resultat.Total;
        NombrePages = Math.Max(1, resultat.NombrePages);
    });

    async partial void OnVehiculeSelectionneChanged(VehiculeDto? value)
    {
        Historique.Clear();
        if (value is null)
            return;

        EditionId = value.Id;
        Immatriculation = value.Immatriculation;
        NumeroChassis = value.NumeroChassis;
        Marque = value.Marque;
        Modele = value.Modele;
        Annee = value.Annee;
        TypeVehicule = value.TypeVehicule;
        Energie = value.Energie;
        Proprietaires.Clear();
        var proprietaire = new ProprietaireDto(value.ProprietaireId, value.ProprietaireNom, "", null, 0);
        Proprietaires.Add(proprietaire);
        ProprietaireSelectionne = proprietaire;
        OnPropertyChanged(nameof(TitreFormulaire));

        await ExecuterAsync(async () =>
        {
            foreach (var c in await api.HistoriqueVehiculeAsync(value.Id))
                Historique.Add(c);
        });
    }

    [RelayCommand]
    private void Nouveau()
    {
        VehiculeSelectionne = null;
        EditionId = null;
        Immatriculation = NumeroChassis = Marque = Modele = "";
        Annee = DateTime.Today.Year;
        TypeVehicule = default;
        Energie = default;
        ProprietaireSelectionne = null;
        Proprietaires.Clear();
        OnPropertyChanged(nameof(TitreFormulaire));
    }

    [RelayCommand]
    private Task RechercherProprietairesAsync() => ExecuterAsync(async () =>
    {
        var resultat = await api.RechercherProprietairesAsync(RechercheProprietaire);
        Proprietaires.Clear();
        foreach (var p in resultat.Elements)
            Proprietaires.Add(p);
        ProprietaireSelectionne = Proprietaires.FirstOrDefault();
    });

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (ProprietaireSelectionne is null)
        {
            Erreur = "Choisissez un propriétaire (recherchez-le par son nom).";
            return;
        }

        var requete = new VehiculeRequete(Immatriculation, NumeroChassis, Marque, Modele, Annee, TypeVehicule, Energie, ProprietaireSelectionne.Id);
        VehiculeDto? enregistre = null;
        var ok = await ExecuterAsync(async () =>
            enregistre = EditionId is { } id
                ? await api.ModifierVehiculeAsync(id, requete)
                : await api.CreerVehiculeAsync(requete),
            "Véhicule enregistré.");

        if (ok && enregistre is not null)
        {
            Recherche = enregistre.Immatriculation;
            await ChargerPageAsync();
            VehiculeSelectionne = Vehicules.FirstOrDefault(v => v.Id == enregistre.Id);
            Message = "Véhicule enregistré.";
        }
    }

    [RelayCommand]
    private async Task OuvrirControleAsync()
    {
        if (VehiculeSelectionne is null || Kilometrage is null)
        {
            Erreur = "Sélectionnez un véhicule et saisissez son kilométrage.";
            return;
        }

        ControleDto? controle = null;
        if (await ExecuterAsync(async () => controle = await api.OuvrirControleAsync(new OuvrirControleRequete(VehiculeSelectionne.Id, Kilometrage.Value))))
            await navigation.NaviguerAsync<SaisieControleViewModel>(vm => vm.ChargerAsync(controle!.Id));
    }

    [RelayCommand]
    private async Task OuvrirControleExistantAsync()
    {
        if (ControleSelectionne is null)
            return;

        if (ControleSelectionne.Statut == StatutControle.Cloture)
            await ExecuterAsync(async () =>
            {
                var (contenu, nom) = await api.TelechargerPvAsync(ControleSelectionne.Id);
                dialogues.OuvrirFichier(contenu, nom);
            });
        else if (session.EstInspecteur)
            await navigation.NaviguerAsync<SaisieControleViewModel>(vm => vm.ChargerAsync(ControleSelectionne.Id));
    }
}
