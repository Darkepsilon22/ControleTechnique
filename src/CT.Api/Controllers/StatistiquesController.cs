using CT.Api.Securite;
using CT.Api.Services;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Controllers;

[ApiController]
[Route("api/statistiques")]
[Authorize(Roles = Roles.Administrateur)]
public class StatistiquesController(IStatistiqueService statistiques) : ControllerBase
{
    [HttpGet("resume")]
    public Task<StatistiquesDto> Resume(DateOnly? du, DateOnly? au, CancellationToken ct) =>
        statistiques.ResumeAsync(du, au, ct);
}
