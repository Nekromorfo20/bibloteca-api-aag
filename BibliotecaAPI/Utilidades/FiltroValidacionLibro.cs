using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter {
        private readonly ApplicationDbContext dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            // Antes de ejecutar la acción

            // Validar que se haya pasa de parámetro el DTO libroCreacionDTO
            if (!context.ActionArguments.TryGetValue("libroCreacionDTO", out var value) || value is not LibroCreacionDTO libroCreacionDTO) {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es válido");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            // Se evalua si se envio el arreglo de Ids
            if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), "No se puede crear un libro sin autores");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            // Se obtiene todos los ids de autores con los Ids enviados
            var autoresIdsExisten = await dbContext.Autores
                .Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            // Se compara los Ids encontrados en BD con los Ids enviados, si alguno no existe manda un error
            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count) {
                var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(",", autoresNoExisten);
                var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensajeDeError);
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            await next();
            // Despues de ejecutar la acción
        }
    }
}
