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
    public class ActividadController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Actividad
        public async Task<ActionResult> Index()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = await db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToListAsync();

                var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA)
                      .Where(a => obrasAcceso.Contains(a.OBRA_obra_id));
                return View(await aCTIVIDAD.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public ActionResult ExportToExcel()
        {
            var actividades = db.ACTIVIDAD.Include(r => r.OBRA).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Actividades");
                worksheet.Cell(1, 1).Value = "Obra Asociada";
                worksheet.Cell(1, 2).Value = "Codigo Actividad";
                worksheet.Cell(1, 3).Value = "Nombre Actividad";
                worksheet.Cell(1, 4).Value = "Estado (Activo/Bloqueado)";
                worksheet.Cell(1, 5).Value = "Tipo de Actividad (Proyecto/Inmueble)";


                int row = 2;
                foreach (var actividad in actividades)
                {
                    worksheet.Cell(row, 1).Value = actividad.OBRA.nombre_obra;
                    worksheet.Cell(row, 2).Value = actividad.codigo_actividad;
                    worksheet.Cell(row, 3).Value = actividad.nombre_actividad;
                    worksheet.Cell(row, 4).Value = actividad.estado;
                    worksheet.Cell(row, 5).Value = actividad.tipo_actividad;


                    row++;
                }

                // Ajustar el ancho de las columnas según el contenido
                worksheet.Columns().AdjustToContents();

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);

                var fileName = "Actividades.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        // GET: Actividad/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);
            if (aCTIVIDAD == null)
            {
                return HttpNotFound();
            }
            return View(aCTIVIDAD);

        }

        // GET: Actividad/Create
        public ActionResult Create()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;


                var obrasUsuarioIds = db.ACCESO_OBRAS
                               .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                               .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                               .Distinct()
                               .ToList();

                // Obtén la lista completa de obras desde la base de datos
                var todasLasObras = db.OBRA.ToList();

                // Filtra las obras que no tienen el nombre "Oficina Central"
                var obrasFiltradas = todasLasObras.Where(a => obrasUsuarioIds.Contains(a.obra_id)).ToList();

                // Asigna la lista filtrada a ViewBag.Obras
                ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");

                return View();
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        // POST: Actividad/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado,tipo_actividad,notas,OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasUsuarioIds = db.ACCESO_OBRAS
                               .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                               .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                               .Distinct()
                               .ToList();

                // Transformar los campos para que la primera letra sea mayúscula
                aCTIVIDAD.nombre_actividad = CapitalizeFirstLetter(aCTIVIDAD.nombre_actividad);
                if (ModelState.IsValid)
                {
                    db.ACTIVIDAD.Add(aCTIVIDAD);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                // Obtén la lista completa de obras desde la base de datos
                var todasLasObras = db.OBRA.ToList();



                // Filtra las obras que no tienen el nombre "Oficina Central"
                var obrasFiltradas = todasLasObras.Where(a => obrasUsuarioIds.Contains(a.obra_id)).ToList();

                // Asigna la lista filtrada a ViewBag.Obras
                ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Palabras que deben permanecer en minúscula
            var lowercaseExceptions = new HashSet<string> { "a", "de", "en", "y", "o", "con", "por", "para", "al", "del", "un", "una", "los", "las" };

            input = input.Trim().ToLower();
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                // Capitalizar la primera palabra o palabras que no están en la lista de excepciones
                if (i == 0 || !lowercaseExceptions.Contains(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        // GET: Actividad/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasUsuarioIds = db.ACCESO_OBRAS
                               .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                               .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                               .Distinct()
                               .ToList();

                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);
                if (aCTIVIDAD == null)
                {
                    return HttpNotFound();
                }

                var estadoFinalCartilla = await db.CARTILLA
                              .Where(c => c.ACTIVIDAD_actividad_id == aCTIVIDAD.actividad_id)
                              .Select(c => c.ESTADO_FINAL_estado_final_id)
                              .FirstOrDefaultAsync();

                bool actividadRelacionada = await db.CARTILLA.AnyAsync(c => c.ACTIVIDAD_actividad_id == aCTIVIDAD.actividad_id);


                var obras = db.OBRA.Where(a => obrasUsuarioIds.Contains(a.obra_id)).ToList();
                // Filtra las obras que no tienen el nombre "Oficina Central"
           

                var selectedObraId = aCTIVIDAD.OBRA_obra_id; // Obtener el ID de la obra asociada a la actividad
                ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", selectedObraId);


                // Dentro de tu método Edit GET
                ViewBag.CartillaRelacionada = actividadRelacionada ? "True" : "False";
                ViewBag.EstadoFinalCartilla = estadoFinalCartilla == 1 ? "Aprobada" : (estadoFinalCartilla == 2 ? "En proceso" : "Rechazada");

                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        // POST: Actividad/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado, tipo_actividad, notas, OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasUsuarioIds = db.ACCESO_OBRAS
                               .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                               .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                               .Distinct()
                               .ToList();

                if (ModelState.IsValid)
                {
                    aCTIVIDAD.nombre_actividad = CapitalizeFirstLetter(aCTIVIDAD.nombre_actividad);
                    // Verificar si la actividad está relacionada a una cartilla
                    bool actividadRelacionada = await db.CARTILLA.AnyAsync(c => c.ACTIVIDAD_actividad_id == aCTIVIDAD.actividad_id);

                    // Si la actividad está relacionada a una cartilla
                    if (actividadRelacionada)
                    {
                        var estadoFinalCartilla = await db.CARTILLA
                            .Where(c => c.ACTIVIDAD_actividad_id == aCTIVIDAD.actividad_id)
                            .Select(c => c.ESTADO_FINAL_estado_final_id)
                            .FirstOrDefaultAsync();

                        // Si el estado final de la cartilla es "Aprobada" (VB), establecer el estado de la actividad como "B" (Bloqueado)
                        if (estadoFinalCartilla == 1) // VB - Aprobada
                        {

                            aCTIVIDAD.estado = "B";
                        }
                        // Si el estado final de la cartilla es "En proceso" (EP) o "Rechazada" (R), establecer el estado de la actividad como "A" (Activo)
                        else if (estadoFinalCartilla == 2 || estadoFinalCartilla == 3) // R - Rechazada, EP - En proceso
                        {

                            // Mantén el estado de la actividad como "Activo"
                            aCTIVIDAD.estado = "A";
                        }
                        else
                        {

                            var obras = db.OBRA.Where(a => obrasUsuarioIds.Contains(a.obra_id)).ToList();
                            var selectedObraId = aCTIVIDAD.OBRA_obra_id; // Obtener el ID de la obra asociada a la actividad
                            ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", selectedObraId);


                            return View(aCTIVIDAD);
                        }
                    }

                    // Actualizar el estado de la actividad en la base de datos
                    db.Entry(aCTIVIDAD).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }


                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public JsonResult CheckProjectBlock(int obraId)
        {
            bool hasProjectBlock = db.LOTE_INMUEBLE.Any(l => l.OBRA_obra_id == obraId && l.tipo_bloque == "Proyecto");
            return Json(new { hasProjectBlock }, JsonRequestBehavior.AllowGet);
        }


        // GET: Actividad/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);
            if (aCTIVIDAD == null)
            {
                return HttpNotFound();
            }
            return View(aCTIVIDAD);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);

            if (aCTIVIDAD == null)
            {
                return HttpNotFound();
            }

            // Eliminar ítems de verificación asociados a la actividad
            var itemsToDelete = db.ITEM_VERIF.Where(iv => iv.ACTIVIDAD_actividad_id == id);
            db.ITEM_VERIF.RemoveRange(itemsToDelete);

            bool tieneActividadesRelacionadas = db.CARTILLA.Any(a => a.ACTIVIDAD_actividad_id == id);

            if (tieneActividadesRelacionadas)
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Actividad porque está relacionada a una Cartilla de Autocontrol.";
                return View("Delete", aCTIVIDAD); // Mostrar vista de eliminación con el mensaje de error
            }
            else
            {
                db.ACTIVIDAD.Remove(aCTIVIDAD);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
        }






        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
