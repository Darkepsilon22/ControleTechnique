using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<ConnexionReponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConnexionReponse>> Login(ConnexionRequete requete, CancellationToken ct)
    {
        var reponse = await auth.ConnecterAsync(requete, ct);
        return reponse is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou mot de passe incorrect.")
            : Ok(reponse);
    }
}
