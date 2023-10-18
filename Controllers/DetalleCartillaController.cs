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
    public class DetalleCartillaController : Controller
    {
        private ObraManzanoConexion db = new ObraManzanoConexion();

        // GET: DetalleCartilla
        public async Task<ActionResult> Index()
        {
            var dETALLE_CARTILLA = db.DETALLE_CARTILLA.Include(d => d.ACTIVIDAD).Include(d => d.CARTILLA).Include(d => d.INMUEBLE).Include(d => d.ITEM_VERIF);
            return View(await dETALLE_CARTILLA.ToListAsync());
        }

        // GET: DetalleCartilla/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DETALLE_CARTILLA dETALLE_CARTILLA = await db.DETALLE_CARTILLA.FindAsync(id);
            if (dETALLE_CARTILLA == null)
            {
                return HttpNotFound();
            }
            return View(dETALLE_CARTILLA);
        }

        // GET: DetalleCartilla/Create
        public ActionResult Create()
        {
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad");
            ViewBag.CARTILLA_cartilla_id = new SelectList(db.CARTILLA, "cartilla_id", "ruta_documento_pdf");
            ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble");
            ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion");
            return View();
        }

        // POST: DetalleCartilla/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "detalle_cartilla_id,estado_otec,estado_ito,ITEM_VERIF_item_verif_id,ACTIVIDAD_actividad_id,CARTILLA_cartilla_id,INMUEBLE_inmueble_id")] DETALLE_CARTILLA dETALLE_CARTILLA)
        {
            if (ModelState.IsValid)
            {
                db.DETALLE_CARTILLA.Add(dETALLE_CARTILLA);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", dETALLE_CARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.CARTILLA_cartilla_id = new SelectList(db.CARTILLA, "cartilla_id", "ruta_documento_pdf", dETALLE_CARTILLA.CARTILLA_cartilla_id);
            ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble", dETALLE_CARTILLA.INMUEBLE_inmueble_id);
            ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion", dETALLE_CARTILLA.ITEM_VERIF_item_verif_id);
            return View(dETALLE_CARTILLA);
        }

        // GET: DetalleCartilla/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DETALLE_CARTILLA dETALLE_CARTILLA = await db.DETALLE_CARTILLA.FindAsync(id);
            if (dETALLE_CARTILLA == null)
            {
                return HttpNotFound();
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", dETALLE_CARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.CARTILLA_cartilla_id = new SelectList(db.CARTILLA, "cartilla_id", "ruta_documento_pdf", dETALLE_CARTILLA.CARTILLA_cartilla_id);
            ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble", dETALLE_CARTILLA.INMUEBLE_inmueble_id);
            ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion", dETALLE_CARTILLA.ITEM_VERIF_item_verif_id);
            return View(dETALLE_CARTILLA);
        }

        // POST: DetalleCartilla/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "detalle_cartilla_id,estado_otec,estado_ito,ITEM_VERIF_item_verif_id,ACTIVIDAD_actividad_id,CARTILLA_cartilla_id,INMUEBLE_inmueble_id")] DETALLE_CARTILLA dETALLE_CARTILLA)
        {
            if (ModelState.IsValid)
            {
                db.Entry(dETALLE_CARTILLA).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", dETALLE_CARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.CARTILLA_cartilla_id = new SelectList(db.CARTILLA, "cartilla_id", "ruta_documento_pdf", dETALLE_CARTILLA.CARTILLA_cartilla_id);
            ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble", dETALLE_CARTILLA.INMUEBLE_inmueble_id);
            ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion", dETALLE_CARTILLA.ITEM_VERIF_item_verif_id);
            return View(dETALLE_CARTILLA);
        }

        // GET: DetalleCartilla/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DETALLE_CARTILLA dETALLE_CARTILLA = await db.DETALLE_CARTILLA.FindAsync(id);
            if (dETALLE_CARTILLA == null)
            {
                return HttpNotFound();
            }
            return View(dETALLE_CARTILLA);
        }

        // POST: DetalleCartilla/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            DETALLE_CARTILLA dETALLE_CARTILLA = await db.DETALLE_CARTILLA.FindAsync(id);

            if (dETALLE_CARTILLA == null)
            {
                return HttpNotFound();
            }

            // Verificar si existen relaciones con claves foráneas
            if (db.DETALLE_CARTILLA.Any(t => t.detalle_cartilla_id == id))
            {
                ViewBag.ErrorMessage = "No se puede eliminar el Detalle Cartilla debido a  que esta relacionado a otras Entidades.";
                return View("Delete", dETALLE_CARTILLA); // Mostrar vista de eliminación con el mensaje de error
            }

            db.DETALLE_CARTILLA.Remove(dETALLE_CARTILLA);
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
