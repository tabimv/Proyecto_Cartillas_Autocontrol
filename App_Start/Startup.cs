using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Threading.Tasks;
using Microsoft.Owin.Cors;

[assembly: OwinStartup(typeof(Proyecto_Cartilla_Autocontrol.App_Start.Startup))]

namespace Proyecto_Cartilla_Autocontrol.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Habilitar CORS
            app.UseCors(CorsOptions.AllowAll);

            // Configuración de autenticación con cookies
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                ExpireTimeSpan = TimeSpan.FromHours(12),
                LoginPath = new PathString("/Account/Login")
            });
        }
    }
}
