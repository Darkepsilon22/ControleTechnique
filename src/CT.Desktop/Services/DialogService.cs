using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CT.Desktop.Services;

public interface IDialogService
{
    bool Confirmer(string message, string titre = "Confirmation");
    void Informer(string message, string titre = "Information");
    void OuvrirFichier(byte[] contenu, string nomFichier);
}

public class DialogService : IDialogService
{
    public bool Confirmer(string message, string titre = "Confirmation") =>
        MessageBox.Show(message, titre, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Informer(string message, string titre = "Information") =>
        MessageBox.Show(message, titre, MessageBoxButton.OK, MessageBoxImage.Information);

    public void OuvrirFichier(byte[] contenu, string nomFichier)
    {
        var chemin = Path.Combine(Path.GetTempPath(), "ControleTechnique", nomFichier);
        Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);
        File.WriteAllBytes(chemin, contenu);
        Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
    }
}
