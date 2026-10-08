using System.Security.Claims;
using System.Text;
using CT.Api.Configuration;
using CT.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CT.Api.Securite;

public interface IJetonService
{
    (string Jeton, DateTime ExpireLe) Creer(Utilisateur utilisateur);
}

public class JetonService(IOptions<JwtOptions> options, TimeProvider horloge) : IJetonService
{
    public const string ClaimId = "sub";
    public const string ClaimNom = "name";
    public const string ClaimRole = "role";

    public (string Jeton, DateTime ExpireLe) Creer(Utilisateur utilisateur)
    {
        var jwt = options.Value;
        var expireLe = horloge.GetUtcNow().UtcDateTime.AddHours(jwt.DureeHeures);

        var descripteur = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expireLe,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimId, utilisateur.Id.ToString()),
                new Claim(ClaimNom, utilisateur.NomComplet),
                new Claim("email", utilisateur.Email),
                new Claim(ClaimRole, utilisateur.Role.ToString())
            ]),
            SigningCredentials = new SigningCredentials(CleDeSignature(jwt), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descripteur), expireLe);
    }

    public static SymmetricSecurityKey CleDeSignature(JwtOptions jwt)
    {
        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException(
                "La clé JWT est absente ou trop courte (32 caractères minimum). " +
                "Depuis src/CT.Api : dotnet user-secrets set \"Jwt:Key\" \"<clé>\"");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
    }
}
