namespace CT.Infrastructure.Security;

public interface IHachageMotDePasse
{
    string Hacher(string motDePasse);
    bool Verifier(string motDePasse, string hash);
}

public class BCryptHachageMotDePasse : IHachageMotDePasse
{
    public string Hacher(string motDePasse) => BCrypt.Net.BCrypt.HashPassword(motDePasse);

    public bool Verifier(string motDePasse, string hash) => BCrypt.Net.BCrypt.Verify(motDePasse, hash);
}
