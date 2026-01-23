using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaAPI.Utilidades
{
    public class HATEOASFilterAttribute: ResultFilterAttribute
    {
        protected bool DebeIncluirHATEOAS(ResultExecutingContext context) {
            // Se valida que la petición HTTP retorne un objeto de respuesta y tenga status 200
            if (context.Result is not ObjectResult result || !EsRespuestaExitosa(result)) {
                return false;
            }

            // Se valida que en la cabecera exista el valor "IncluirHATEOAS"
            if (!context.HttpContext.Request.Headers.TryGetValue("IncluirHATEOAS", out var cabecera)) {
                return false;
            }

            return string.Equals(cabecera, "Y", StringComparison.OrdinalIgnoreCase);
        }

        private bool EsRespuestaExitosa(ObjectResult result) {
            // Se valida que la propiedad Value exista en el result
            if (result.Value is null) {
                return false;
            }

            // Se valida que el result tenga el código 200
            if (result.StatusCode.HasValue && !result.StatusCode.Value.ToString().StartsWith("2")) {
                return false;
            }

            return true;
        }
    }
}
