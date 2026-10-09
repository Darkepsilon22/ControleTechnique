using System.Text.RegularExpressions;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Domain.Entities;

public partial class PointControle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FonctionId { get; set; }
    public Fonction? Fonction { get; set; }
    public required string Code { get; set; }
    public required string Libelle { get; set; }
    public bool Actif { get; set; } = true;

    public List<Defaillance> Defaillances { get; set; } = [];

    public int NumeroFonction => int.Parse(Code.Split('.')[0]);
    public string Ensemble => EnsembleDe(Code);

    public static string EnsembleDe(string codePoint) => string.Join('.', codePoint.Split('.').Take(2));

    // 8.2.1 : moteurs à allumage commandé, 8.2.2 : moteurs à allumage par compression (annexe I, fonction 8).
    public bool EstApplicable(Energie energie)
    {
        if (Code.StartsWith("8.2.1"))
            return energie is Energie.Essence or Energie.Hybride or Energie.Gpl;
        if (Code.StartsWith("8.2.2"))
            return energie == Energie.Diesel;
        return true;
    }

    public static void VerifierCode(string code)
    {
        if (!FormatCode().IsMatch(code))
            throw new RegleMetierException($"Le code de point « {code} » ne respecte pas le format de l'annexe I (ex. 1.1.13).");
    }

    [GeneratedRegex(@"^\d{1,2}\.\d{1,2}\.\d{1,2}(\.\d{1,2})?$")]
    private static partial Regex FormatCode();
}
