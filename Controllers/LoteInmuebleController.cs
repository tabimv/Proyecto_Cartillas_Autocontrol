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
using System.Data.Entity.Validation;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using System.IO;
using DocumentFormat.OpenXml.EMMA;
using System.Data.SqlClient;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class LoteInmuebleController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: LoteInmueble
        public async Task<ActionResult> Index()
        {
            var lOTE_INMUEBLE = db.LOTE_INMUEBLE.Include(l => l.OBRA);
            return View(await lOTE_INMUEBLE.ToListAsync());
        }

        public async Task<ActionResult> LoteLista()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtra las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var obrasAcceso = db.ACCESO_OBRAS
                                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                    .Select(a => a.obra_id)
                                    .ToList();

                var inmuebleGroupedByObra = await db.LOTE_INMUEBLE
                     .Include(e => e.OBRA)
                     .Where(e => obrasAcceso.Contains(e.OBRA_obra_id)) // Filtra solo las obras a las que el usuario tiene acceso
                     .OrderBy(e => e.lote_id)
                     .GroupBy(e => e.OBRA_obra_id)
                     .Select(g => g.FirstOrDefault())
                     .ToListAsync();

                return View(inmuebleGroupedByObra);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        // GET: EliminarLotesInmueblesPorObra
        public async Task<ActionResult> EliminarLotesInmueblesPorObra(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var obra = await db.OBRA.FindAsync(id);

            if (obra == null)
            {
                return HttpNotFound();
            }

            var lotesInmuebles = await db.LOTE_INMUEBLE
                .Include(l => l.OBRA)
                .Where(l => l.OBRA_obra_id == id)
                .ToListAsync();

            ViewBag.Obra = obra;
            return View(lotesInmuebles);
        }

        // POST: EliminarLotesInmueblesPorObra
        [HttpPost, ActionName("EliminarLotesInmueblesPorObra")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarLotesInmueblesPorObraConfirmed(int id)
        {
            var lotesInmuebles = await db.LOTE_INMUEBLE
                .Include(l => l.INMUEBLE)
                .Where(l => l.OBRA_obra_id == id)
                .ToListAsync();

            if (lotesInmuebles.Any())
            {
                foreach (var lote in lotesInmuebles)
                {
                    db.INMUEBLE.RemoveRange(lote.INMUEBLE);
                    db.LOTE_INMUEBLE.Remove(lote);
                }
                await db.SaveChangesAsync();
            }

            return RedirectToAction("LoteLista");
        }

        public async Task<ActionResult> LoteDetails(int ObraId)
        {
            var obraSeleccionado = await db.OBRA.FindAsync(ObraId);
            if (obraSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.LOTE_INMUEBLE
                .Where(a => a.OBRA_obra_id == ObraId)
                .ToListAsync();

            // Crear diccionarios para almacenar el estado de cada lote
            var loteDetalleCartillaStatus = new Dictionary<int, bool>();
            var loteInmueblesEnCartillasStatus = new Dictionary<int, bool>();

            foreach (var item in items)
            {
                // Verificar si el lote tiene detalles asociados
                bool tieneDetalleCartilla = LoteTieneDetalleCartilla(item.lote_id);
                // Verificar si todos los inmuebles del lote están en las cartillas de autocontrol
                bool todosInmueblesEnCartillas = TodosInmueblesEnCartillas(item.lote_id, ObraId);

                // Almacenar los resultados en los diccionarios respectivos
                loteDetalleCartillaStatus[item.lote_id] = tieneDetalleCartilla;
                loteInmueblesEnCartillasStatus[item.lote_id] = todosInmueblesEnCartillas;
            }

            // Pasar la información a la vista
            ViewBag.ObraSeleccionado = obraSeleccionado;
            ViewBag.LoteDetalleCartillaStatus = loteDetalleCartillaStatus ?? new Dictionary<int, bool>(); // Inicializar como un diccionario vacío si es null
            ViewBag.LoteInmueblesEnCartillasStatus = loteInmueblesEnCartillasStatus ?? new Dictionary<int, bool>(); // Inicializar como un diccionario vacío si es null

            return View(items);
        }

        private bool LoteTieneDetalleCartilla(int loteId)
        {
            return db.DETALLE_CARTILLA
                .Any(d => db.INMUEBLE
                    .Any(i => i.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE_inmueble_id == i.inmueble_id));
        }

        private bool TodosInmueblesEnCartillas(int loteId, int obraId)
        {
            // Obtener todos los IDs de inmuebles del lote
            var inmuebleIds = db.INMUEBLE
                .Where(i => i.LOTE_INMUEBLE_lote_id == loteId)
                .Select(i => i.inmueble_id)
                .ToList();

            // Obtener todos los IDs de inmuebles que están en las cartillas de autocontrol de la obra
            var cartillaInmuebleIds = db.DETALLE_CARTILLA
                .Where(d => db.CARTILLA.Any(c => c.OBRA_obra_id == obraId && c.cartilla_id == d.CARTILLA_cartilla_id))
                .Select(d => d.INMUEBLE_inmueble_id)
                .Distinct()
                .ToList();

            // Verificar si todos los inmuebles del lote están en las cartillas de autocontrol
            return !inmuebleIds.Except(cartillaInmuebleIds).Any();
        }


        [HttpPost]
        public ActionResult EjecutarProcedimiento(int loteId, int obraId)
        {
            // Llamar al procedimiento almacenado
            db.Database.ExecuteSqlCommand(
                "EXEC sp_AdicionarInmueblesFaltantes @LOTE_INMUEBLE_lote_id, @OBRA_obra_id",
                new SqlParameter("@LOTE_INMUEBLE_lote_id", loteId),
                new SqlParameter("@OBRA_obra_id", obraId)
            );

            // Redirigir a la vista actual después de la ejecución
            return Json(new { success = true });
        }


       


        // Acción para descargar el archivo Excel
        public async Task<ActionResult> DescargarExcel(int obraId)
        {
            var items = await db.LOTE_INMUEBLE
                .Where(a => a.OBRA_obra_id == obraId)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Lotes de Inmueble");

                // Agregar encabezados
                worksheet.Cell(1, 1).Value = "Obra Asociada";
                worksheet.Cell(1, 2).Value = "Tipo de Bloque";
                worksheet.Cell(1, 3).Value = "Abreviatura";
                worksheet.Cell(1, 4).Value = "Rango Inicial";
                worksheet.Cell(1, 5).Value = "Rango Final";
                worksheet.Cell(1, 6).Value = "Cantidad de Pisos";
                worksheet.Cell(1, 7).Value = "Cantidad de Inmuebles";

                // Agregar datos
                var row = 2;
                foreach (var item in items)
                {
                    worksheet.Cell(row, 1).Value = item.OBRA.nombre_obra;
                    worksheet.Cell(row, 2).Value = item.tipo_bloque;
                    worksheet.Cell(row, 3).Value = item.abreviatura;
                    worksheet.Cell(row, 4).Value = item.rango_inicial;
                    worksheet.Cell(row, 5).Value = item.rango_final;
                    worksheet.Cell(row, 6).Value = item.cantidad_pisos;
                    worksheet.Cell(row, 7).Value = item.cantidad_inmuebles;
                    row++;
                }

                // Ajustar el ancho de las columnas según el contenido
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LotesInmueble.xlsx");
                }
            }
        }




        // GET: LoteInmueble/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LOTE_INMUEBLE lOTE_INMUEBLE = await db.LOTE_INMUEBLE.FindAsync(id);
            if (lOTE_INMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(lOTE_INMUEBLE);
        }

        public string ObtenerNombreObraPorId(int obraId)
        {
            // Aquí deberías tener lógica para buscar el nombre de la obra en tu base de datos
            // o cualquier otra fuente de datos que estés utilizando en tu aplicación
            // Por ejemplo, si estás utilizando Entity Framework, podría ser algo como:

            var obra = db.OBRA.FirstOrDefault(o => o.obra_id == obraId);

            if (obra != null)
            {
                return obra.nombre_obra;
            }
            else
            {
                return "Nombre de obra no encontrado";
            }
        }



        public ActionResult CreateByObra(int obraId)
        {
            var obras = db.OBRA.Where(o => o.nombre_obra != "Oficina Central").ToList();
            ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra");
            ViewBag.CantidadInmuebles = 0;
            ViewBag.ObraId = obraId;
            var nombreObra = ObtenerNombreObraPorId(obraId);
            ViewBag.NombreObra = nombreObra;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateByObra(int obraId, [Bind(Include = "lote_id,tipo_bloque,numero_letra_bloque_1,abreviatura,cantidad_pisos,cantidad_inmuebles,OBRA_obra_id")] LOTE_INMUEBLE lOTE_INMUEBLE)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    lOTE_INMUEBLE.OBRA_obra_id = obraId;

                    // Verificar si el tipo de bloque es "Manzana"
                    if (lOTE_INMUEBLE.tipo_bloque == "Manzana")
                    {
                        lOTE_INMUEBLE.cantidad_pisos = 0; // Si es Manzana, no se considera la cantidad de pisos
                    }

                    // Crear inmuebles según el tipo de bloque y la cantidad especificada
                    if ((lOTE_INMUEBLE.tipo_bloque == "Manzana" && lOTE_INMUEBLE.cantidad_inmuebles > 0) ||
                        (lOTE_INMUEBLE.tipo_bloque == "Torre" && lOTE_INMUEBLE.cantidad_pisos > 0))
                    {
                        // Agregar el lote a la base de datos
                        db.LOTE_INMUEBLE.Add(lOTE_INMUEBLE);
                        await db.SaveChangesAsync();

                        // Obtener el ID del lote recién creado
                        var loteId = lOTE_INMUEBLE.lote_id;
                        var tipoInmueble = lOTE_INMUEBLE.tipo_bloque == "Manzana" ? "Vivienda" : "Departamento";

                        // Crear inmuebles según la cantidad especificada
                        if (lOTE_INMUEBLE.tipo_bloque == "Manzana")
                        {
                            for (int i = 1; i <= lOTE_INMUEBLE.cantidad_inmuebles; i++)
                            {
                                var codigoInmueble = $"{lOTE_INMUEBLE.abreviatura}-VIV{i}";
                                var inmueble = new INMUEBLE
                                {
                                    tipo_inmueble = tipoInmueble,
                                    codigo_inmueble = codigoInmueble,
                                    LOTE_INMUEBLE_lote_id = loteId
                                };

                                db.INMUEBLE.Add(inmueble);
                            }
                        }
                        else if (lOTE_INMUEBLE.tipo_bloque == "Torre")
                        {
                            var obra = db.OBRA.Find(lOTE_INMUEBLE.OBRA_obra_id);
                            var totalDeptos = obra.total_deptos;

                            for (int piso = 1; piso <= lOTE_INMUEBLE.cantidad_pisos; piso++)
                            {
                                for (int depto = 1; depto <= totalDeptos; depto++)
                                {
                                    var codigoInmueble = $"{lOTE_INMUEBLE.abreviatura}-{(piso - 1) * totalDeptos + depto}";
                                    var inmueble = new INMUEBLE
                                    {
                                        tipo_inmueble = tipoInmueble,
                                        codigo_inmueble = codigoInmueble,
                                        LOTE_INMUEBLE_lote_id = loteId
                                    };

                                    db.INMUEBLE.Add(inmueble);
                                }
                            }
                        }

                        // Guardar los cambios en la base de datos
                        await db.SaveChangesAsync();
                    }

                    // Redirigir al usuario a la lista de lotes
                    return RedirectToAction("LoteLista");
                }
            }
            catch (DbEntityValidationException ex)
            {
                // Manejar errores de validación de la entidad...
            }

            // Si llega a este punto, significa que ha habido un error o la validación ha fallado
            // Por lo tanto, volvemos a cargar la vista con los datos del lOTE_INMUEBLE y las obras disponibles
            var obras = db.OBRA.Where(o => o.nombre_obra != "Oficina Central").ToList();
            ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", lOTE_INMUEBLE.OBRA_obra_id);
            return View(lOTE_INMUEBLE);
        }

        public ActionResult Create()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso =  db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                var obrasSinCartilla = db.OBRA
                   .Where(a => obrasAcceso.Contains(a.obra_id) && !db.CARTILLA.Any(c => c.OBRA_obra_id == a.obra_id))
                   .ToList();


                ViewBag.OBRA_obra_id = new SelectList(obrasSinCartilla, "obra_id", "nombre_obra");


                ViewBag.CantidadInmuebles = 0;

                return View();
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "lote_id,tipo_bloque,numero_letra_bloque_1,abreviatura,cantidad_pisos,cantidad_inmuebles,rango_inicial,rango_final,OBRA_obra_id")] LOTE_INMUEBLE lOTE_INMUEBLE)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasAcceso = db.ACCESO_OBRAS
                           .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                           .Select(a => a.obra_id)
                           .ToList();

                try
                {
                    if (ModelState.IsValid)
                    {
                        bool abreviaturaExists = db.LOTE_INMUEBLE.Any(l => l.OBRA_obra_id == lOTE_INMUEBLE.OBRA_obra_id && l.abreviatura == lOTE_INMUEBLE.abreviatura);
                        if (abreviaturaExists)
                        {
                            ModelState.AddModelError("abreviatura", "Ya existe esta abreviatura relacionada a otro lote inmueble.\nRecarga la página y vuelve a intentar con otro N° o Letra de bloque");
                            var obritas = db.OBRA.Where(a => obrasAcceso.Contains(a.obra_id)).ToList();
                            ViewBag.OBRA_obra_id = new SelectList(obritas, "obra_id", "nombre_obra", lOTE_INMUEBLE.OBRA_obra_id);
                            return View(lOTE_INMUEBLE);
                        }

                        // Agregar el lote a la base de datos
                        db.LOTE_INMUEBLE.Add(lOTE_INMUEBLE);
                        await db.SaveChangesAsync();

                        // Obtener el ID del lote recién creado
                        var loteId = lOTE_INMUEBLE.lote_id;


                        if (lOTE_INMUEBLE.tipo_bloque == "Manzana")
                        {
                            // Verificar que los rangos no sean nulos
                            if (lOTE_INMUEBLE.rango_inicial.HasValue && lOTE_INMUEBLE.rango_final.HasValue)
                            {
                                int rangoInicial = (int)lOTE_INMUEBLE.rango_inicial.Value;
                                int rangoFinal = (int)lOTE_INMUEBLE.rango_final.Value;

                                // Variable para contar la cantidad de inmuebles creados
                                int cantidadInmuebles = 0;

                                // Crear inmuebles para Manzana basados en el rango inicial y final
                                for (int i = rangoInicial; i <= rangoFinal; i++)
                                {
                                    var codigoInmueble = $"{lOTE_INMUEBLE.abreviatura}-VIV{i}";
                                    var inmueble = new INMUEBLE
                                    {
                                        tipo_inmueble = "Vivienda",
                                        codigo_inmueble = codigoInmueble,
                                        LOTE_INMUEBLE_lote_id = loteId
                                    };

                                    db.INMUEBLE.Add(inmueble);

                                    // Incrementar el contador de inmuebles
                                    cantidadInmuebles++;
                                }

                                // Actualizar la cantidad de inmuebles en el lote
                                lOTE_INMUEBLE.cantidad_inmuebles = cantidadInmuebles;
                            }
                        }
                        else if (lOTE_INMUEBLE.tipo_bloque == "Torre")
                        {
                            // Crear inmuebles para Torre
                            for (int piso = 1; piso <= lOTE_INMUEBLE.cantidad_pisos; piso++)
                            {
                                for (int i = 1; i <= lOTE_INMUEBLE.cantidad_inmuebles; i++)
                                {
                                    var codigoInmueble = $"{lOTE_INMUEBLE.abreviatura}-{piso * 100 + i}";
                                    var inmueble = new INMUEBLE
                                    {
                                        tipo_inmueble = "Departamento",
                                        codigo_inmueble = codigoInmueble,
                                        LOTE_INMUEBLE_lote_id = loteId
                                    };

                                    db.INMUEBLE.Add(inmueble);
                                }
                            }
                        }
                        // Verificar si el tipo_bloque es igual a "PROYECTO"
                        else if (lOTE_INMUEBLE.tipo_bloque == "Proyecto")
                        {

                            // Crear un solo inmueble para Proyecto
                            var codigoInmueble = "PRO-001";
                            var inmueble = new INMUEBLE
                            {
                                tipo_inmueble = "Proyecto",
                                codigo_inmueble = codigoInmueble,
                                LOTE_INMUEBLE_lote_id = loteId // Relacionar con el LOTE_INMUEBLE creado
                                                               // Otros campos necesarios
                            };
                            db.INMUEBLE.Add(inmueble);
                            db.SaveChanges();

                            return RedirectToAction("LoteLista");
                        }
                        // Guardar los cambios en la base de datos
                        await db.SaveChangesAsync();

                        // Redirigir al usuario a la lista de lotes
                        return RedirectToAction("LoteLista");
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    // Manejar errores de validación de la entidad...
                }
                // Si llega a este punto, significa que ha habido un error o la validación ha fallado
                // Por lo tanto, volvemos a cargar la vista con los datos del lOTE_INMUEBLE y las obras disponibles
                var obrasSinCartilla = db.OBRA
                    .Where(a => obrasAcceso.Contains(a.obra_id) && !db.CARTILLA.Any(c => c.OBRA_obra_id == a.obra_id))
                    .ToList();

                ViewBag.OBRA_obra_id = new SelectList(obrasSinCartilla, "obra_id", "nombre_obra");
                return View(lOTE_INMUEBLE);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }






        public string ObtenerTipoProyectoPorObraId(int obraId)
        {
            var tipoProyecto = "";
            var obra = db.OBRA.FirstOrDefault(o => o.obra_id == obraId);
            if (obra != null)
            {
                tipoProyecto = obra.tipo_proyecto;
            }

            return tipoProyecto;
        }

        [HttpGet]
        public ActionResult ObtenerTipoProyecto(int obraId)
        {
            var tipoProyecto = ObtenerTipoProyectoPorObraId(obraId);
            return Json(new { tipoProyecto }, JsonRequestBehavior.AllowGet);
        }

        public int ObtenerTotalViviendas(int obraId)
        {
            var obra = db.OBRA.FirstOrDefault(o => o.obra_id == obraId);
            var totalViv = obra != null ? (int)(obra.total_viv ?? 0) : 0;

            return totalViv;
        }

        [HttpGet]
        public ActionResult ObtenerCantidadInmuebles(int obraId)
        {
            try
            {
                var obra = db.OBRA.Find(obraId);
                if (obra != null)
                {
                    // Obtener la cantidad de inmuebles (total_viv) de la obra
                    var cantidadInmuebles = obra.total_viv ?? 0;
                    return Json(new { cantidadInmuebles }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    // Si no se encuentra la obra, retornar error
                    return Json(new { error = "No se encontró la obra especificada" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción y retornar error
                return Json(new { error = "Error al obtener la cantidad de inmuebles de la obra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetLotesInmueblesByObra(int obraId)
        {
            try
            {
                using (var dbContext = new ObraManzanoFinal())
                {
                    // Filtrar los lotes de inmuebles asociados a la obra seleccionada
                    var lotesInmuebles = dbContext.LOTE_INMUEBLE
                        .Where(lote => lote.OBRA_obra_id == obraId)
                        .Select(lote => new
                        {
                            lote_id = lote.lote_id,
                            abreviatura = lote.abreviatura
                        })
                        .ToList();

                    return Json(lotesInmuebles, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener los lotes de inmuebles asociados a la obra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // GET: LoteInmueble/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LOTE_INMUEBLE lOTE_INMUEBLE = await db.LOTE_INMUEBLE.FindAsync(id);
            if (lOTE_INMUEBLE == null)
            {
                return HttpNotFound();
            }

            var obras = db.OBRA.Where(o => o.nombre_obra != "Oficina Central").ToList();
            ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", lOTE_INMUEBLE.OBRA_obra_id);
            return View(lOTE_INMUEBLE);
        }

        // POST: LoteInmueble/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: LoteInmueble/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "lote_id,tipo_bloque,numero_letra_bloque_1,abreviatura,cantidad_pisos,cantidad_inmuebles,OBRA_obra_id")] LOTE_INMUEBLE lOTE_INMUEBLE)
        {
            if (ModelState.IsValid)
            {
                // Verificar si ya existe un registro con la misma combinación de numero_letra_bloque_1 y abreviatura en la misma obra
                var existeLoteInmueble = db.LOTE_INMUEBLE.Any(l =>
                    l.numero_letra_bloque_1 == lOTE_INMUEBLE.numero_letra_bloque_1 &&
                    l.abreviatura == lOTE_INMUEBLE.abreviatura &&
                    l.OBRA_obra_id == lOTE_INMUEBLE.OBRA_obra_id &&
                    l.lote_id != lOTE_INMUEBLE.lote_id);

                if (existeLoteInmueble)
                {
                    ModelState.AddModelError("", "Ya existe un Lote con la misma N° o Letra de bloque y Abreviatura en la misma Obra.");
                    var obrass = db.OBRA.Where(o => o.nombre_obra != "Oficina Central").ToList();
                    ViewBag.OBRA_obra_id = new SelectList(obrass, "obra_id", "nombre_obra", lOTE_INMUEBLE.OBRA_obra_id);
                    return View(lOTE_INMUEBLE);
                }

                db.Entry(lOTE_INMUEBLE).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Actualizar códigos de inmueble
                var inmuebles = db.INMUEBLE.Where(i => i.LOTE_INMUEBLE_lote_id == lOTE_INMUEBLE.lote_id).ToList();
                foreach (var inmueble in inmuebles)
                {
                    var parts = inmueble.codigo_inmueble.Split('-');
                    if (parts.Length > 1)
                    {
                        inmueble.codigo_inmueble = lOTE_INMUEBLE.abreviatura + "-" + parts[1];
                    }
                }
                await db.SaveChangesAsync();

                return RedirectToAction("LoteDetails", new { ObraId = lOTE_INMUEBLE.OBRA_obra_id });
            }
            var obras = db.OBRA.Where(o => o.nombre_obra != "Oficina Central").ToList();
            ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", lOTE_INMUEBLE.OBRA_obra_id);

            return View(lOTE_INMUEBLE);
        }







        // GET: LoteInmueble/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LOTE_INMUEBLE lOTE_INMUEBLE = await db.LOTE_INMUEBLE.FindAsync(id);
            if (lOTE_INMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(lOTE_INMUEBLE);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            // Obtener el lote de inmueble a eliminar
            LOTE_INMUEBLE loteInmueble = await db.LOTE_INMUEBLE.FindAsync(id);
            if (loteInmueble == null)
            {
                return HttpNotFound();
            }
            // Verificar si hay inmuebles asociados a este lote que están referenciados en DETALLE_CARTILLA
            var inmueblesReferenciados = db.DETALLE_CARTILLA.Any(dc => dc.INMUEBLE.LOTE_INMUEBLE_lote_id == id);
            if (inmueblesReferenciados)
            {
                ViewBag.ErrorMessage = "No se puede eliminar este Lote de Inmueble porque está relacionada a una Cartilla de Autocontrol.";
                return View(loteInmueble);
            }
          
            // Eliminar los registros de INMUEBLE asociados a este lote
            var inmuebles = await db.INMUEBLE.Where(i => i.LOTE_INMUEBLE_lote_id == id).ToListAsync();
            db.INMUEBLE.RemoveRange(inmuebles);

            // Eliminar el lote de inmueble
            db.LOTE_INMUEBLE.Remove(loteInmueble);

            await db.SaveChangesAsync();

            // Redirigir a la vista LoteLista
            return RedirectToAction("LoteLista");
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
