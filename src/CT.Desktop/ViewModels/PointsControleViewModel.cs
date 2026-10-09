using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;

namespace CT.Desktop.ViewModels;

public record FonctionOption(Guid Id, int Numero, string Libelle)
{
    public string Affichage => $"{Numero} — {Libelle}";
}

public record LignePoint(PointControleDto Point)
{
    public string Fonction => $"{Point.NumeroFonction} — {Point.Fonction}";
}

public partial class PointsControleViewModel : ViewModelBase
{
    private readonly ApiClient _api;

    [ObservableProperty] private LignePoint? _pointSelectionne;
    [ObservableProperty] private Guid? _pointId;
    [ObservableProperty] private FonctionOption? _fonction;
    [ObservableProperty] private string _codePoint = "";
    [ObservableProperty] private string _libellePoint = "";
    [ObservableProperty] private bool _pointActif = true;

    [ObservableProperty] private DefaillanceDto? _defaillanceSelectionnee;
    [ObservableProperty] private Guid? _defaillanceId;
    [ObservableProperty] private string _codeDefaillance = "";
    [ObservableProperty] private string _libelleDefaillance = "";
    [ObservableProperty] private bool _defaillanceActive = true;

    public ObservableCollection<LignePoint> Points { get; } = [];
    public ObservableCollection<FonctionOption> Fonctions { get; } = [];
    public ObservableCollection<DefaillanceDto> Defaillances { get; } = [];
    public ICollectionView PointsParFonction { get; }

    public string TitrePoint => PointId is null ? "Nouveau point" : $"Point {CodePoint}";
    public string TitreDefaillance => DefaillanceId is null ? "Nouvelle défaillance" : $"Défaillance {CodeDefaillance}";
    public bool PeutGererDefaillances => PointId is not null;

    public PointsControleViewModel(ApiClient api)
    {
        _api = api;
        PointsParFonction = CollectionViewSource.GetDefaultView(Points);
        PointsParFonction.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LignePoint.Fonction)));
    }

    public override Task ChargerAsync() => ChargerCatalogueAsync(null);

    private Task ChargerCatalogueAsync(Guid? pointASelectionner) => ExecuterAsync(async () =>
    {
        var points = await _api.ListerPointsAsync();
        Points.Clear();
        foreach (var p in points)
            Points.Add(new LignePoint(p));

        Fonctions.Clear();
        foreach (var f in points.Select(p => new FonctionOption(p.FonctionId, p.NumeroFonction, p.Fonction)).Distinct().OrderBy(f => f.Numero))
            Fonctions.Add(f);

        PointSelectionne = Points.FirstOrDefault(l => l.Point.Id == pointASelectionner);
    });

    partial void OnPointSelectionneChanged(LignePoint? value)
    {
        Defaillances.Clear();
        NouvelleDefaillance();
        if (value is null)
            return;

        var p = value.Point;
        PointId = p.Id;
        Fonction = Fonctions.FirstOrDefault(f => f.Id == p.FonctionId);
        CodePoint = p.Code;
        LibellePoint = p.Libelle;
        PointActif = p.Actif;
        foreach (var d in p.Defaillances)
            Defaillances.Add(d);
        CodeDefaillance = p.Code + ".";
    }

    partial void OnPointIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(TitrePoint));
        OnPropertyChanged(nameof(PeutGererDefaillances));
    }

    partial void OnDefaillanceSelectionneeChanged(DefaillanceDto? value)
    {
        if (value is null)
            return;
        DefaillanceId = value.Id;
        CodeDefaillance = value.Code;
        LibelleDefaillance = value.Libelle;
        DefaillanceActive = value.Actif;
    }

    partial void OnDefaillanceIdChanged(Guid? value) => OnPropertyChanged(nameof(TitreDefaillance));

    [RelayCommand]
    private void NouveauPoint()
    {
        PointSelectionne = null;
        PointId = null;
        CodePoint = LibellePoint = "";
        PointActif = true;
    }

    [RelayCommand]
    private void NouvelleDefaillance()
    {
        DefaillanceSelectionnee = null;
        DefaillanceId = null;
        CodeDefaillance = PointId is null ? "" : CodePoint + ".";
        LibelleDefaillance = "";
        DefaillanceActive = true;
    }

    [RelayCommand]
    private async Task EnregistrerPointAsync()
    {
        if (Fonction is null)
        {
            Erreur = "Choisissez la fonction du point.";
            return;
        }

        PointControleDto? point = null;
        if (await ExecuterAsync(async () =>
                point = await _api.EnregistrerPointAsync(new PointControleRequete(PointId, Fonction.Id, CodePoint, LibellePoint, PointActif))))
        {
            await ChargerCatalogueAsync(point!.Id);
            Message = "Point de contrôle enregistré.";
        }
    }

    [RelayCommand]
    private async Task EnregistrerDefaillanceAsync()
    {
        if (PointId is not { } pointId)
            return;

        if (await ExecuterAsync(() => _api.EnregistrerDefaillanceAsync(pointId,
                new DefaillanceRequete(DefaillanceId, CodeDefaillance, LibelleDefaillance, DefaillanceActive))))
        {
            await ChargerCatalogueAsync(pointId);
            Message = "Défaillance enregistrée : son niveau est déduit du dernier chiffre du code.";
        }
    }
}
