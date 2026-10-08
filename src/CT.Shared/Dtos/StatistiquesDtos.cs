namespace CT.Shared.Dtos;

public record ControlesParMoisDto(int Annee, int Mois, int Total, int Favorables)
{
    public string Libelle => $"{Mois:00}/{Annee}";
}

public record PointNonConformeDto(string Libelle, string Categorie, int Nombre);

public record StatistiquesDto(
    DateOnly Du,
    DateOnly Au,
    int TotalControles,
    int Favorables,
    int Defavorables,
    double TauxFavorable,
    int ControlesEnCours,
    IReadOnlyList<ControlesParMoisDto> ParMois,
    IReadOnlyList<PointNonConformeDto> PointsLesPlusNonConformes);
