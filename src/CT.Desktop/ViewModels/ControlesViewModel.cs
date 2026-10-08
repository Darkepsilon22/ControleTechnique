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

    public static IReadOnlyList<FiltreStatut> Filtres { get; } =
    [
        new("Tous", null),
        new("En cours (brouillon)", StatutControle.Brouillon),
        new("Clôturés", StatutControle.Cloture)
    ];

    public ObservableCollection<ControleResumeDto> Controles { get; } = [];
    public ObservableCollection<ResultatPointDto> NonConformes { get; } = [];

    public bool EstInspecteur => session.EstInspecteur;
    public bool PeutReprendre => Detail is { Statut: StatutControle.Brouillon } d && d.InspecteurId == session.Utilisateur?.Id;
    public bool PeutOuvrirPv => Detail?.Statut == StatutControle.Cloture;

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
        NonConformes.Clear();
        if (value is null)
            return;

        await ExecuterAsync(async () =>
        {
            Detail = await api.ObtenirControleAsync(value.Id);
            foreach (var r in Detail.Resultats.Where(r => r.Etat == EtatPoint.NonConforme))
                NonConformes.Add(r);
        });
    }

    partial void OnDetailChanged(ControleDto? value)
    {
        OnPropertyChanged(nameof(PeutReprendre));
        OnPropertyChanged(nameof(PeutOuvrirPv));
    }

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
