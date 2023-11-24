using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using Proyecto_Cartilla_Autocontrol.Models;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaPerfilOTECController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();
        // GET: VistaPerfilOTEC
        public async Task<ActionResult> Index()
        {
            var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA);
            return View(await cARTILLA.ToListAsync());
        }

        public ActionResult EditarCartilla(int id)
        {
            // Obtener la Cartilla que se quiere editar por su ID
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);

            if (cartilla != null)
            {
                CartillasViewModel viewModel = new CartillasViewModel();
                viewModel.Cartilla = cartilla;

                // Obtener los detalles de la Cartilla que se quiere editar
                viewModel.DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList();

                // Obtener otras listas necesarias para el formulario (actividades, elementos de verificación, inmuebles, etc.)
                viewModel.ActividadesList = db.ACTIVIDAD.ToList();
                viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
                viewModel.InmuebleList = db.INMUEBLE.ToList();
                viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();

                return View(viewModel);
            }

            // Manejar si no se encuentra la Cartilla con el ID proporcionado
            return RedirectToAction("Index", "Cartilla"); // O alguna otra acción adecuada
        }

        [HttpPost]
        public ActionResult EditarCartilla(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoNoviembre())
                    {
                        // Actualizar la información de la Cartilla en la base de datos
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;

                        // Actualizar o agregar los detalles de la Cartilla en la base de datos
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            if (detalleCartilla.detalle_cartilla_id == 0)
                            {
                                // Nuevo detalle de Cartilla, agregarlo
                                detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                                dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            }
                            else
                            {
                                // Detalle de Cartilla existente, marcar como modificado
                                dbContext.Entry(detalleCartilla).State = EntityState.Modified;
                            }
                        }

                        // Eliminar detalles de la Cartilla que se hayan quitado en la edición
                        foreach (var detalle in dbContext.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id))
                        {
                            if (!viewModel.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                            {
                                dbContext.DETALLE_CARTILLA.Remove(detalle);
                            }
                        }

                        // Guardar los cambios en la base de datos
                        dbContext.SaveChanges();

                        return RedirectToAction("Index", "Cartilla");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            return View(viewModel);
        }
    }
}