using Proyecto_Cartilla_Autocontrol.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Data;
using System.Threading.Tasks;



namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class AdjuntarDocumentoController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();


        public async Task<ActionResult> GestionarDocumento(int id)
        {
            var cartillaSeleccionada = await db.CARTILLA.FindAsync(id);
            if (cartillaSeleccionada == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }
            ViewBag.CartillaSeleccionada = cartillaSeleccionada;

            var detalleCartilla = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA).Where(d => d.CARTILLA.cartilla_id == id).ToList();
            return View(detalleCartilla);
        }


        public async Task<ActionResult> GestionarDocumentoFiltrado(int id)
        {

            var cartillaSeleccionada = await db.CARTILLA.FindAsync(id);
            if (cartillaSeleccionada == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }
            ViewBag.CartillaSeleccionada = cartillaSeleccionada;

            var detalleCartilla = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA).Where(d => d.CARTILLA.cartilla_id == id).ToList();
            return View(detalleCartilla);
        }

        public async Task<ActionResult> ListaCartillasPorActividad()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var detalleCartillas = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA.ACTIVIDAD.OBRA.RESPONSABLE)
                    .Include(d => d.CARTILLA).Where(d => d.CARTILLA.ACTIVIDAD_actividad_id == d.CARTILLA.ACTIVIDAD.actividad_id).Where(d => d.CARTILLA.ACTIVIDAD.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));

                return View(await detalleCartillas.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        [HttpPost]
        public ActionResult AdjuntarDocumento(HttpPostedFileBase file, int cartillaId)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(file.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/documento"), fileName); // Ruta donde se guardarán los archivos

                    // Guardar el archivo en la carpeta de Documentos
                    file.SaveAs(path);

                    // Actualizar la ruta_pdf en la base de datos
                    var cartilla = db.CARTILLA.Find(cartillaId);
                    if (cartilla != null)
                    {
                        cartilla.ruta_pdf = "~/Content/documento/" + fileName; // Guardar la ruta relativa del archivo
                        db.SaveChanges();
                    }

                    // Redirigir a la vista GestionarDocumento con el ID de la cartilla
                    return RedirectToAction("GestionarDocumento", new { id = cartillaId });
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción que pueda ocurrir durante la subida del archivo
                    ViewBag.Error = "Hubo un error al subir el archivo: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Error = "Por favor, seleccione un archivo.";
            }

            // En caso de error, redirigir a la vista GestionarDocumento con el ID de la cartilla
            return RedirectToAction("GestionarDocumento", new { id = cartillaId });
        }

    }
}