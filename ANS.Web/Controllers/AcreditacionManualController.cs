using ANS.Web.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANS.Web.Controllers
{
    public class AcreditacionManualController : Controller
    {
        private readonly ServicioAcreditacionManual _servicio;

        public AcreditacionManualController()
        {
            _servicio = ServicioAcreditacionManual.getInstancia();
        }

        // GET: AcreditacionManual
        public IActionResult Index()
        {
            return View();
        }

        // GET: AcreditacionManual/BuscarBuzon?query=...
        [HttpGet]
        public async Task<IActionResult> BuscarBuzon(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<BuzonBusquedaDto>());

            try
            {
                var buzones = await _servicio.BuscarBuzonesPorNN(query);
                return Json(buzones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: AcreditacionManual/ObtenerEmpresas?nc=...
        [HttpGet]
        public async Task<IActionResult> ObtenerEmpresas(string nc)
        {
            if (string.IsNullOrWhiteSpace(nc))
                return Json(new List<EmpresaDto>());

            try
            {
                var empresas = await _servicio.ObtenerEmpresasPorBuzon(nc);
                return Json(empresas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: AcreditacionManual/ObtenerDepositos?nc=...&empresa=...&desde=...&hasta=...&moneda=...
        [HttpGet]
        public async Task<IActionResult> ObtenerDepositos(
            string nc,
            string empresa,
            DateTime? desde,
            DateTime? hasta,
            string moneda = null)
        {
            if (string.IsNullOrWhiteSpace(nc) || string.IsNullOrWhiteSpace(empresa))
                return Json(new List<DepositoAcreditacionDto>());

            try
            {
                // Por defecto: últimos 7 días
                var fechaDesde = desde ?? DateTime.Today.AddDays(-7);
                var fechaHasta = hasta ?? DateTime.Today;

                var depositos = await _servicio.ObtenerDepositosUltimos7Dias(nc, empresa, fechaDesde, fechaHasta, moneda);
                
                // Mapear con estado de acreditación
                var depositosConEstado = await _servicio.MapearDepositosConEstadoAcreditado(depositos);

                return Json(depositosConEstado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: AcreditacionManual/Acreditar
        [HttpPost]
        public async Task<IActionResult> Acreditar([FromBody] AcreditarRequest request)
        {
            if (request == null || request.DepositosIds == null || !request.DepositosIds.Any())
            {
                return BadRequest(new { error = "No se seleccionaron depósitos para acreditar" });
            }

            try
            {
                // Obtener depósitos completos por IDs
                // Nota: En una implementación real, deberías obtener los depósitos desde la sesión o BD
                // Por ahora asumimos que el frontend envía toda la información necesaria
                var depositosSeleccionados = request.Depositos?.ToList() ?? new List<DepositoAcreditacionDto>();

                if (!depositosSeleccionados.Any())
                {
                    return BadRequest(new { error = "No se encontraron depósitos para acreditar" });
                }

                var resultados = await _servicio.AcreditarDepositos(
                    depositosSeleccionados,
                    request.UsuarioActual ?? "Sistema");

                return Json(new { exitoso = true, resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, exitoso = false });
            }
        }
    }

    public class AcreditarRequest
    {
        public List<int> DepositosIds { get; set; }
        public List<DepositoAcreditacionDto> Depositos { get; set; }
        public string UsuarioActual { get; set; }
    }
}
