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
    public class ItemVerifController : Controller
    {
        private ObraManzanoConexion db = new ObraManzanoConexion();

        // GET: ItemVerif
        public async Task<ActionResult> Index()
        {
            var iTEM_VERIF = db.ITEM_VERIF.Include(i => i.ACTIVIDAD);
            return View(await iTEM_VERIF.ToListAsync());
        }

        // GET: ItemVerif/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            if (iTEM_VERIF == null)
            {
                return HttpNotFound();
            }
            return View(iTEM_VERIF);
        }

        // GET: ItemVerif/Create
        public ActionResult Create()
        {
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad");
            ViewBag.ACTIVIDAD_nombre_actividad = new SelectList(db.ACTIVIDAD, "actividad_id", "nombre_actividad");
            return View();
        }

        // POST: ItemVerif/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "item_verif_id,elemento_verificacion,label,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
        {
            if (ModelState.IsValid)
            {
                db.ITEM_VERIF.Add(iTEM_VERIF);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", iTEM_VERIF.ACTIVIDAD_actividad_id);
            return View(iTEM_VERIF);
        }

        // GET: ItemVerif/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            if (iTEM_VERIF == null)
            {
                return HttpNotFound();
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", iTEM_VERIF.ACTIVIDAD_actividad_id);
            return View(iTEM_VERIF);
        }

        // POST: ItemVerif/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "item_verif_id,elemento_verificacion,label,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iTEM_VERIF).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", iTEM_VERIF.ACTIVIDAD_actividad_id);
            return View(iTEM_VERIF);
        }

        // GET: ItemVerif/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            if (iTEM_VERIF == null)
            {
                return HttpNotFound();
            }
            return View(iTEM_VERIF);
        }

        // POST: ItemVerif/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            db.ITEM_VERIF.Remove(iTEM_VERIF);
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
