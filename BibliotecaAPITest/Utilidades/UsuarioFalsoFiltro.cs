using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BibliotecaAPITest.Utilidades
{
    // Clase filtro para establecer en los claims un usuario de pruebas.
    public class UsuarioFalsoFiltro : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            // Antes de la acción
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> {
                new Claim("email", "ejemplo@hotmail.com")
            }, "prueba"));

            await next();

            // Despues de la acción
        }
    }
}
