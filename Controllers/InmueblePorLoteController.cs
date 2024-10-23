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
using System.Text.RegularExpressions;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class InmueblePorLoteController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: InmueblePorLote
        public async Task<ActionResult> Index()
        {
            var iNMUEBLE = db.INMUEBLE.Include(i => i.LOTE_INMUEBLE);
            return View(await iNMUEBLE.ToListAsync());
        }

        // GET: InmueblePorLote/Details/5
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

        // GET: InmueblePorLote/Create

        public ActionResult Crear()
        {
            try
            {
                // Obtener la lista de lotes de inmuebles desde la base de datos
                var todosLosLotes = db.LOTE_INMUEBLE.ToList();

                // Crear una lista de SelectListItems para el DropDownList
                var listaLotes = todosLosLotes.Select(l => new SelectListItem
                {
                    Value = l.lote_id.ToString(),
                    Text = $"{l.abreviatura} - {l.OBRA.nombre_obra}"
                });

                ViewBag.Lotes = new SelectList(listaLotes, "Value", "Text");

                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error: " + ex.Message);
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(int loteId)
        {
            try
            {
                var lote = db.LOTE_INMUEBLE.Find(loteId);
                string tipoInmueble = lote.tipo_bloque;
                string abreviatura = lote.abreviatura;
                int cantidadInmuebles = (int)(lote.cantidad_inmuebles != null ? lote.cantidad_inmuebles : 0.0m);

                for (int i = 1; i <= cantidadInmuebles; i++)
                {
                    var nuevoInmueble = new INMUEBLE
                    {
                        tipo_inmueble = tipoInmueble == "torre" ? "Departamento" : "Vivienda",
                        codigo_inmueble = abreviatura.StartsWith("M") ? $"{abreviatura}-VIV{i + 100}" : $"{abreviatura}-{i + 100}",
                        LOTE_INMUEBLE_lote_id = loteId
                    };

                    db.INMUEBLE.Add(nuevoInmueble);
                }

                db.SaveChanges();
                return RedirectToAction("InmuebleLista", "Inmueble");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los registros: " + ex.Message);
                // Redirige a la vista con los lotes nuevamente para que el usuario pueda seleccionar otro
                var todosLosLotes = db.LOTE_INMUEBLE.ToList();
                var listaLotes = todosLosLotes.Select(l => new SelectListItem
                {
                    Value = l.lote_id.ToString(),
                    Text = $"{l.abreviatura} - {l.OBRA.nombre_obra}"
                });
                ViewBag.Lotes = new SelectList(listaLotes, "Value", "Text");
                return View();
            }
        }




        public async Task<ActionResult> ConfirmarEliminarInmuebles(int loteID)
        {
            var lote = await db.LOTE_INMUEBLE.FindAsync(loteID); // Obtén la actividad por su ID
            if (lote == null)
            {
                return HttpNotFound(); // Manejar el caso donde la actividad no existe
            }

            return View(lote);
        }

        [HttpPost]
        public async Task<ActionResult> EliminarInmueblesPorLote(int loteID)
        {
            // Encuentra y elimina todos los registros de ITEM_VERIF asociados a la actividadId
            var inmueblesAEliminar = await db.INMUEBLE
                .Where(e => e.LOTE_INMUEBLE_lote_id == loteID)
                .ToListAsync();

            if (inmueblesAEliminar == null || !inmueblesAEliminar.Any())
            {
                return HttpNotFound(); // O el código de error que desees si no se encuentran elementos para eliminar
            }

            // Elimina los registros asociados a la actividadId
            db.INMUEBLE.RemoveRange(inmueblesAEliminar);
            await db.SaveChangesAsync();

            return RedirectToAction("InmuebleLista", "Inmueble"); // Redirige a la vista de la lista de ítems
        }


        // GET: Inmueble/EditByLoteId/5
        public ActionResult EditByLoteId(int? loteId)
        {
            if (loteId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var inmuebles = db.INMUEBLE.Where(i => i.LOTE_INMUEBLE_lote_id == loteId).ToList();
            if (inmuebles.Count == 0)
            {
                return HttpNotFound();
            }

            return View(inmuebles);
        }

        // POST: Inmueble/UpdateByLoteId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateByLoteId(List<INMUEBLE> inmuebles)
        {
            if (ModelState.IsValid)
            {
                foreach (var inmueble in inmuebles)
                {
                    db.Entry(inmueble).State = EntityState.Modified;
                }
                db.SaveChanges();
                return RedirectToAction("InmuebleLista", "Inmueble");
            }

            return View(inmuebles);
        }



        // GET: InmueblePorLote/Edit/5
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
            ViewBag.LOTE_INMUEBLE_lote_id = new SelectList(db.LOTE_INMUEBLE, "lote_id", "abreviatura", iNMUEBLE.LOTE_INMUEBLE_lote_id);
            return View(iNMUEBLE);
        }

        // POST: InmueblePorLote/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "inmueble_id,tipo_inmueble,codigo_inmueble,LOTE_INMUEBLE_lote_id")] INMUEBLE iNMUEBLE)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iNMUEBLE).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("InmuebleLista", "Inmueble");
            }
            ViewBag.LOTE_INMUEBLE_lote_id = new SelectList(db.LOTE_INMUEBLE, "lote_id", "abreviatura", iNMUEBLE.LOTE_INMUEBLE_lote_id);
            return View(iNMUEBLE);
        }

        // GET: InmueblePorLote/Delete/5
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

        // POST: InmueblePorLote/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            INMUEBLE inmueble = await db.INMUEBLE.FindAsync(id);
            if (inmueble == null)
            {
                return HttpNotFound();
            }

            // Verificar si el inmueble está referenciado en DETALLE_CARTILLA
            bool isReferenced = db.DETALLE_CARTILLA.Any(dc => dc.INMUEBLE_inmueble_id == id);
            if (isReferenced)
            {
                ViewBag.ErrorMessage = "No se puede eliminar este Inmueble porque está relacionada a una Cartilla de Autocontrol.";
                return View(inmueble);
            }
            // Obtener el ID del lote asociado al inmueble
            int loteId = inmueble.LOTE_INMUEBLE.lote_id;

            // Eliminar el inmueble
            db.INMUEBLE.Remove(inmueble);
            await db.SaveChangesAsync();

            // Actualizar la cantidad de inmuebles en el lote asociado
            var lote = await db.LOTE_INMUEBLE.FindAsync(loteId);
            if (lote != null)
            {
                // Obtener el número actual de inmuebles en el lote
                // Obtener el número actual de inmuebles en el lote
                decimal cantidadInmueblesDecimal = lote.cantidad_inmuebles;

                // Convertir el valor decimal a entero
                int cantidadInmuebles = (int)cantidadInmueblesDecimal;


                // Restar 1 al número de inmuebles
                cantidadInmuebles--;

                // Asignar el nuevo valor al número de inmuebles en el lote
                lote.cantidad_inmuebles = cantidadInmuebles;

                // Guardar los cambios en la base de datos
                await db.SaveChangesAsync();
            }
            else
            {
                // Manejar el caso en el que no se encuentra el lote asociado
                return HttpNotFound("El lote asociado al inmueble no fue encontrado.");
            }

            // Redirigir a la acción InmuebleLista del controlador Inmueble
            return RedirectToAction("InmuebleLista", "Inmueble");
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
