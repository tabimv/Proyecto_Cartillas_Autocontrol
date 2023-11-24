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
    public class VistaConsultaController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();
        // GET: VistsConsulta
        public async Task<ActionResult> Obra()
        {
            var oBRA = db.OBRA.Include(o => o.COMUNA);
            return View(await oBRA.ToListAsync());
        }

        public async Task<ActionResult> Responsable()
        {
            var rESPONSABLE = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.PERSONA);
            return View(await rESPONSABLE.ToListAsync());
        }

        public async Task<ActionResult> Inmueble()
        {
            var iNMUEBLE = db.INMUEBLE.Include(i => i.OBRA);
            return View(await iNMUEBLE.ToListAsync());
        }

        public async Task<ActionResult> Actividad()
        {
            var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA);
            return View(await aCTIVIDAD.ToListAsync());
        }

        public async Task<ActionResult> ItemVerificacion()
        {
            var iTEM_VERIF = db.ITEM_VERIF.Include(i => i.ACTIVIDAD).OrderBy(i => i.label).ThenBy(i => i.ACTIVIDAD_actividad_id);
            return View(await iTEM_VERIF.ToListAsync());
        }
    }
}