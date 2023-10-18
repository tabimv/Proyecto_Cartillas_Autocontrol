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
    public class ResponsableController : Controller
    {
        private ObraManzanoConexion db = new ObraManzanoConexion();

        // GET: Responsable
        public async Task<ActionResult> Index()
        {
            var rESPONSABLE = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.PERSONA);
            return View(await rESPONSABLE.ToListAsync());
        }

        // GET: Responsable/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            if (rESPONSABLE == null)
            {
                return HttpNotFound();
            }
            return View(rESPONSABLE);
        }

        // GET: Responsable/Create
        public ActionResult Create()
        {
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre");
            return View();
        }

        // POST: Responsable/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "responsable_id,cargo,OBRA_obra_id,PERSONA_rut")] RESPONSABLE rESPONSABLE)
        {
            if (ModelState.IsValid)
            {
                db.RESPONSABLE.Add(rESPONSABLE);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", rESPONSABLE.PERSONA_rut);
            return View(rESPONSABLE);
        }

        // GET: Responsable/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            if (rESPONSABLE == null)
            {
                return HttpNotFound();
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", rESPONSABLE.PERSONA_rut);
            return View(rESPONSABLE);
        }

        // POST: Responsable/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "responsable_id,cargo,OBRA_obra_id,PERSONA_rut")] RESPONSABLE rESPONSABLE)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rESPONSABLE).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", rESPONSABLE.PERSONA_rut);
            return View(rESPONSABLE);
        }

        // GET: Responsable/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            if (rESPONSABLE == null)
            {
                return HttpNotFound();
            }
            return View(rESPONSABLE);
        }

        // POST: Responsable/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            db.RESPONSABLE.Remove(rESPONSABLE);
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
