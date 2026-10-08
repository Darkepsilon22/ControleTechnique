using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/vehicules")]
[Authorize]
public class VehiculesController(IVehiculeService vehicules) : ControllerBase
{
    [HttpGet]
    public Task<PageResultat<VehiculeDto>> Rechercher(
        string? search, int page = 1, int taille = Pagination.TaillePageParDefaut, CancellationToken ct = default) =>
        vehicules.RechercherAsync(search, page, taille, ct);

    [HttpGet("{id:guid}")]
    public Task<VehiculeDto> Obtenir(Guid id, CancellationToken ct) => vehicules.ObtenirAsync(id, ct);

    [HttpGet("{id:guid}/controles")]
    public Task<IReadOnlyList<ControleResumeDto>> Historique(Guid id, CancellationToken ct) =>
        vehicules.HistoriqueAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = Roles.AdministrateurOuReception)]
    [ProducesResponseType<VehiculeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehiculeDto>> Creer(VehiculeRequete requete, CancellationToken ct)
    {
        var vehicule = await vehicules.CreerAsync(requete, ct);
        return CreatedAtAction(nameof(Obtenir), new { id = vehicule.Id }, vehicule);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdministrateurOuReception)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<VehiculeDto> Modifier(Guid id, VehiculeRequete requete, CancellationToken ct) =>
        vehicules.ModifierAsync(id, requete, ct);
}
