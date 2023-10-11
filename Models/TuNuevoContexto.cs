using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models
{

    public class TuNuevoContexto : DbContext
    {
        public DbSet<PERFIL> Perfiles { get; set; }
        // Agrega otros DbSet para las demás tablas de tu base de datos
    }


}