namespace CT.Shared.Dtos;

public record PageResultat<T>(IReadOnlyList<T> Elements, int Page, int TaillePage, int Total)
{
    public int NombrePages => TaillePage == 0 ? 0 : (int)Math.Ceiling(Total / (double)TaillePage);
}

public static class Pagination
{
    public const int TaillePageParDefaut = 20;
    public const int TaillePageMax = 100;
}
