using CT.Domain.Rules;

namespace CT.Tests.Domain;

public class RegleContreVisiteTests
{
    private static readonly string[] Points =
    [
        "0.1.1", "0.2.1", "1.1.13", "1.2.2", "2.1.1", "3.4.1", "4.3.1", "4.4.1",
        "5.1.3", "5.2.3", "5.3.2", "6.1.1", "6.1.2", "6.1.3", "7.1.2", "7.11.1", "8.1.1", "8.2.12", "8.4.1"
    ];

    private static IReadOnlySet<string> AReverifier(params string[] defaillances) =>
        RegleContreVisite.PointsAReverifier(defaillances, Points);

    [Fact]
    public void La_fonction_0_et_le_compteur_kilometrique_sont_toujours_reverifies()
    {
        var points = AReverifier("3.4.1.a.2");

        Assert.Contains("0.1.1", points);
        Assert.Contains("0.2.1", points);
        Assert.Contains("7.11.1", points);
    }

    [Fact]
    public void Une_defaillance_de_freinage_entraine_la_reverification_de_toute_la_fonction_1()
    {
        var points = AReverifier("1.1.13.a.2");

        Assert.Contains("1.1.13", points);
        Assert.Contains("1.2.2", points);
        Assert.DoesNotContain("2.1.1", points);
    }

    [Fact]
    public void Pour_les_autres_fonctions_seul_l_ensemble_de_points_est_reverifie()
    {
        var points = AReverifier("4.3.1.a.2");

        Assert.Contains("4.3.1", points);
        Assert.DoesNotContain("4.4.1", points);
    }

    [Fact]
    public void Une_defaillance_de_suspension_entraine_les_ensembles_5_1_et_5_3()
    {
        var points = AReverifier("5.3.2.a.2");

        Assert.Contains("5.1.3", points);
        Assert.Contains("5.3.2", points);
        Assert.DoesNotContain("5.2.3", points);
    }

    [Fact]
    public void Une_defaillance_d_emissions_entraine_les_ensembles_8_1_et_8_2_et_l_echappement()
    {
        var points = AReverifier("8.2.12.a.2");

        Assert.Superset(new HashSet<string> { "8.1.1", "8.2.12", "6.1.2", "6.1.3" }, points.ToHashSet());
        Assert.DoesNotContain("8.4.1", points);
    }

    [Fact]
    public void Un_point_de_la_fonction_6_n_entraine_que_lui_meme()
    {
        var points = AReverifier("6.1.1.c.2");

        Assert.Contains("6.1.1", points);
        Assert.DoesNotContain("6.1.2", points);
    }

    [Fact]
    public void Un_numero_d_identification_non_conforme_entraine_un_controle_complet()
    {
        Assert.Equal(Points.ToHashSet(), AReverifier("0.2.1.b.2").ToHashSet());
    }
}
