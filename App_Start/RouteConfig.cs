using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Proyecto_Cartilla_Autocontrol
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );

            routes.MapRoute(
            name: "DescargarPDF",
            url: "CartillasAutocontrol/DescargarPDF/{id}",
            defaults: new { controller = "CartillasAutocontrol", action = "DescargarPDF" }
           );

            routes.MapRoute(
             name: "GenerarPDF",
             url: "VisualizarCartilla/GenerarPDF/{id}",
             defaults: new { controller = "VisualizarCartilla", action = "GenerarPDF", id = UrlParameter.Optional }
         );


        }

    }
}
