using System;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;

namespace Proyecto_Cartilla_Autocontrol
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static Timer _timer;
        private static readonly int IntervalInMinutes = 1; // Intervalo de tiempo en minutos para ejecutar la tarea

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Iniciar el temporizador para tareas periódicas si es necesario
            _timer = new Timer(ExecuteScheduledTasks, null, TimeSpan.Zero, TimeSpan.FromMinutes(IntervalInMinutes));
        }

        private void ExecuteScheduledTasks(object state)
        {
            // Lógica de tareas periódicas
        }

        protected void Session_Start(Object sender, EventArgs e)
        {
            Session.Timeout = 720; // Configurar un tiempo de espera de sesión de 12 horas (720 minutos)
        }

        protected void Application_End()
        {
            // Detener el temporizador cuando la aplicación finalice
            _timer?.Dispose();
        }
    }
}
