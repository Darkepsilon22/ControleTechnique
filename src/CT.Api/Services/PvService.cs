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
    public async Task<(byte[] Contenu, string NomFichier)> GenererAsync(Guid controleId, CancellationToken ct)
    {
        var controle = await controles.ObtenirAsync(controleId, ct);
        if (controle.Statut != StatutControle.Cloture)
            throw new ConflitException("Le procès-verbal n'est disponible qu'après la clôture du contrôle.");

        var contenu = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text(controle.EstContreVisite ? "Procès-verbal de contre-visite" : "Procès-verbal de contrôle technique").FontSize(18).Bold();
                col.Item().Text($"N° {controle.Id.ToString()[..8].ToUpperInvariant()} — {controle.DateControle:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                if (controle.DateControleInitial is { } dateInitiale)
                    col.Item().Text($"Contre-visite du contrôle défavorable du {dateInitiale:dd/MM/yyyy}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(15).Column(col =>
            {
                col.Spacing(12);
                col.Item().Element(c => Resultat(c, controle));
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => Bloc(c, "Véhicule",
                        $"{controle.Immatriculation}", controle.Vehicule, $"Kilométrage : {controle.Kilometrage:N0} km"));
                    row.ConstantItem(15);
                    row.RelativeItem().Element(c => Bloc(c, "Propriétaire", controle.ProprietaireNom));
                    row.ConstantItem(15);
                    row.RelativeItem().Element(c => Bloc(c, "Inspecteur", controle.InspecteurNom,
                        $"Clôturé le {controle.ClotureLe:dd/MM/yyyy HH:mm}"));
                });
                col.Item().Element(c => PointsNonConformes(c, controle));
                if (!string.IsNullOrWhiteSpace(controle.Observations))
                    col.Item().Element(c => Bloc(c, "Observations (points mineurs)", controle.Observations.Split(Environment.NewLine)));
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Document fictif généré par l'application de démonstration ControleTechnique — page ");
                t.CurrentPageNumber();
            });
        })).GeneratePdf();

        var nomFichier = $"PV_{controle.Immatriculation}_{controle.DateControle:yyyyMMdd}.pdf";
        return (contenu, nomFichier);
    }

    private static void Resultat(IContainer conteneur, ControleDto controle)
    {
        var favorable = controle.Resultat == ResultatControle.Favorable;
        conteneur.Background(favorable ? Colors.Green.Lighten4 : Colors.Red.Lighten4).Padding(12).Column(col =>
        {
            col.Item().Text(favorable ? "FAVORABLE" : "DÉFAVORABLE").FontSize(16).Bold()
                .FontColor(favorable ? Colors.Green.Darken3 : Colors.Red.Darken3);
            col.Item().Text(controle.DateFinValidite is { } fin
                ? $"Valide jusqu'au {fin:dd/MM/yyyy}"
                : controle.EstContreVisite
                    ? "Aucune date de validité : un nouveau contrôle complet est nécessaire."
                    : "Aucune date de validité : une contre-visite est nécessaire.");
        });
    }

    private static void Bloc(IContainer conteneur, string titre, params string[] lignes)
    {
        conteneur.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text(titre).Bold().FontColor(Colors.Grey.Darken2);
            foreach (var ligne in lignes)
                col.Item().Text(ligne);
        });
    }

    private static void PointsNonConformes(IContainer conteneur, ControleDto controle)
    {
        var nonConformes = controle.Resultats.Where(r => r.Etat == EtatPoint.NonConforme).ToList();
        conteneur.Column(col =>
        {
            col.Item().PaddingBottom(5).Text($"Points non conformes ({nonConformes.Count})").Bold().FontSize(12);
            if (nonConformes.Count == 0)
            {
                col.Item().Text("Aucun point non conforme.");
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(3);
                    c.RelativeColumn(1);
                    c.RelativeColumn(3);
                });
                table.Header(h =>
                {
                    foreach (var entete in new[] { "Catégorie", "Point", "Gravité", "Commentaire" })
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(entete).Bold();
                });
                foreach (var r in nonConformes)
                {
                    table.Cell().Padding(4).Text(r.Categorie);
                    table.Cell().Padding(4).Text(r.Libelle);
                    table.Cell().Padding(4).Text(r.Gravite.ToString());
                    table.Cell().Padding(4).Text(r.Commentaire ?? "");
                }
            });
        });
    }
}
