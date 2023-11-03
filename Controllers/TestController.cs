using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks; // Agrega este using para admitir operaciones asincrónicas
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class TestController : Controller
    {
        private ObraManzanoConexion db = new ObraManzanoConexion();

        public ActionResult Index()
        {
            return View();
        }

        // GET: Test
        public async Task<ActionResult> CrearEditarCartilla(int actividadId)
        {
            // Consulta la base de datos para obtener cartillas y detalles de cartilla para la actividad especificada.
            var cartillas = await db.CARTILLA.Where(c => c.ACTIVIDAD_actividad_id == actividadId).ToListAsync();
            var detallesCartilla = await db.DETALLE_CARTILLA.Where(d => d.ACTIVIDAD_actividad_id == actividadId).ToListAsync();

            // Crea una instancia del modelo de vista y asigna los datos.
            var viewModel = new CartillaDetalleViewModel
            {
                ActividadId = actividadId,
                Cartillas = cartillas,
                DetallesCartilla = detallesCartilla
            };

            return View(viewModel);
        }



        [HttpPost]
        public async Task<ActionResult> GuardarCartilla(CartillaDetalleViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var dbContext = new ObraManzanoConexion())
                {
                    // Obtén la actividad a la que pertenecen las cartillas y detalles
                    var actividad = await dbContext.ACTIVIDAD.FindAsync(viewModel.ActividadId);

                    if (actividad != null)
                    {
                        // Actualiza las cartillas
                        foreach (var cartilla in viewModel.Cartillas)
                        {
                            var cartillaExistente = await dbContext.CARTILLA.FindAsync(cartilla.cartilla_id);
                            if (cartillaExistente != null)
                            {
                                // Aplica las actualizaciones a la cartilla
                                cartillaExistente.observaciones = cartilla.observaciones;
                                // Puedes seguir actualizando otros campos según sea necesario
                            }
                        }

                        // Actualiza los detalles de cartilla
                        foreach (var detalleCartilla in viewModel.DetallesCartilla)
                        {
                            var detalleCartillaExistente = await dbContext.DETALLE_CARTILLA.FindAsync(detalleCartilla.detalle_cartilla_id);
                            if (detalleCartillaExistente != null)
                            {
                                // Aplica las actualizaciones a los detalles de cartilla
                                detalleCartillaExistente.estado_otec = detalleCartilla.estado_otec;
                                detalleCartillaExistente.estado_ito = detalleCartilla.estado_ito;
                                // Puedes seguir actualizando otros campos según sea necesario
                            }
                        }

                        // Guarda los cambios en la base de datos de forma asincrónica
                        await dbContext.SaveChangesAsync();
                    }
                }

                // Redirige a la página de inicio o a donde sea necesario después de guardar los datos.
                return RedirectToAction("Index");
            }

            // Si hay errores en el modelo, vuelve a mostrar la vista para corregirlos.
            return View("CrearEditarCartilla", viewModel);
        }
    }
}
