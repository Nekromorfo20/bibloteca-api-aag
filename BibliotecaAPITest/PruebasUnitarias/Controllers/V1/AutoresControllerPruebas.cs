using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPITest.Utilidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using BibliotecaAPI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BibliotecaAPITest.Utilidades.Dobles;
using BibliotecaAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace BibliotecaAPITest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore outputCacheStore = null!;
        IServicioAutores serviciosAutores = null!;
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void SetUp()
        {
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutomapper();
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();
            serviciosAutores = Substitute.For<IServicioAutores>();

            controller = new AutoresController(context, mapper, almacenadorArchivos, logger, outputCacheStore, serviciosAutores);
        }

        // GET - /api/autor/id | Revisar que un autor exista, debe retornar 404
        [TestMethod]
        public async Task Get_Retorna404_CuandoAutorConIdNoExiste()
        {
            // Prueba
            var respuesta = await controller.Get(1);

            // Verificación
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        // GET - /api/autor/id | Revisar que un autor exista, debe retortar 200
        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorConIdExiste()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor { Nombres = "Felipe", Apellidos = "Gavilan" });
            context.Autores.Add(new Autor { Nombres = "Claudia", Apellidos = "Rodriguez" });
            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificación
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
        }

        // GET - /api/autor/id | Revisar que un autor con libros exista, debe retortar un autor con sus libros
        [TestMethod]
        public async Task Get_RetornaAutorConLibros_CuandoAutorTieneLibros()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            var libro1 = new Libro { Titulo = "Libro 1" };
            var libro2 = new Libro { Titulo = "Libro 2" };
            var autor = new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Libros = new List<AutorLibro> {
                                new AutorLibro { Libro = libro1 },
                                new AutorLibro { Libro = libro2 }
                            }
            };

            context.Add(autor);
            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificación
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
            Assert.AreEqual(expected: 2, actual: resultado.Libros.Count);
        }

        // GET - /api/autores | Obtiene un listado de autores y revisa que se use "ServicioAutores", debe retornar un objeto paginacionDTO
        [TestMethod]
        public async Task Get_DebeLlamarGetDelServicioAutores()
        {
            // Preparación
            var paginacionDTO = new PaginacionDTO(2, 3);

            // Prueba
            await controller.Get(paginacionDTO);

            // Verificacion
            await serviciosAutores.Received(1).Get(paginacionDTO);
        }

        // POST - /api/autor | Revisar que un autor se cree, debe retornar "1" al menos al buscarse en la tabla
        [TestMethod]
        public async Task Post_DebeCrearAutor_CuandoEnviamosAutor()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            var nuevoAutor = new AutorCreacionDTO { Nombres = "Nuevo", Apellidos = "Autor" };

            // Prueba
            var respuesta = await controller.Post(nuevoAutor);

            // Verificación
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado);

            var context2 = ConstruirContext(nombreBD);
            var cantidad = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidad);
        }

        // PUT - /api/autor | Revisar que no exista un Autor, debe retornar 404
        [TestMethod]
        public async Task Put_Retorna404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO: null!);

            // Verifiación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        // PUT - /api/autor | Revisar que actualice un sin Autor, debe retornar un Autor sin foto y que no se use el almacenadorArchivos
        [TestMethod]
        public async Task Put_ActualizaAutor_CuandoEnviamosAutorSinFoto()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "Id"
            });
            await context.SaveChangesAsync();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Felipe2",
                Apellidos = "Gavilan2",
                Identificacion = "Id2"
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Gavilan2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!);
        }

        // PUT - /api/autor | Revisar que actualice un con Autor, debe retornar un Autor con foto y que sí se use el almacenadorArchivos
        [TestMethod]
        public async Task Put_ActualizaAutor_CuandoEnviamosAutorConFoto()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);

            var urlAnterior = "URL-1";
            var urlNueva = "URL-2";
            almacenadorArchivos.Editar(default, default!, default!).ReturnsForAnyArgs(urlNueva);

            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "Id",
                Foto = urlAnterior
            });
            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Felipe2",
                Apellidos = "Gavilan2",
                Identificacion = "Id2",
                Foto = formFile
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Gavilan2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNueva, actual: autorActualizado.Foto);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Editar(urlAnterior, contenedor, formFile);
        }

        // PATCH - /api/autor | Revisar que se haya enviado un patchDoc, debe retornar 400
        [TestMethod]
        public async Task Patch_Retorna400_CuandoPatchDocEsNulo()
        {
            // Prueba
            var respuesta = await controller.Patch(1, patchDoc: null!);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(400, resultado!.StatusCode);
        }

        // PATCH - /api/autor | Revisar que un Autor no se actualice porque no existe, debe retornar 404
        [TestMethod]
        public async Task Patch_Retorna400_CuandoAutorNoExiste()
        {
            // Preparación
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc!);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        // PATCH - /api/autor | Revisar que un Autor no se actualice por errores de validación
        [TestMethod]
        public async Task Patch_RetornaValidationProblem_CuandoHayErrorDeValidacion()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            });
            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var mensajeDeError = "mensaje de error";
            controller.ModelState.AddModelError("", mensajeDeError);

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc!);

            // Verificacion
            var resultado = respuesta as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }

        // PATCH - /api/autor | Revisar que una columna de un Autor se actualice, debe retornar 204
        [TestMethod]
        public async Task Patch_ActualizaUnCampo_CuandoSeLeEnviaUnaOperacion()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123",
                Foto = "URL-1"
            });
            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();
            patchDoc.Operations.Add(new Operation<AutorPatchDTO>("replace", "/nombres", null, "Felipe2"));

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, resultado!.StatusCode);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);

            var context2 = ConstruirContext(nombreBD);
            var autorBD = await context2.Autores.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", autorBD.Nombres);
            Assert.AreEqual(expected: "Gavilan", autorBD.Apellidos);
            Assert.AreEqual(expected: "123", autorBD.Identificacion);
            Assert.AreEqual(expected: "URL-1", autorBD.Foto);
        }

        // DELETE - /api/autor | Revisar ocurra un error al no existir un Autor que borrar, retorna 404
        [TestMethod]
        public async Task Delete_Retorna404_CuandoAutorNoExiste() {
            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        // DELETE - /api/autor | Revisar que se borra un autor, debe retornar 204
        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorExiste() {
            // Preparación
            var urlFoto = "URL-1";

            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor {
                Nombres = "Autor1",
                Apellidos = "Autor1",
                Foto = urlFoto
            });
            context.Autores.Add(new Autor
            {
                Nombres = "Autor2",
                Apellidos = "Autor2"
            });

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);
            var cantidadAutores = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidadAutores);

            var autor2Existe = await context2.Autores.AnyAsync(x => x.Nombres == "Autor2");
            Assert.IsTrue(autor2Existe);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Borrar(urlFoto, contenedor);
        }
    }
}
