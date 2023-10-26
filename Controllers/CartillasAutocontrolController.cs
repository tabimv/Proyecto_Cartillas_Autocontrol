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
using Rotativa;
using Rotativa.Options;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillasAutocontrolController : Controller
    {

        private ObraManzanoConexion db = new ObraManzanoConexion();

        public async Task<ActionResult> ListaCartillasPorActividad()
        {
            var detalleCartillas = db.DETALLE_CARTILLA.Include(d => d.ACTIVIDAD).Include(d => d.ITEM_VERIF).Where(d => d.ACTIVIDAD_actividad_id == d.ACTIVIDAD.actividad_id);
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
                .Where(dc => dc.ACTIVIDAD_actividad_id == actividad.actividad_id)
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
                .Where(dc => dc.ACTIVIDAD_actividad_id == actividad.actividad_id)
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





    }
}



