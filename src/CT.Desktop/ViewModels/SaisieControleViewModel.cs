using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public partial class ChoixDefaillance : ObservableObject
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required NiveauDefaillance Niveau { get; init; }

    [ObservableProperty] private bool _cochee;
}

public partial class LigneSaisie : ObservableObject
{
    public required Guid PointControleId { get; init; }
    public required string Code { get; init; }
    public required string Libelle { get; init; }
    public required string Fonction { get; init; }
    public required IReadOnlyList<ChoixDefaillance> Defaillances { get; init; }

    [ObservableProperty] private EtatPoint? _etat;
    [ObservableProperty] private string? _commentaire;

    public void Initialiser()
    {
        foreach (var choix in Defaillances)
            choix.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChoixDefaillance.Cochee) && choix.Cochee)
                    Etat = EtatPoint.NonConforme;
                OnPropertyChanged(nameof(Defaillances));
            };
    }

    partial void OnEtatChanged(EtatPoint? value)
    {
        if (value == EtatPoint.NonConforme)
            return;
        foreach (var choix in Defaillances)
            choix.Cochee = false;
    }
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
    [ObservableProperty] private int _nombreMineures;
    [ObservableProperty] private int _nombreMajeures;
    [ObservableProperty] private int _nombreCritiques;

    public ObservableCollection<LigneSaisie> Lignes { get; } = [];
    public ICollectionView LignesParFonction { get; }

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
        LignesParFonction = CollectionViewSource.GetDefaultView(Lignes);
        LignesParFonction.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LigneSaisie.Fonction)));
    }

    public Task ChargerAsync(Guid controleId) => ExecuterAsync(async () =>
    {
        var controle = await _api.ObtenirControleAsync(controleId);
        var catalogue = await _api.ListerPointsAsync();
        var resultats = controle.Resultats.ToDictionary(r => r.PointControleId);

        foreach (var ligne in Lignes)
            ligne.PropertyChanged -= LigneModifiee;
        Lignes.Clear();

        foreach (var point in catalogue.Where(p => controle.PointsASaisir.Contains(p.Id) || resultats.ContainsKey(p.Id)))
        {
            resultats.TryGetValue(point.Id, out var resultat);
            var constatees = resultat?.Defaillances.Select(d => d.Id).ToHashSet() ?? [];
            var ligne = new LigneSaisie
            {
                PointControleId = point.Id,
                Code = point.Code,
                Libelle = point.Libelle,
                Fonction = $"{point.NumeroFonction} — {point.Fonction}",
                Defaillances = point.Defaillances
                    .Where(d => d.Actif || constatees.Contains(d.Id))
                    .Select(d => new ChoixDefaillance
                    {
                        Id = d.Id,
                        Code = d.Code,
                        Libelle = d.Libelle,
                        Niveau = d.Niveau,
                        Cochee = constatees.Contains(d.Id)
                    })
                    .ToList(),
                Etat = resultat?.Etat,
                Commentaire = resultat?.Commentaire
            };
            ligne.Initialiser();
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
        var cochees = Lignes.SelectMany(l => l.Defaillances).Where(d => d.Cochee).ToList();
        NombreMineures = cochees.Count(d => d.Niveau == NiveauDefaillance.Mineure);
        NombreMajeures = cochees.Count(d => d.Niveau == NiveauDefaillance.Majeure);
        NombreCritiques = cochees.Count(d => d.Niveau == NiveauDefaillance.Critique);
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
            var resultat = Controle.Resultat switch
            {
                ResultatControle.Favorable => "FAVORABLE (A)",
                ResultatControle.DefavorableMajeur => "DÉFAVORABLE pour défaillances majeures (S)",
                _ => "DÉFAVORABLE pour défaillances critiques (R)"
            };
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
            .Select(l => new SaisiePointRequete(
                l.PointControleId,
                l.Etat!.Value,
                l.Defaillances.Where(d => d.Cochee).Select(d => d.Id).ToList(),
                l.Commentaire))
            .ToList();
        Controle = await _api.SaisirControleAsync(Controle!.Id, new SaisirControleRequete(Kilometrage, saisies));
    }
}
