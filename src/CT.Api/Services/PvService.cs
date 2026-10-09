using CT.Domain.Exceptions;
using CT.Shared.Dtos;
using CT.Shared.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CT.Api.Services;

public interface IPvService
{
    Task<(byte[] Contenu, string NomFichier)> GenererAsync(Guid controleId, CancellationToken ct);
}

public class PvService(IControleService controles) : IPvService
{
    private record LigneDefaillance(string Code, string Libelle, string Point, string? Commentaire);

    public async Task<(byte[] Contenu, string NomFichier)> GenererAsync(Guid controleId, CancellationToken ct)
    {
        var controle = await controles.ObtenirAsync(controleId, ct);
        if (controle.Statut != StatutControle.Cloture)
            throw new ConflitException("Le procès-verbal n'est disponible qu'après la clôture du contrôle.");

        var contenu = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(9.5f));

            page.Header().Column(col =>
            {
                col.Item().Text(controle.EstContreVisite
                    ? "Procès-verbal de contre-visite"
                    : "Procès-verbal de contrôle technique périodique").FontSize(17).Bold();
                col.Item().Text($"N° {controle.Id.ToString()[..8].ToUpperInvariant()} — {controle.DateControle:dd/MM/yyyy HH:mm}")
                    .FontColor(Colors.Grey.Darken1);
                if (controle.EstContreVisite)
                    col.Item().Text($"Contre-visite du contrôle technique périodique du {controle.DateControlePeriodique:dd/MM/yyyy}")
                        .FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(10);
                col.Item().Element(c => Resultat(c, controle));
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Element(c => Bloc(c, "Véhicule",
                        $"{controle.Immatriculation} — {controle.Vehicule}",
                        $"VIN : {controle.NumeroChassis}",
                        $"Catégorie : {(controle.TypeVehicule == TypeVehicule.VoitureParticuliere ? "M1" : "N1")} — Énergie : {controle.Energie}",
                        $"1re immatriculation : {controle.DatePremiereImmatriculation:dd/MM/yyyy}",
                        $"Kilométrage : {controle.Kilometrage:N0} km"));
                    row.ConstantItem(10);
                    row.RelativeItem(2).Element(c => Bloc(c, "Propriétaire", controle.ProprietaireNom));
                    row.ConstantItem(10);
                    row.RelativeItem(2).Element(c => Bloc(c, "Contrôleur", controle.InspecteurNom,
                        $"Clôturé le {controle.ClotureLe:dd/MM/yyyy HH:mm}"));
                });

                foreach (var (niveau, titre) in new[]
                {
                    (NiveauDefaillance.Critique, "Défaillances critiques"),
                    (NiveauDefaillance.Majeure, "Défaillances majeures"),
                    (NiveauDefaillance.Mineure, "Défaillances mineures")
                })
                {
                    var lignes = controle.Resultats
                        .SelectMany(r => r.Defaillances.Where(d => d.Niveau == niveau)
                            .Select(d => new LigneDefaillance(d.Code, d.Libelle, $"{r.Code} {r.Libelle}", r.Commentaire)))
                        .ToList();
                    col.Item().Element(c => Defaillances(c, titre, niveau, lignes));
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                t.Span("Établi selon l'arrêté du 18 juin 1991 modifié — application de démonstration, non agréée — page ");
                t.CurrentPageNumber();
            });
        })).GeneratePdf();

        return (contenu, $"PV_{controle.Immatriculation}_{controle.DateControle:yyyyMMdd}.pdf");
    }

    private static void Resultat(IContainer conteneur, ControleDto controle)
    {
        var (titre, fond, texte) = controle.Resultat switch
        {
            ResultatControle.Favorable => ("FAVORABLE (A)", Colors.Green.Lighten4, Colors.Green.Darken3),
            ResultatControle.DefavorableMajeur => ("DÉFAVORABLE POUR DÉFAILLANCES MAJEURES (S)", Colors.Orange.Lighten4, Colors.Orange.Darken4),
            _ => ("DÉFAVORABLE POUR DÉFAILLANCES CRITIQUES (R)", Colors.Red.Lighten4, Colors.Red.Darken3)
        };

        conteneur.Background(fond).Padding(10).Column(col =>
        {
            col.Item().Text(titre).FontSize(14).Bold().FontColor(texte);
            switch (controle.Resultat)
            {
                case ResultatControle.Favorable:
                    col.Item().Text($"Prochain contrôle technique périodique avant le {controle.DateFinValidite:dd/MM/yyyy}.");
                    break;
                case ResultatControle.DefavorableMajeur:
                    col.Item().Text($"Contre-visite à effectuer au plus tard le {controle.DateLimiteContreVisite:dd/MM/yyyy}.");
                    break;
                default:
                    col.Item().Text($"Le véhicule ne peut circuler que jusqu'à minuit le {controle.DateFinValidite:dd/MM/yyyy}.").Bold();
                    col.Item().Text($"Contre-visite à effectuer au plus tard le {controle.DateLimiteContreVisite:dd/MM/yyyy}.");
                    break;
            }
        });
    }

    private static void Bloc(IContainer conteneur, string titre, params string[] lignes)
    {
        conteneur.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Column(col =>
        {
            col.Item().Text(titre).Bold().FontColor(Colors.Grey.Darken2);
            foreach (var ligne in lignes)
                col.Item().Text(ligne);
        });
    }

    private static void Defaillances(IContainer conteneur, string titre, NiveauDefaillance niveau, List<LigneDefaillance> lignes)
    {
        var couleur = niveau switch
        {
            NiveauDefaillance.Critique => Colors.Red.Darken3,
            NiveauDefaillance.Majeure => Colors.Orange.Darken4,
            _ => Colors.Grey.Darken3
        };

        conteneur.Column(col =>
        {
            col.Item().PaddingBottom(4).Text($"{titre} ({lignes.Count})").Bold().FontSize(11).FontColor(couleur);
            if (lignes.Count == 0)
            {
                col.Item().Text("Aucune.").FontColor(Colors.Grey.Darken1);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);
                    c.RelativeColumn(4);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                });
                table.Header(h =>
                {
                    foreach (var entete in new[] { "Code", "Défaillance", "Point de contrôle", "Commentaire" })
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(entete).Bold();
                });
                foreach (var l in lignes)
                {
                    table.Cell().Padding(3).Text(l.Code);
                    table.Cell().Padding(3).Text(l.Libelle);
                    table.Cell().Padding(3).Text(l.Point);
                    table.Cell().Padding(3).Text(l.Commentaire ?? "");
                }
            });
        });
    }
}
