using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Practicas.Api.Errors;

/// <summary>
/// Convierte las excepciones de dominio en respuestas ProblemDetails (RFC 9457)
/// y evita exponer detalles internos cuando ocurre un error inesperado.
/// </summary>
public sealed class ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails detalle;

        if (exception is ExcepcionDominio dominio)
        {
            detalle = new ProblemDetails
            {
                Status = dominio.CodigoHttp,
                Title = dominio.Titulo,
                Detail = dominio.Message
            };
        }
        else
        {
            logger.LogError(exception, "Error no controlado procesando {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);

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
}
