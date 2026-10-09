using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public partial class LigneSaisie : ObservableObject
{
    public required Guid PointControleId { get; init; }
    public required string Categorie { get; init; }
    public required string Libelle { get; init; }
    public required Gravite Gravite { get; init; }

    [ObservableProperty] private EtatPoint? _etat;
    [ObservableProperty] private string? _commentaire;
}

public partial class SaisieControleViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly Session _session;
    private readonly NavigationService _navigation;
    private readonly IDialogService _dialogues;

    [ObservableProperty] private ControleDto? _controle;
    [ObservableProperty] private int? _kilometrage;
    [ObservableProperty] private int _nombreSaisis;
    [ObservableProperty] private int _nombreNonConformes;

    public ObservableCollection<LigneSaisie> Lignes { get; } = [];
    public ICollectionView LignesParCategorie { get; }

    public bool EstModifiable =>
        Controle is { Statut: StatutControle.Brouillon } c && c.InspecteurId == _session.Utilisateur?.Id;
    public bool EstCloture => Controle?.Statut == StatutControle.Cloture;
    public string Titre => Controle?.EstContreVisite == true ? "Contre-visite" : "Contrôle";

    public SaisieControleViewModel(ApiClient api, Session session, NavigationService navigation, IDialogService dialogues)
    {
        _api = api;
        _session = session;
        _navigation = navigation;
        _dialogues = dialogues;
        LignesParCategorie = CollectionViewSource.GetDefaultView(Lignes);
        LignesParCategorie.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LigneSaisie.Categorie)));
    }

    public Task ChargerAsync(Guid controleId) => ExecuterAsync(async () =>
    {
        var controle = await _api.ObtenirControleAsync(controleId);
        var points = await _api.ListerPointsAsync();
        var resultats = controle.Resultats.ToDictionary(r => r.PointControleId);

        foreach (var ligne in Lignes)
            ligne.PropertyChanged -= LigneModifiee;
        Lignes.Clear();

        var aSaisir = controle.EstContreVisite
            ? points.Where(p => controle.PointsContreVisite.Contains(p.Id))
            : points.Where(p => p.Actif || resultats.ContainsKey(p.Id));

        foreach (var point in aSaisir)
        {
            resultats.TryGetValue(point.Id, out var resultat);
            var ligne = new LigneSaisie
            {
                PointControleId = point.Id,
                Categorie = point.Categorie,
                Libelle = point.Libelle,
                Gravite = point.Gravite,
                Etat = resultat?.Etat,
                Commentaire = resultat?.Commentaire
            };
            ligne.PropertyChanged += LigneModifiee;
            Lignes.Add(ligne);
        }

        Kilometrage = controle.Kilometrage;
        Controle = controle;
        RecalculerCompteurs();
    });

    partial void OnControleChanged(ControleDto? value)
    {
        OnPropertyChanged(nameof(EstModifiable));
        OnPropertyChanged(nameof(EstCloture));
        OnPropertyChanged(nameof(Titre));
    }

    private void LigneModifiee(object? sender, PropertyChangedEventArgs e) => RecalculerCompteurs();

    private void RecalculerCompteurs()
    {
        NombreSaisis = Lignes.Count(l => l.Etat is not null);
        NombreNonConformes = Lignes.Count(l => l.Etat == EtatPoint.NonConforme);
    }

    [RelayCommand]
    private void ToutConforme()
    {
        foreach (var ligne in Lignes.Where(l => l.Etat is null))
            ligne.Etat = EtatPoint.Conforme;
    }

    [RelayCommand]
    private Task EnregistrerAsync() => ExecuterAsync(EnvoyerSaisieAsync, "Saisie enregistrée.");

    [RelayCommand]
    private async Task CloturerAsync()
    {
        if (Controle is null)
            return;
        if (NombreSaisis < Lignes.Count)
        {
            Erreur = $"{Lignes.Count - NombreSaisis} point(s) restent à saisir avant la clôture.";
            return;
        }
        if (!_dialogues.Confirmer("Après la clôture, le contrôle ne pourra plus être modifié. Clôturer maintenant ?"))
            return;

        var ok = await ExecuterAsync(async () =>
        {
            await EnvoyerSaisieAsync();
            Controle = await _api.CloturerControleAsync(Controle.Id);
        });

        if (ok && Controle is not null)
        {
            var resultat = Controle.Resultat == ResultatControle.Favorable ? "FAVORABLE" : "DÉFAVORABLE";
            Message = $"Contrôle clôturé : {resultat}.";
            if (_dialogues.Confirmer($"Résultat : {resultat}. Ouvrir le procès-verbal ?", "Contrôle clôturé"))
                await OuvrirPvAsync();
        }
    }

    [RelayCommand]
    private Task OuvrirPvAsync() => ExecuterAsync(async () =>
    {
        var (contenu, nom) = await _api.TelechargerPvAsync(Controle!.Id);
        _dialogues.OuvrirFichier(contenu, nom);
    });

    [RelayCommand]
    private Task RetourAsync() => _navigation.NaviguerAsync<ControlesViewModel>();

    private async Task EnvoyerSaisieAsync()
    {
        var saisies = Lignes
            .Where(l => l.Etat is not null)
            .Select(l => new SaisiePointRequete(l.PointControleId, l.Etat!.Value, l.Commentaire))
            .ToList();
        Controle = await _api.SaisirControleAsync(Controle!.Id, new SaisirControleRequete(Kilometrage, saisies));
    }
}
