using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/utilisateurs")]
[Authorize(Roles = Roles.Administrateur)]
public class UtilisateursController(IUtilisateurService utilisateurs) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UtilisateurDto>> Lister(CancellationToken ct) => utilisateurs.ListerAsync(ct);

    [HttpPost]
    [ProducesResponseType<UtilisateurDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UtilisateurDto>> Creer(CreerUtilisateurRequete requete, CancellationToken ct)
    {
        var utilisateur = await utilisateurs.CreerAsync(requete, ct);
        return Created($"api/utilisateurs/{utilisateur.Id}", utilisateur);
    }

    [HttpPut("{id:guid}")]
    public Task<UtilisateurDto> Modifier(Guid id, ModifierUtilisateurRequete requete, CancellationToken ct) =>
        utilisateurs.ModifierAsync(id, requete, User.IdUtilisateur(), ct);
}
