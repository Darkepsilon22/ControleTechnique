using CT.Shared.Enums;

namespace CT.Shared.Dtos;

public record ControlesParMoisDto(int Annee, int Mois, int Total, int Favorables)
{
    public string Libelle => $"{Mois:00}/{Annee}";
}

public record DefaillanceFrequenteDto(string Code, string Libelle, NiveauDefaillance Niveau, int Nombre);

public record StatistiquesDto(
    DateOnly Du,
    DateOnly Au,
    int TotalControles,
    int Favorables,
    int DefavorablesMajeurs,
    int DefavorablesCritiques,
    double TauxFavorable,
    int ContreVisites,
    int ControlesEnCours,
    IReadOnlyList<ControlesParMoisDto> ParMois,
    IReadOnlyList<DefaillanceFrequenteDto> DefaillancesLesPlusFrequentes);
