using CT.Domain.Entities;
using D = CT.Domain.Enums;
using S = CT.Shared.Enums;

namespace CT.Tests.Domain;

public class VehiculeEtContratsTests
{
    [Theory]
    [InlineData("ab-123-cd", "AB-123-CD")]
    [InlineData("  ab 123 cd ", "AB123CD")]
    public void L_immatriculation_est_normalisee(string saisie, string attendu)
    {
        Assert.Equal(attendu, Vehicule.NormaliserImmatriculation(saisie));
    }

    [Theory]
    [InlineData(typeof(D.Role), typeof(S.Role))]
    [InlineData(typeof(D.Gravite), typeof(S.Gravite))]
    [InlineData(typeof(D.EtatPoint), typeof(S.EtatPoint))]
    [InlineData(typeof(D.StatutControle), typeof(S.StatutControle))]
    [InlineData(typeof(D.ResultatControle), typeof(S.ResultatControle))]
    [InlineData(typeof(D.TypeVehicule), typeof(S.TypeVehicule))]
    [InlineData(typeof(D.Energie), typeof(S.Energie))]
    public void Les_enumerations_partagees_correspondent_a_celles_du_domaine(Type domaine, Type partage)
    {
        Assert.Equal(Enum.GetNames(domaine), Enum.GetNames(partage));
        Assert.Equal(
            Enum.GetValues(domaine).Cast<object>().Select(Convert.ToInt32),
            Enum.GetValues(partage).Cast<object>().Select(Convert.ToInt32));
    }
}
