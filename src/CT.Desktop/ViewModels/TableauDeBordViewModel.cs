using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;
using CT.Shared.Dtos;

namespace CT.Desktop.ViewModels;

public record BarreMois(string Libelle, int Total, int Favorables, double HauteurTotal, double HauteurFavorables);

public partial class TableauDeBordViewModel(ApiClient api) : ViewModelBase
{
    private const double HauteurMax = 180;

    [ObservableProperty] private DateTime _du = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
    [ObservableProperty] private DateTime _au = DateTime.Today;
    [ObservableProperty] private StatistiquesDto? _statistiques;

    public ObservableCollection<BarreMois> Barres { get; } = [];
    public ObservableCollection<DefaillanceFrequenteDto> Defaillances { get; } = [];

    public override Task ChargerAsync() => ActualiserAsync();

    [RelayCommand]
    private Task ActualiserAsync() => ExecuterAsync(async () =>
    {
        var stats = await api.StatistiquesAsync(DateOnly.FromDateTime(Du), DateOnly.FromDateTime(Au));
        Statistiques = stats;

        var max = Math.Max(1, stats.ParMois.Select(m => m.Total).DefaultIfEmpty().Max());
        Barres.Clear();
        foreach (var mois in stats.ParMois)
            Barres.Add(new BarreMois(mois.Libelle, mois.Total, mois.Favorables,
                HauteurMax * mois.Total / max, HauteurMax * mois.Favorables / max));

        Defaillances.Clear();
        foreach (var defaillance in stats.DefaillancesLesPlusFrequentes)
            Defaillances.Add(defaillance);
    });
}
