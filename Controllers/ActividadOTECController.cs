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
using ClosedXML.Excel;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ActividadOTECController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        public async Task<ActionResult> Index()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA)
                               .Include(e => e.OBRA.USUARIO)
                    .Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));
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
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var actividades = db.ACTIVIDAD.Include(r => r.OBRA)
                    .Where(o => o.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Responsables");
                    worksheet.Cell(1, 1).Value = "Código Actividad";
                    worksheet.Cell(1, 2).Value = "Nombre Actividad";
                    worksheet.Cell(1, 3).Value = "Estado (Activo/Bloqueado)";
                    worksheet.Cell(1, 4).Value = "Obra Asociada";


                    int row = 2;
                    foreach (var actividad in actividades)
                    {
                        worksheet.Cell(row, 1).Value = actividad.codigo_actividad;
                        worksheet.Cell(row, 2).Value = actividad.nombre_actividad;
                        worksheet.Cell(row, 3).Value = actividad.estado;
                        worksheet.Cell(row, 4).Value = actividad.OBRA.nombre_obra;


                        row++;
                    }

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Actividades.xlsx";
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


        public ActionResult Create()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                var obrasAsociadas = db.OBRA
                    .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                    .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "obra_id", "nombre_obra");

                return View();
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: Actividad/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado,OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                if (ModelState.IsValid)
                {
                    db.ACTIVIDAD.Add(aCTIVIDAD);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                var obrasAsociadas = db.OBRA
                   .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                   .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "obra_id", "nombre_obra");
                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }


        public async Task<ActionResult> Edit(int? id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);
                if (aCTIVIDAD == null)
                {
                    return HttpNotFound();
                }
                var obrasAsociadas = db.OBRA
                  .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                  .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "obra_id", "nombre_obra");
                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: Actividad/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado,OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                if (ModelState.IsValid)
                {
                    db.Entry(aCTIVIDAD).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                var obrasAsociadas = db.OBRA
                     .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                     .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "obra_id", "nombre_obra");
                return View(aCTIVIDAD);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

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

        // POST: Actividad/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            ACTIVIDAD aCTIVIDAD = await db.ACTIVIDAD.FindAsync(id);

            if (aCTIVIDAD == null)
            {
                return HttpNotFound();
            }

            // Verificar si existen relaciones con claves foráneas
            if (db.ACTIVIDAD.Any(t => t.actividad_id == id))
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Actividad debido a  que esta relacionado a otras Entidades.";
                return View("Delete", aCTIVIDAD); // Mostrar vista de eliminación con el mensaje de error
            }

            db.ACTIVIDAD.Remove(aCTIVIDAD);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");


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