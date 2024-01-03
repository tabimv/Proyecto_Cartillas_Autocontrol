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
using ClosedXML.Excel;


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




        public ActionResult ExportToExcel()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obras = db.OBRA.Include(o => o.COMUNA)
                    .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Obras");

                    worksheet.Cell(1, 1).Value = "Nombre Obra";
                    worksheet.Cell(1, 2).Value = "Dirección";
                    worksheet.Cell(1, 3).Value = "Nombre Comuna";

                    int row = 2;
                    foreach (var obra in obras)
                    {
                        worksheet.Cell(row, 1).Value = obra.nombre_obra;
                        worksheet.Cell(row, 2).Value = obra.direccion;
                        worksheet.Cell(row, 3).Value = obra.COMUNA.nombre_comuna;
                        row++;
                    }

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Obras.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }

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

        public ActionResult ExportToExcelResponsable()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var responsables = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.PERSONA)
                    .Where(r => r.OBRA.USUARIO.Any(u => u.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Responsables");
                    worksheet.Cell(1, 1).Value = "Rut Responsable de Obra";
                    worksheet.Cell(1, 2).Value = "Nombre Responsable";
                    worksheet.Cell(1, 3).Value = "Cargo";
                    worksheet.Cell(1, 4).Value = "Obra Asociada";


                    int row = 2;
                    foreach (var responsable in responsables)
                    {
                        worksheet.Cell(row, 1).Value = responsable.PERSONA_rut;
                        worksheet.Cell(row, 2).Value = $"{responsable.PERSONA.nombre} {responsable.PERSONA.apeliido_paterno} {responsable.PERSONA.apellido_materno}";
                        worksheet.Cell(row, 3).Value = responsable.cargo;
                        worksheet.Cell(row, 4).Value = responsable.OBRA.nombre_obra;


                        row++;
                    }

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Responsables.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
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


        public ActionResult ExportToExcelInmueble()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var inmuebles = db.INMUEBLE.Include(o => o.OBRA)
                     .Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inmuebles");

                    worksheet.Cell(1, 1).Value = "Código Inmueble";
                    worksheet.Cell(1, 2).Value = "Tipo de Inmueble";
                    worksheet.Cell(1, 3).Value = "Obra Asociada";

                    int row = 2;
                    foreach (var inmueble in inmuebles)
                    {
                        worksheet.Cell(row, 1).Value = inmueble.codigo_inmueble;
                        worksheet.Cell(row, 2).Value = inmueble.tipo_inmueble;
                        worksheet.Cell(row, 3).Value = inmueble.OBRA.nombre_obra;
                        row++;
                    }

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Inmuebles.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
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