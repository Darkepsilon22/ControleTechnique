using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/proprietaires")]
[Authorize(Roles = Roles.AdministrateurOuReception)]
public class ProprietairesController(IProprietaireService proprietaires) : ControllerBase
{
    [HttpGet]
    public Task<PageResultat<ProprietaireDto>> Rechercher(
        string? search, int page = 1, int taille = Pagination.TaillePageParDefaut, CancellationToken ct = default) =>
        proprietaires.RechercherAsync(search, page, taille, ct);

    [HttpPost]
    [ProducesResponseType<ProprietaireDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProprietaireDto>> Creer(ProprietaireRequete requete, CancellationToken ct)
    {
        var proprietaire = await proprietaires.CreerAsync(requete, ct);
        return Created($"api/proprietaires/{proprietaire.Id}", proprietaire);
    }

    [HttpPut("{id:guid}")]
    public Task<ProprietaireDto> Modifier(Guid id, ProprietaireRequete requete, CancellationToken ct) =>
        proprietaires.ModifierAsync(id, requete, ct);
}
