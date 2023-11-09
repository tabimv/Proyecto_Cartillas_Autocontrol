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
    public class ActividadController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();

        // GET: Actividad
        public async Task<ActionResult> Index()
        {
            var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA);
            return View(await aCTIVIDAD.ToListAsync());
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
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
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

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", aCTIVIDAD.OBRA_obra_id);
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
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", aCTIVIDAD.OBRA_obra_id);
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
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", aCTIVIDAD.OBRA_obra_id);
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
