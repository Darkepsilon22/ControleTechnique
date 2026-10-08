using CT.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CT.Api.Erreurs;

public class GestionnaireExceptions(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexte, Exception exception, CancellationToken ct)
    {
        var (statut, titre) = exception switch
        {
            RegleMetierException => (StatusCodes.Status400BadRequest, "Règle métier non respectée"),
            IntrouvableException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
            ConflitException => (StatusCodes.Status409Conflict, "Conflit"),
            AccesInterditException => (StatusCodes.Status403Forbidden, "Accès interdit"),
            _ => (0, "")
        };

        if (statut == 0)
            return false;

        contexte.Response.StatusCode = statut;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexte,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = statut, Title = titre, Detail = exception.Message }
        });
    }
}
