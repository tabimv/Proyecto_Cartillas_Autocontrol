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
    public class InmuebleController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();

        // GET: Inmueble
        public async Task<ActionResult> Index()
        {
            var iNMUEBLE = db.INMUEBLE.Include(i => i.OBRA);
            return View(await iNMUEBLE.ToListAsync());
        }

        // GET: Inmueble/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(iNMUEBLE);
        }

        // GET: Inmueble/Create
        public ActionResult Create()
        {
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
            return View();
        }

        // POST: Inmueble/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "inmueble_id,tipo_inmueble,OBRA_obra_id")] INMUEBLE iNMUEBLE)
        {
            if (ModelState.IsValid)
            {
                db.INMUEBLE.Add(iNMUEBLE);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }

        // GET: Inmueble/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }

        // POST: Inmueble/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "inmueble_id,tipo_inmueble,OBRA_obra_id")] INMUEBLE iNMUEBLE)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iNMUEBLE).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }
 
        // GET: Inmueble/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(iNMUEBLE);
        }

        // POST: Inmueble/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            db.INMUEBLE.Remove(iNMUEBLE);
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
