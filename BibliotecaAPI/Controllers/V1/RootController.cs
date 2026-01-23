using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class RootController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;

        public RootController(IAuthorizationService authorizationService) {
            this.authorizationService = authorizationService;
        }

        [HttpGet(Name = "ObtenerRootV1")]
        [AllowAnonymous]
        public async Task<IEnumerable<DatosHATEOASDTO>> Get() {
            var datosHATEOAS = new List<DatosHATEOASDTO>();

            var esAdmin = await authorizationService.AuthorizeAsync(User, "esadmin");
            
            // Acciones que cualquiera puede realizar
            datosHATEOAS.Add(new DatosHATEOASDTO(
                Enlace: Url.Link("ObtenerRootV1", new { })!,
                Descripción: "self",
                Metodo: "GET"
             ));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                Enlace: Url.Link("ObtenerAutoresV1", new { })!,
                Descripción: "autores-obtener",
                Metodo: "GET"
            ));
            
            datosHATEOAS.Add(new DatosHATEOASDTO(
                Enlace: Url.Link("RegistroUsuarioV1", new { })!,
                Descripción: "usuario-registrar",
                Metodo: "POST"
            ));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                Enlace: Url.Link("LoginUsuarioV1", new { })!,
                Descripción: "usuario-login",
                Metodo: "POST"
            ));

            // Acciones que pueden realizar autores autenticados
            if (User.Identity!.IsAuthenticated) {
                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("ActualizarUsuarioV1", new { })!,
                    Descripción: "usuario-actualizar",
                    Metodo: "PUT"
                ));

                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("RenovarTokenV1", new { })!,
                    Descripción: "token-renovar",
                    Metodo: "GET"
                ));
            }

            // Acciones que solo usuarios admin pueden realizar
            if (esAdmin.Succeeded) {
                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("CrearAutorV1", new { })!,
                    Descripción: "autor-crear",
                    Metodo: "POST"
                ));

                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("CrearAutoresV1", new { })!,
                    Descripción: "autores-crear",
                    Metodo: "POST"
                ));

                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("CrearLibroV1", new { })!,
                    Descripción: "libro-crear",
                    Metodo: "POST"
                ));

                datosHATEOAS.Add(new DatosHATEOASDTO(
                    Enlace: Url.Link("ObtenerUsuariosV1", new { })!,
                    Descripción: "usuarios-obtener",
                    Metodo: "GET"
                ));
            }

            return datosHATEOAS;
        }

    }
}
