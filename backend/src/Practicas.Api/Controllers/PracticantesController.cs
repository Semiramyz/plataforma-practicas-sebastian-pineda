using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practicas.Api.Auth;
using Practicas.Api.Contracts;
using Practicas.Api.Domain;
using Practicas.Api.Services;

namespace Practicas.Api.Controllers;

[ApiController]
[Route("api/practicantes")]
[Produces("application/json")]
[Authorize(Roles = Roles.Analista)]
public class PracticantesController(IPracticanteService servicio) : ControllerBase
{
    /// <summary>Registra un practicante con su hoja de vida (formulario público).</summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitudBytes)]
    [ProducesResponseType<RegistroPracticanteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistroPracticanteResponse>> Registrar(
        [FromForm] RegistrarPracticanteRequest solicitud, CancellationToken cancellationToken)
    {
        var registro = await servicio.RegistrarAsync(solicitud, cancellationToken);
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

    /// <summary>Marca al practicante como "Viable" o "No viable".</summary>
    [HttpPatch("{id:int}/estado")]
    [Consumes("application/json")]
    [ProducesResponseType<PracticanteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticanteResponse>> ActualizarEstado(
        int id, [FromBody] ActualizarEstadoRequest solicitud, CancellationToken cancellationToken)
    {
        var practicante = await servicio.ActualizarEstadoAsync(id, solicitud.Estado!.Value, cancellationToken);
        return practicante is null ? NoEncontrado(id) : Ok(practicante);
    }

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
