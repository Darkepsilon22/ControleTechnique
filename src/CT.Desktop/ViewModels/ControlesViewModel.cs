using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public record FiltreStatut(string Libelle, StatutControle? Valeur);

public partial class ControlesViewModel(ApiClient api, Session session, NavigationService navigation, IDialogService dialogues)
    : ViewModelBase
{
    [ObservableProperty] private string? _recherche;
    [ObservableProperty] private FiltreStatut _filtre = Filtres[0];
    [ObservableProperty] private bool _mesControles = session.EstInspecteur;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _nombrePages;
    [ObservableProperty] private int _total;
    [ObservableProperty] private ControleResumeDto? _controleSelectionne;
    [ObservableProperty] private ControleDto? _detail;
    [ObservableProperty] private int? _kilometrageContreVisite;
    [ObservableProperty] private DateTime _dateContreVisite = DateTime.Today;

    public static IReadOnlyList<FiltreStatut> Filtres { get; } =
    [
        new("Tous", null),
        new("En cours (brouillon)", StatutControle.Brouillon),
        new("Clôturés", StatutControle.Cloture)
    ];

    public ObservableCollection<ControleResumeDto> Controles { get; } = [];
    public ObservableCollection<DefaillanceDto> Defaillances { get; } = [];

    public bool EstInspecteur => session.EstInspecteur;
    public bool PeutReprendre => Detail is { Statut: StatutControle.Brouillon } d && d.InspecteurId == session.Utilisateur?.Id;
    public bool PeutOuvrirPv => Detail?.Statut == StatutControle.Cloture;
    public bool PeutOuvrirContreVisite => session.EstInspecteur
        && Detail is { Statut: StatutControle.Cloture, Resultat: not ResultatControle.Favorable, ContreVisiteId: null }
        && DateOnly.FromDateTime(DateTime.Today) <= Detail.DateLimiteContreVisite;
    public bool AucuneDefaillance => Detail is not null && Defaillances.Count == 0;
    public string? Echeance => Detail?.Resultat switch
    {
        ResultatControle.Favorable => $"Prochain contrôle avant le {Detail.DateFinValidite:dd/MM/yyyy}",
        ResultatControle.DefavorableMajeur => $"Contre-visite avant le {Detail.DateLimiteContreVisite:dd/MM/yyyy}",
        ResultatControle.DefavorableCritique =>
            $"Circulation autorisée jusqu'à minuit le {Detail.DateFinValidite:dd/MM/yyyy} — contre-visite avant le {Detail.DateLimiteContreVisite:dd/MM/yyyy}",
        _ => null
    };

    public override Task ChargerAsync() => RechercherAsync();

    partial void OnFiltreChanged(FiltreStatut value) => RechercherCommand.Execute(null);
    partial void OnMesControlesChanged(bool value) => RechercherCommand.Execute(null);

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
        var resultat = await api.ListerControlesAsync(Filtre.Valeur, Recherche, MesControles, Page);
        Controles.Clear();
        foreach (var c in resultat.Elements)
            Controles.Add(c);
        Total = resultat.Total;
        NombrePages = Math.Max(1, resultat.NombrePages);
    });

    async partial void OnControleSelectionneChanged(ControleResumeDto? value)
    {
        Detail = null;
        Defaillances.Clear();
        if (value is null)
            return;

        await ExecuterAsync(async () =>
        {
            Detail = await api.ObtenirControleAsync(value.Id);
            foreach (var d in Detail.DefaillancesConstatees.OrderByDescending(d => d.Niveau).ThenBy(d => d.Code))
                Defaillances.Add(d);
            OnPropertyChanged(nameof(AucuneDefaillance));
        });
    }

    partial void OnDetailChanged(ControleDto? value)
    {
        OnPropertyChanged(nameof(PeutReprendre));
        OnPropertyChanged(nameof(PeutOuvrirPv));
        OnPropertyChanged(nameof(PeutOuvrirContreVisite));
        OnPropertyChanged(nameof(AucuneDefaillance));
        OnPropertyChanged(nameof(Echeance));
    }

    [RelayCommand]
    private async Task OuvrirContreVisiteAsync()
    {
        if (Detail is null || KilometrageContreVisite is null)
        {
            Erreur = "Saisissez le kilométrage relevé pour la contre-visite.";
            return;
        }

        var requete = new OuvrirContreVisiteRequete(KilometrageContreVisite.Value, DateAvecHeure(DateContreVisite));
        ControleDto? contreVisite = null;
        if (await ExecuterAsync(async () => contreVisite = await api.OuvrirContreVisiteAsync(Detail.Id, requete)))
            await navigation.NaviguerAsync<SaisieControleViewModel>(vm => vm.ChargerAsync(contreVisite!.Id));
    }

    public static DateTime? DateAvecHeure(DateTime date) =>
        date.Date == DateTime.Today ? null : date.Date.Add(DateTime.Now.TimeOfDay);

    [RelayCommand]
    private Task ReprendreAsync() =>
        Detail is null ? Task.CompletedTask : navigation.NaviguerAsync<SaisieControleViewModel>(vm => vm.ChargerAsync(Detail.Id));

    [RelayCommand]
    private Task OuvrirPvAsync() => ExecuterAsync(async () =>
    {
        var (contenu, nom) = await api.TelechargerPvAsync(Detail!.Id);
        dialogues.OuvrirFichier(contenu, nom);
    });
}
