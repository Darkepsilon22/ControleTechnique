using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.ViewModels;

public record CategorieOption(Guid Id, string Libelle);

public partial class PointsControleViewModel : ViewModelBase
{
    private readonly ApiClient _api;

    [ObservableProperty] private PointControleDto? _pointSelectionne;
    [ObservableProperty] private Guid? _editionId;
    [ObservableProperty] private CategorieOption? _categorie;
    [ObservableProperty] private string _libelle = "";
    [ObservableProperty] private Gravite _gravite;
    [ObservableProperty] private bool _actif = true;

    public ObservableCollection<PointControleDto> Points { get; } = [];
    public ObservableCollection<CategorieOption> Categories { get; } = [];
    public ICollectionView PointsParCategorie { get; }
    public IReadOnlyList<Gravite> Gravites { get; } = Enum.GetValues<Gravite>();
    public string TitreFormulaire => EditionId is null ? "Nouveau point" : "Modifier le point";

    public PointsControleViewModel(ApiClient api)
    {
        _api = api;
        PointsParCategorie = CollectionViewSource.GetDefaultView(Points);
        PointsParCategorie.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PointControleDto.Categorie)));
    }

    public override Task ChargerAsync() => ExecuterAsync(async () =>
    {
        var points = await _api.ListerPointsAsync();
        Points.Clear();
        foreach (var p in points)
            Points.Add(p);

        Categories.Clear();
        foreach (var c in points.Select(p => new CategorieOption(p.CategorieId, p.Categorie)).Distinct().OrderBy(c => c.Libelle))
            Categories.Add(c);
    });

    partial void OnPointSelectionneChanged(PointControleDto? value)
    {
        if (value is null)
            return;
        EditionId = value.Id;
        Categorie = Categories.FirstOrDefault(c => c.Id == value.CategorieId);
        Libelle = value.Libelle;
        Gravite = value.Gravite;
        Actif = value.Actif;
    }

    partial void OnEditionIdChanged(Guid? value) => OnPropertyChanged(nameof(TitreFormulaire));

    [RelayCommand]
    private void Nouveau()
    {
        PointSelectionne = null;
        EditionId = null;
        Libelle = "";
        Gravite = Gravite.Mineur;
        Actif = true;
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (Categorie is null)
        {
            Erreur = "Choisissez une catégorie.";
            return;
        }

        var ok = await ExecuterAsync(() => _api.EnregistrerPointAsync(
            new PointControleRequete(EditionId, Categorie.Id, Libelle, Gravite, Actif)));
        if (ok)
        {
            Nouveau();
            await ChargerAsync();
            Message = "Point de contrôle enregistré.";
        }
    }
}
