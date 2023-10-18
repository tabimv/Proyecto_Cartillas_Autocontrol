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
    public class CartillaController : Controller
    {
        private ObraManzanoConexion db = new ObraManzanoConexion();

        // GET: Cartilla
        public async Task<ActionResult> Index()
        {
            var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA);
            return View(await cARTILLA.ToListAsync());
        }

        // GET: Cartilla/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CARTILLA cARTILLA = await db.CARTILLA.FindAsync(id);
            if (cARTILLA == null)
            {
                return HttpNotFound();
            }
            return View(cARTILLA);
        }

        // GET: Cartilla/Create
        public ActionResult Create()
        {
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad");
            ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado");
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
            return View();
        }

        // POST: Cartilla/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "cartilla_id,fecha,ruta_documento_pdf,observaciones,OBRA_obra_id,ACTIVIDAD_actividad_id,ESTADO_FINAL_estado_final_id")] CARTILLA cARTILLA)
        {
            if (ModelState.IsValid)
            {
                db.CARTILLA.Add(cARTILLA);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", cARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado", cARTILLA.ESTADO_FINAL_estado_final_id);
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", cARTILLA.OBRA_obra_id);
            return View(cARTILLA);
        }

        // GET: Cartilla/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CARTILLA cARTILLA = await db.CARTILLA.FindAsync(id);
            if (cARTILLA == null)
            {
                return HttpNotFound();
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", cARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado", cARTILLA.ESTADO_FINAL_estado_final_id);
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", cARTILLA.OBRA_obra_id);
            return View(cARTILLA);
        }

        // POST: Cartilla/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "cartilla_id,fecha,ruta_documento_pdf,observaciones,OBRA_obra_id,ACTIVIDAD_actividad_id,ESTADO_FINAL_estado_final_id")] CARTILLA cARTILLA)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cARTILLA).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", cARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado", cARTILLA.ESTADO_FINAL_estado_final_id);
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", cARTILLA.OBRA_obra_id);
            return View(cARTILLA);
        }

        // GET: Cartilla/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CARTILLA cARTILLA = await db.CARTILLA.FindAsync(id);
            if (cARTILLA == null)
            {
                return HttpNotFound();
            }
            return View(cARTILLA);
        }

        // POST: Cartilla/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            CARTILLA cARTILLA = await db.CARTILLA.FindAsync(id);

            if (cARTILLA == null)
            {
                return HttpNotFound();
            }

            // Verificar si existen relaciones con claves foráneas
            if (db.CARTILLA.Any(t => t.cartilla_id == id))
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Cartilla debido a  que esta relacionado a otras Entidades.";
                return View("Delete", cARTILLA); // Mostrar vista de eliminación con el mensaje de error
            }

            db.CARTILLA.Remove(cARTILLA);
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
