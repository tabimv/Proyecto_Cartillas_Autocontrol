using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaConsultaController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        // GET: VistsConsulta
        public async Task<ActionResult> Obra()
        {
            // Comprueba si el usuario está autenticado
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Busca las obras directamente y realiza la condición deseada
                var obras = await db.OBRA
                    .Include(o => o.USUARIO)
                    .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .Include(o => o.COMUNA)
                    .ToListAsync();

                return View(obras);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> Responsable()
        {
            // Comprueba si el usuario está autenticado
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var rESPONSABLE = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.OBRA.USUARIO).Where(r => r.OBRA.USUARIO.Any(u => u.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));
                return View(await rESPONSABLE.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }

        }

        public async Task<ActionResult> InmuebleLista()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var inmuebleGroupedByObra = await db.INMUEBLE
                .Include(e => e.OBRA.USUARIO)
                .Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                .OrderBy(e => e.inmueble_id)
                .GroupBy(e => e.OBRA_obra_id)
                .Select(g => g.FirstOrDefault())
                .ToListAsync();


                return View(inmuebleGroupedByObra);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> InmuebleDetails(int obraId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obraSeleccionado = await db.OBRA.FindAsync(obraId);
                if (obraSeleccionado == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.INMUEBLE
                    .Include(e => e.OBRA.USUARIO)
                    .Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .Where(a => a.OBRA_obra_id == obraId)
                    .OrderBy(a => a.inmueble_id)
                    .ToListAsync();


                ViewBag.ObraSeleccionado = obraSeleccionado;

                return View(items);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> Actividad()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;


                var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA).Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));
                return View(await aCTIVIDAD.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> ItemVerificacion()
        {
            var iTEM_VERIF = db.ITEM_VERIF.Include(i => i.ACTIVIDAD).OrderBy(i => i.label).ThenBy(i => i.ACTIVIDAD_actividad_id);
            return View(await iTEM_VERIF.ToListAsync());
        }


        public async Task<ActionResult> ItemLista()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var itemsGroupedByActivity = await db.ITEM_VERIF
                .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                  .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                .OrderBy(e => e.ACTIVIDAD_actividad_id)
                .GroupBy(e => e.ACTIVIDAD_actividad_id)
                .Select(g => g.FirstOrDefault())
                .ToListAsync();


                return View(itemsGroupedByActivity);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> ItemDetails(int actividadId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var actividadSeleccionado = await db.ACTIVIDAD.FindAsync(actividadId);
                if (actividadSeleccionado == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.ITEM_VERIF
                    .Where(a => a.ACTIVIDAD_actividad_id == actividadId)
                    .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                    .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .OrderBy(a => a.label)
                    .ToListAsync();


                ViewBag.ActividadSeleccionado = actividadSeleccionado;

                return View(items);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

    }
}