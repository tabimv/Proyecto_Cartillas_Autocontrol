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
            var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA);
            return View(await aCTIVIDAD.ToListAsync());
        }

        public ActionResult ExportToExcel()
        {
            var actividades = db.ACTIVIDAD.Include(r => r.OBRA).ToList();

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
            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View();
        }

        // POST: Actividad/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado,OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (ModelState.IsValid)
            {
                db.ACTIVIDAD.Add(aCTIVIDAD);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View(aCTIVIDAD);
        }

        // GET: Actividad/Edit/5
        public async Task<ActionResult> Edit(int? id)
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
            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View(aCTIVIDAD);
        }

        // POST: Actividad/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "actividad_id,codigo_actividad,nombre_actividad,estado,OBRA_obra_id")] ACTIVIDAD aCTIVIDAD)
        {
            if (ModelState.IsValid)
            {
                db.Entry(aCTIVIDAD).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View(aCTIVIDAD);
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

            // Verificar si existen otras relaciones, excluyendo la relación con la obra
            bool tieneOtrasRelaciones = db.ACTIVIDAD.Any(o => o.OBRA_obra_id != aCTIVIDAD.OBRA_obra_id);
            bool tieneActividadesRelacionadas = db.CARTILLA.Any(a => a.ACTIVIDAD_actividad_id == id);

            if (tieneOtrasRelaciones && !tieneActividadesRelacionadas)
            {
                // Si la obra tiene relación con Obra, se permite la eliminación
                db.ACTIVIDAD.Remove(aCTIVIDAD);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Actividad porque está relacionada con otras entidades.";
                return View("Delete", aCTIVIDAD); // Mostrar vista de eliminación con el mensaje de error
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
