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
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System.Data.SqlClient;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ItemVerifController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();

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




        // GET: ItemVerif/Crear

        public ActionResult Crear()
        {
            ViewBag.Actividades = new SelectList(db.ACTIVIDAD.ToList(), "actividad_id", "nombre_actividad"); // Obtener actividades para el DropDownList
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRegistros(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                // Obtener el valor de la actividad seleccionada
                int actividadId = int.Parse(form["ActividadId"]);

                // Obtener los datos de los campos clonados para ITEM_VERIF y guardarlos
                for (int i = 0; i < form.Count / 3; i++) // Suponiendo que por cada registro hay 3 campos (ElementoVerificacion, Label y ActividadId)
                {
                    var nuevoItem = new ITEM_VERIF
                    {
                        elemento_verificacion = form[$"ItemsVerif[{i}].ElementoVerificacion"],
                        label = form[$"ItemsVerif[{i}].Label"],
                        ACTIVIDAD_actividad_id = actividadId
                    };

                    db.ITEM_VERIF.Add(nuevoItem);
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // En caso de errores, regresar a la vista
            ViewBag.Actividades = new SelectList(db.ACTIVIDAD.ToList(), "actividad_id", "nombre_actividad");
            return View("Crear");
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
