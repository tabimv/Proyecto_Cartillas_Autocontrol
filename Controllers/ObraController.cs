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
    public class ObraController : Controller
    {
        private TestConexion db = new TestConexion();

        // GET: OBRA
        public async Task<ActionResult> Index()
        {
            var oBRA = db.OBRA.Include(o => o.COMUNA);
            return View(await oBRA.ToListAsync());
        }

        // GET: OBRA/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }
            return View(oBRA);
        }

        // GET: OBRA/Create
        public ActionResult Create()
        {
            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna");
            return View();
        }

        // POST: OBRA/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "obra_id,nombre_obra,direccion,COMUNA_comuna_id")] OBRA oBRA)
        {
            if (ModelState.IsValid)
            {
                db.OBRA.Add(oBRA);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            return View(oBRA);
        }

        // GET: OBRA/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }
            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            return View(oBRA);
        }

        // POST: OBRA/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "obra_id,nombre_obra,direccion,COMUNA_comuna_id")] OBRA oBRA)
        {
            if (ModelState.IsValid)
            {
                db.Entry(oBRA).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            return View(oBRA);
        }

        // GET: OBRA/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }
            return View(oBRA);
        }

        // POST: OBRA/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            OBRA oBRA = await db.OBRA.FindAsync(id);

            if (oBRA == null)
            {
                return HttpNotFound();
            }

            // Verificar si existen relaciones con claves foráneas
            if (db.OBRA.Any(t => t.obra_id == id))
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta obra debido a  que esta relacionada a otras Entidades.";
                return View("Delete", oBRA); // Mostrar vista de eliminación con el mensaje de error
            }

            db.OBRA.Remove(oBRA);
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
