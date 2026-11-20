using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pera.Backend.Datos;
using Pera.Backend.Modelos;

namespace Pera.Backend.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "CoordinacionPERA")]
public sealed class TutoriasController(
    IRepositorioExpedientes expedientes, IRepositorioTutorias tutorias) : ControllerBase
{
    [HttpGet("tutores")]
    public IReadOnlyList<Tutor> ListarDisponibles() => tutorias.ListarActivos();

    [HttpGet("expedientes/{carnet}/tutores")]
    public IActionResult Consultar(string carnet)
    {
        var expediente = expedientes.BuscarPorCarnet(carnet.Trim());
        if (expediente is null)
            return NotFound(new { mensaje = "No se encontró el expediente." });

        return Ok(tutorias.Consultar(expediente.Carnet));
    }

    [HttpPost("expedientes/{carnet}/tutores")]
    public IActionResult Asignar(string carnet, SolicitudAsignacion solicitud)
    {
        var expediente = expedientes.BuscarPorCarnet(carnet.Trim());
        if (expediente is null)
            return NotFound(new { mensaje = "No se encontró el expediente." });

        var tutor = tutorias.ListarActivos().FirstOrDefault(t => t.Id == solicitud.TutorId);
        if (tutor is null)
            return BadRequest(new { mensaje = "Seleccioná un tutor activo del catálogo." });

        var area = Regex.Replace(solicitud.AreaRefuerzo?.Trim() ?? "", @"\s+", " ").Normalize();
        if (area.Length == 0)
            return BadRequest(new { mensaje = "Indicá el área a reforzar." });

        var responsable = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(responsable)) return Forbid();

        var asignacion = tutorias.Asignar(expediente.Carnet, tutor, area, responsable);
        if (asignacion is null)
            return Conflict(new { mensaje = "El estudiante ya tiene a este tutor en esa área." });

        return StatusCode(StatusCodes.Status201Created, asignacion);
    }
}
