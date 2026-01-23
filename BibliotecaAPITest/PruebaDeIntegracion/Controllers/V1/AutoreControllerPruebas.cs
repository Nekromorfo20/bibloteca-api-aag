using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPITest.Utilidades;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace BibliotecaAPITest.PruebaDeIntegracion.Controllers.V1
{
    [TestClass]
    public class AutoreControllerPruebas : BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();

        // GET - /api/v1/autores | Devuelve 404 si autor no existe
        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste() {
            
            // Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificación
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: respuesta.StatusCode);
        }

        // GET - /api/v1/autores | Revisa si un autor existe con su Id
        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste(){

            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres = "Felipe", Apellidos = "Gavilan" });
            context.Autores.Add(new Autor() { Nombres = "Claudia", Apellidos = "Rodriguez" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificación
            respuesta.EnsureSuccessStatusCode();

            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, autor.Id);
        }

        // POST - /api/v1/autores | Revisa si el usuario que crea el autor no está autenticado, se devuelve 401
        [TestMethod]
        public async Task Post_Devuelve401_CuandoAutorNoEstaVerificado() {
            // Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Pruebas
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificación
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        // POST - /api/v1/autores | Revisa si el usuario que crea el autor no está autorizado con "esadmin", se devuelve 403
        [TestMethod]
        public async Task Post_Devuelve403_CuandoAutorNoEsAdmin() {
            // Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var token = await CrearUsuario(nombreBD, factory);
            
            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Pruebas
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificación
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

        // POST - /api/v1/autores | Revisa si el usuario autenticado y admin crea autor, se devuelve 201
        [TestMethod]
        public async Task Post_Devuelve201_CuandoAutorEsAdmin()
        {
            // Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var claims = new List<Claim> { adminClaim };
            var token = await CrearUsuario(nombreBD, factory, claims);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Pruebas
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            respuesta.EnsureSuccessStatusCode();

            // Verificación
            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }
    }
}
