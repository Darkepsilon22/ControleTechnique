using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using CT.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/controles")]
[Authorize]
public class ControlesController(IControleService controles, IPvService pv) : ControllerBase
{
    [HttpGet]
    public Task<PageResultat<ControleResumeDto>> Lister(
        StatutControle? statut, string? search, bool mesControles = false,
        int page = 1, int taille = Pagination.TaillePageParDefaut, CancellationToken ct = default) =>
        controles.ListerAsync(statut, search, mesControles ? User.IdUtilisateur() : null, page, taille, ct);

    [HttpGet("{id:guid}")]
    public Task<ControleDto> Obtenir(Guid id, CancellationToken ct) => controles.ObtenirAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Inspecteur)]
    [ProducesResponseType<ControleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ControleDto>> Ouvrir(OuvrirControleRequete requete, CancellationToken ct)
    {
        var controle = await controles.OuvrirAsync(requete, User.IdUtilisateur(), ct);
        return CreatedAtAction(nameof(Obtenir), new { id = controle.Id }, controle);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Inspecteur)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ControleDto> Saisir(Guid id, SaisirControleRequete requete, CancellationToken ct) =>
        controles.SaisirAsync(id, requete, User.IdUtilisateur(), ct);

    [HttpPost("{id:guid}/cloturer")]
    [Authorize(Roles = Roles.Inspecteur)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ControleDto> Cloturer(Guid id, CancellationToken ct) =>
        controles.CloturerAsync(id, User.IdUtilisateur(), ct);

    [HttpGet("{id:guid}/pv")]
    [Produces("application/pdf")]
    public async Task<IActionResult> TelechargerPv(Guid id, CancellationToken ct)
    {
        var (contenu, nomFichier) = await pv.GenererAsync(id, ct);
        return File(contenu, "application/pdf", nomFichier);
    }
}
