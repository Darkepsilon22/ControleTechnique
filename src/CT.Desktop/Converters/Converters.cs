using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CT.Shared.Enums;

namespace CT.Desktop.Converters;

public class LibelleEnumConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Libelles = new()
    {
        ["NonConforme"] = "Défaillance",
        ["NonApplicable"] = "Non applicable",
        ["Cloture"] = "Clôturé",
        ["Favorable"] = "Favorable (A)",
        ["DefavorableMajeur"] = "Défavorable (S)",
        ["DefavorableCritique"] = "Défavorable (R)",
        ["Reception"] = "Réception",
        ["VoitureParticuliere"] = "Voiture particulière (M1)",
        ["UtilitaireLeger"] = "Utilitaire léger (N1)",
        ["Electrique"] = "Électrique",
        ["Gpl"] = "GPL",
        ["AJour"] = "À jour",
        ["ControleEnRetard"] = "En retard",
        ["ContreVisiteAFaire"] = "Contre-visite à faire",
        ["CirculationInterdite"] = "Circulation interdite"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? "—" : Libelles.GetValueOrDefault(value.ToString()!, value.ToString()!);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class EnumEgalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter : Binding.DoNothing;
}

public class NullVersVisibiliteConverter : IValueConverter
{
    public bool Inverser { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value is not null && (value is not string texte || texte.Length > 0);
        return visible ^ Inverser ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class CouleurResultatConverter : IValueConverter
{
    private static readonly Brush Vert = Gele("#1E8E3E");
    private static readonly Brush Rouge = Gele("#D93025");
    private static readonly Brush Orange = Gele("#E37400");
    private static readonly Brush Gris = Gele("#80868B");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ResultatControle.Favorable or EtatPoint.Conforme or StatutEcheance.AJour => Vert,
        ResultatControle.DefavorableCritique or EtatPoint.NonConforme or NiveauDefaillance.Critique
            or StatutEcheance.ControleEnRetard or StatutEcheance.CirculationInterdite => Rouge,
        ResultatControle.DefavorableMajeur or NiveauDefaillance.Majeure or StatutControle.Brouillon
            or StatutEcheance.ContreVisiteAFaire => Orange,
        _ => Gris
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Gele(string couleur)
    {
        var pinceau = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleur));
        pinceau.Freeze();
        return pinceau;
    }
}
