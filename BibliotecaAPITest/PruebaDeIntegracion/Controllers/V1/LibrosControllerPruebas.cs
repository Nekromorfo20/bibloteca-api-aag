using BibliotecaAPI.DTOs;
using BibliotecaAPITest.Utilidades;
using System.Net;

namespace BibliotecaAPITest.PruebaDeIntegracion.Controllers.V1
{
    [TestClass]
    public class LibrosControllerPruebas : BasePruebas {
        private readonly string url = "/api/v1/libros";
        private string nombreBD = Guid.NewGuid().ToString();

        // POST - /api/v1/libros | Crea un libros usando un DTO incompleto, retorna 400
        [TestMethod]
        public async Task Post_Devuleve400_CuandoAutoresIdsEsVacio() {
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();
            var libroCreacionDTO = new LibroCreacionDTO { Titulo = "titulo" };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url ,libroCreacionDTO);

            // Verificación
            Assert.AreEqual(expected: HttpStatusCode.BadRequest, actual: respuesta.StatusCode);
        }
    }
}
