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
using Rotativa;
using Rotativa.Options;
using System.Net.Mail;
using System.IO;
using System.Runtime.Remoting.Contexts;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillasAutocontrolController : Controller
    {

        private ObraManzanoDicEntities db = new ObraManzanoDicEntities();

        public async Task<ActionResult> ListaCartillasPorActividad()
        {
            var detalleCartillas = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA).Where(d => d.CARTILLA.ACTIVIDAD_actividad_id == d.CARTILLA.ACTIVIDAD.actividad_id);
            return View(await detalleCartillas.ToListAsync());
        }
        public async Task<ActionResult> VerCartilla(int id)
        {
            // 1. Obtener la actividad específica a partir del parámetro id
            var actividad = await db.ACTIVIDAD.SingleOrDefaultAsync(a => a.actividad_id == id);

            if (actividad == null)
            {
                return HttpNotFound(); // Otra acción adecuada en caso de que la actividad no se encuentre.
            }

            // 2. Obtener todos los elementos de verificación relacionados con esa actividad
            var elementosVerificacion = await db.DETALLE_CARTILLA
                .Include(dc => dc.ITEM_VERIF)
                .Include(dc => dc.CARTILLA.ACTIVIDAD)
                .Where(dc => dc.CARTILLA.ACTIVIDAD_actividad_id == actividad.actividad_id)
                .ToListAsync(); // Utiliza ToListAsync() para cargar los datos de la base de datos de forma asincrónica.

            var ReponsablesObra = await db.RESPONSABLE.Include(r => r.PERSONA).ToListAsync();
            ViewBag.Responsables = ReponsablesObra;

            // 3. Pasar estos datos a la vista
            ViewBag.Actividad = actividad; // Esto es opcional, pero te permite acceder a los datos de la actividad en la vista.
            return View(elementosVerificacion);
        }

        public async Task<ActionResult> GeneratePDF(int id)
        {
            // 1. Obtener la actividad específica a partir del parámetro id
            var actividad = await db.ACTIVIDAD.SingleOrDefaultAsync(a => a.actividad_id == id);

            if (actividad == null)
            {
                return HttpNotFound(); // Otra acción adecuada en caso de que la actividad no se encuentre.
            }

            // 2. Obtener todos los elementos de verificación relacionados con esa actividad
            var elementosVerificacion = await db.DETALLE_CARTILLA
                .Include(dc => dc.ITEM_VERIF)
                .Where(dc => dc.CARTILLA.ACTIVIDAD_actividad_id == actividad.actividad_id)
                .ToListAsync(); // Utiliza ToListAsync() para cargar los datos de la base de datos de forma asincrónica.

            var ReponsablesObra = await db.RESPONSABLE.Include(r => r.PERSONA).ToListAsync();
            ViewBag.Responsables = ReponsablesObra;

            // 3. Pasar estos datos a la vista
            ViewBag.Actividad = actividad; // Esto es opcional, pero te permite acceder a los datos de la actividad en la vista.


            var pdf = new Rotativa.ViewAsPdf("GeneratePDF", elementosVerificacion)
            {
                FileName = "CartillaDeControl.pdf", // Nombre del archivo PDF resultante
                PageSize = Rotativa.Options.Size.B4,
                PageOrientation = Rotativa.Options.Orientation.Landscape,

            };

            return pdf;
        }



        // GET: DetalleCartilla/Edit/5
        public async Task<ActionResult> Edit(int? id, string entityType)
        {
            if (id == null || string.IsNullOrEmpty(entityType))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (entityType.Equals("CARTILLA", StringComparison.OrdinalIgnoreCase))
            {
                CARTILLA cartilla = await db.CARTILLA.FindAsync(id);

                if (cartilla == null)
                {
                    return HttpNotFound();
                }

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", cartilla.ACTIVIDAD_actividad_id);
                ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado", cartilla.ESTADO_FINAL_estado_final_id);
                ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", cartilla.OBRA_obra_id);

                return View("Edit", cartilla);
            }
            else if (entityType.Equals("DETALLE_CARTILLA", StringComparison.OrdinalIgnoreCase))
            {
                DETALLE_CARTILLA detalleCartilla = await db.DETALLE_CARTILLA.FindAsync(id);

                if (detalleCartilla == null)
                {
                    return HttpNotFound();
                }

                // Agrupa los detalles de la cartilla por ACTIVIDAD_actividad_id
                var detallesAgrupados = db.DETALLE_CARTILLA
                    .Include(d => d.ITEM_VERIF)
                    .Where(dc => dc.CARTILLA.ACTIVIDAD_actividad_id == detalleCartilla.CARTILLA.ACTIVIDAD.actividad_id)
                    .ToList();

                ViewBag.DetallesAgrupados = detallesAgrupados;

                ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble", detalleCartilla.INMUEBLE_inmueble_id);
                ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion", detalleCartilla.ITEM_VERIF_item_verif_id);
                return View("Edit", detalleCartilla);
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        // POST: DetalleCartilla/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "detalle_cartilla_id,estado_otec,estado_ito,ITEM_VERIF_item_verif_id,ACTIVIDAD_actividad_id,CARTILLA_cartilla_id,INMUEBLE_inmueble_id, CARTILLA")] DETALLE_CARTILLA detalleCartilla)
        {
            if (ModelState.IsValid)
            {
                db.Entry(detalleCartilla).State = EntityState.Modified;
                db.Entry(detalleCartilla.CARTILLA).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.INMUEBLE_inmueble_id = new SelectList(db.INMUEBLE, "inmueble_id", "tipo_inmueble", detalleCartilla.INMUEBLE_inmueble_id);
            ViewBag.ITEM_VERIF_item_verif_id = new SelectList(db.ITEM_VERIF, "item_verif_id", "elemento_verificacion", detalleCartilla.ITEM_VERIF_item_verif_id);
            ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", detalleCartilla.CARTILLA.ACTIVIDAD_actividad_id);
            ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "estado", detalleCartilla.CARTILLA.ESTADO_FINAL_estado_final_id);
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", detalleCartilla.CARTILLA.OBRA_obra_id);

            return View("Edit", detalleCartilla);
        }

    }
}



