using Microsoft.AspNetCore.Mvc;
using Pera.Backend.Datos;
using Pera.Backend.Modelos;

namespace Pera.Backend.Controllers;

[ApiController]
[Route("api/carreras")]
public sealed class CarrerasController(IRepositorioExpedientes repositorio) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<Carrera> Listar()
    {
        return repositorio.ListarCarreras();
    }
}
