using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/points-controle")]
[Authorize]
public class PointsControleController(IPointControleService points) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PointControleDto>> Lister(CancellationToken ct) => points.ListerAsync(ct);

    [HttpPost]
    [Authorize(Roles = Roles.Administrateur)]
    [ProducesResponseType<PointControleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PointControleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PointControleDto>> Enregistrer(PointControleRequete requete, CancellationToken ct)
    {
        var (point, cree) = await points.EnregistrerAsync(requete, ct);
        return cree ? Created($"api/points-controle/{point.Id}", point) : Ok(point);
    }
}
