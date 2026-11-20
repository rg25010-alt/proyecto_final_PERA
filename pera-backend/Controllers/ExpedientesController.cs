using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pera.Backend.Datos;
using Pera.Backend.Modelos;

namespace Pera.Backend.Controllers;

[ApiController]
[Route("api/expedientes")]
public sealed class ExpedientesController(IRepositorioExpedientes repositorio) : ControllerBase
{
    [HttpPost]
    public ActionResult<Expediente> Crear(CrearExpedienteSolicitud solicitud)
    {
        var carnet = solicitud.Carnet?.Trim().ToUpperInvariant() ?? "";
        var nombre = solicitud.Nombre?.Trim() ?? "";

        if (!Regex.IsMatch(carnet, "^[A-Z0-9]{1,20}$"))
            ModelState.AddModelError("carnet", "El carné debe tener de 1 a 20 letras o números.");

        if (nombre.Length is 0 or > 150)
            ModelState.AddModelError("nombre", "Ingresá un nombre de entre 1 y 150 caracteres.");

        if (!repositorio.ListarCarreras().Any(carrera => carrera.Id == solicitud.CarreraId))
            ModelState.AddModelError("carreraId", "Seleccioná una carrera válida.");

        if (solicitud.Cum is not decimal cum || cum < 0 || cum > 10 || decimal.Round(cum, 2) != cum)
            ModelState.AddModelError("cum", "Ingresá un CUM entre 0 y 10 con hasta dos decimales.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // PostgreSQL calculará el tope de UV cuando se integre el motor de reglas.
        var expediente = new Expediente(
            carnet, nombre, solicitud.CarreraId!.Value, solicitud.Cum!.Value, null, "En Curso");

        if (!repositorio.Guardar(expediente))
            return Conflict(new { mensaje = "Ya existe un expediente con este carné." });

        return CreatedAtAction(nameof(Buscar), new { carnet }, expediente);
    }

    [HttpGet("{carnet}")]
    public ActionResult<Expediente> Buscar(string carnet)
    {
        var expediente = repositorio.BuscarPorCarnet(carnet.Trim());

        if (expediente is null)
            return NotFound(new { mensaje = "No se encontró un expediente con este carné." });

        return expediente;
    }
}
