using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Practicas.Api.Contratos;
using Practicas.Application.Autenticacion;
using Practicas.Application.Practicantes;
using Practicas.Api.Seguridad;
using Practicas.Domain;

namespace Practicas.Api.Controllers;

[ApiController]
[Route("api/practicantes")]
[Produces("application/json")]
[Authorize(Roles = Roles.Analista)]
public sealed class PracticantesController(IPracticanteService servicio) : ControllerBase
{
    /// <summary>Registra un practicante con su hoja de vida (formulario público).</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasLimite.Registro)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitudBytes)]
    [ProducesResponseType<RegistroPracticanteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RegistroPracticanteResponse>> Registrar(
        [FromForm] RegistrarPracticanteRequest solicitud, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var registro = await servicio.RegistrarAsync(solicitud.AComando(), cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = registro.Id }, registro);
    }

    /// <summary>Lista los practicantes, opcionalmente filtrados por estado o por texto.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PracticanteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PracticanteResponse>>> Listar(
        [FromQuery] EstadoPracticante? estado, [FromQuery] string? busqueda, CancellationToken cancellationToken)
        => Ok(await servicio.ListarAsync(estado, busqueda, cancellationToken));

    /// <summary>Detalle de un practicante.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PracticanteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticanteResponse>> Obtener(int id, CancellationToken cancellationToken)
    {
        var practicante = await servicio.ObtenerAsync(id, cancellationToken);
        return practicante is null ? NoEncontrado(id) : Ok(practicante);
    }

    /// <summary>
    /// Evalúa la hoja de vida con la rúbrica de calidad y marca al practicante como "Viable" o "No viable".
    /// No se puede decidir sin calificar todos los criterios; "No viable" exige motivos y una decisión
    /// contraria a la recomendación de la rúbrica exige justificación.
    /// </summary>
    [HttpPut("{id:int}/evaluacion")]
    [Consumes("application/json")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType<PracticanteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticanteResponse>> Evaluar(
        int id, [FromBody] EvaluarPracticanteRequest solicitud, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var practicante = await servicio.EvaluarAsync(id, solicitud.AComando(Evaluador), cancellationToken);
        return practicante is null ? NoEncontrado(id) : Ok(practicante);
    }

    /// <summary>
    /// Guarda el avance de la evaluación mientras el analista califica (criterios, decisión tentativa,
    /// motivos y comentario). No cambia el estado del practicante; se descarta al guardar la evaluación.
    /// </summary>
    [HttpPut("{id:int}/evaluacion/borrador")]
    [Consumes("application/json")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType<BorradorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BorradorResponse>> GuardarBorrador(
        int id, [FromBody] BorradorEvaluacionRequest solicitud, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var borrador = await servicio.GuardarBorradorAsync(id, solicitud.AComando(Evaluador), cancellationToken);
        return borrador is null ? NoEncontrado(id) : Ok(borrador);
    }

    /// <summary>Descarta el avance guardado de la evaluación.</summary>
    [HttpDelete("{id:int}/evaluacion/borrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescartarBorrador(int id, CancellationToken cancellationToken) =>
        await servicio.DescartarBorradorAsync(id, cancellationToken) ? NoContent() : NoEncontrado(id);

    /// <summary>Nombre del analista autenticado, tomado del token (nunca del cuerpo de la solicitud).</summary>
    private string Evaluador => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "Analista";

    /// <summary>Descarga la hoja de vida (PDF) del practicante.</summary>
    [HttpGet("{id:int}/hoja-de-vida")]
    [Produces("application/pdf", "application/problem+json")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarHojaDeVida(int id, CancellationToken cancellationToken)
    {
        var archivo = await servicio.ObtenerHojaDeVidaAsync(id, cancellationToken);
        if (archivo is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Hoja de vida no encontrada",
                detail: $"No se encontró la hoja de vida del practicante {id}.");
        }

        return File(archivo.Contenido, "application/pdf", archivo.NombreDescarga);
    }

    private ObjectResult NoEncontrado(int id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Practicante no encontrado",
        detail: $"No existe un practicante con id {id}.");

    /// <summary>5 MB del archivo más un margen para los campos del formulario.</summary>
    private const long LimiteSolicitudBytes = 6 * 1024 * 1024;
}
