using System.Text.RegularExpressions;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Domain.Entities;

public partial class Defaillance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PointControleId { get; set; }
    public PointControle? PointControle { get; set; }
    public required string Code { get; set; }
    public required string Libelle { get; set; }
    public NiveauDefaillance Niveau { get; set; }
    public bool Actif { get; set; } = true;

    public string CodePoint => CodePointDe(Code);

    public static string CodePointDe(string codeDefaillance) =>
        string.Join('.', codeDefaillance.Split('.').SkipLast(2));

    // Dans l'annexe I, le dernier chiffre du code donne le niveau : 1 mineure, 2 majeure, 3 critique.
    public static NiveauDefaillance NiveauDepuisCode(string code, string codePoint)
    {
        if (!FormatCode().IsMatch(code) || CodePointDe(code) != codePoint)
            throw new RegleMetierException($"Le code « {code} » doit être de la forme {codePoint}.x.n, avec n = 1, 2 ou 3.");
        return (NiveauDefaillance)(code[^1] - '0');
    }

    [GeneratedRegex(@"^\d{1,2}\.\d{1,2}\.\d{1,2}(\.\d{1,2})?\.[a-z]\.[123]$")]
    private static partial Regex FormatCode();
}
