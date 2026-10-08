namespace CT.Api.Mapping;

public static class Enums
{
    public static TCible Vers<TCible>(Enum valeur) where TCible : struct, Enum =>
        Enum.Parse<TCible>(valeur.ToString());

    public static TCible? VersNullable<TCible>(Enum? valeur) where TCible : struct, Enum =>
        valeur is null ? null : Vers<TCible>(valeur);
}
