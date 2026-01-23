using Microsoft.AspNetCore.Authorization;

namespace BibliotecaAPITest.Utilidades
{
    public class AllowAnonymousHandler : IAuthorizationHandler
    {
        // Permitir cualquier requerimento de authorización como exitoso
        public Task HandleAsync(AuthorizationHandlerContext context) {
            foreach (var requirement in context.PendingRequirements) {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
