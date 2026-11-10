using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Practicas.Application.Errores;
using Practicas.Domain;

namespace Practicas.Api.Errores;

/// <summary>
/// Traduce las excepciones esperadas del negocio a respuestas ProblemDetails (RFC 9457) y oculta
/// los detalles internos de los errores inesperados (OWASP A05: no filtrar trazas ni rutas).
/// </summary>
public sealed partial class ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails detalle = exception switch
        {
            EvaluacionInvalidaException evaluacion => new ValidationProblemDetails(evaluacion.Errores.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La evaluación no es válida"
            },
            CorreoDuplicadoException conflicto => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = conflicto.Titulo,
                Detail = conflicto.Message
            },
            ReglaNegocioException regla => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = regla.Titulo,
                Detail = regla.Message
            },
            _ => null!
        };

        if (detalle is null)
        {
            LogErrorNoControlado(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
            detalle = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno",
                Detail = "Ocurrió un error inesperado. Intenta de nuevo más tarde."
            };
        }

        httpContext.Response.StatusCode = detalle.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = detalle,
            Exception = exception
        });
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Error no controlado procesando {Metodo} {Ruta}")]
    private static partial void LogErrorNoControlado(ILogger logger, Exception exception, string metodo, string ruta);
}
