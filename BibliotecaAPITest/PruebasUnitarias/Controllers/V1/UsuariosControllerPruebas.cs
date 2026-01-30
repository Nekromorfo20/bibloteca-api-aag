using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPITest.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class UsuariosControllerPruebas : BasePruebas {
        private string nombreBD = Guid.NewGuid().ToString();
        private UserManager<Usuario> userManager = null!;
        private SignInManager<Usuario> signInManager = null!;
        private UsuariosController controller = null!;
        private IServicioLlaves servicioLlaves = null!;

        [TestInitialize]
        public void Setup() {
            var context = ConstruirContext(nombreBD);
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);
            var miConfiguracion = new Dictionary<string, string>
            {
                { "llavejwt", "wasdfaskceadaskacnksndasdasdasdaxdas" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(miConfiguracion!)
                .Build();

            var contextAccesor = Substitute.For<IHttpContextAccessor>();
            var userClaimsFactory = Substitute.For<IUserClaimsPrincipalFactory<Usuario>>();
            signInManager = Substitute.For<SignInManager<Usuario>>(userManager, contextAccesor, userClaimsFactory, null, null, null, null);

            var serviciosUsuarios = Substitute.For<IServiciosUsuarios>();

            var mapper = ConfigurarAutomapper();

            servicioLlaves = Substitute.For<IServicioLlaves>();

            controller = new UsuariosController(userManager, configuration, signInManager, serviciosUsuarios, context, mapper, servicioLlaves);
        }

        // POST - /api/registro | Realiza un registro fallido, debe retorna un problemDetails
        [TestMethod]
        public async Task Registrar_DevuleveValdiationProblem_CuandoNoEsExitoso() {
            // Preparación
            var mensajeDeError = "prueba";
            var credenciales = new CredencialesUsuarioDTO {
                Email = "prueba@hotmail.com",
                Password = "aA12345!"
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                .Returns(IdentityResult.Failed(new IdentityError {
                    Code = "prueba",
                    Description = mensajeDeError
                }));

            // Prueba
            var respuesta = await controller.Registrar(credenciales);

            // Verificación
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }

        // POST - /api/registro | Realiza un registro exitoso, debe retorna un token en la respuesta
        [TestMethod]
        public async Task Registrar_DevuleveToken_CuandoEsExitoso(){
            // Preparación
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA12345!"
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            // Prueba
            var respuesta = await controller.Registrar(credenciales);

            // Verificación
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblema_CuandoUsuarioNoExiste() {
            // Preparación
            var credenciales = new CredencialesUsuarioDTO {
                Email = "prueba@hotmail.com",
                Password = "aA12345!"
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(null!));

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Verifiación
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto", actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblema_CuandoLoginEsIncorrecto() {
            // Preparación
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA12345!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario!));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false).Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Verifiación
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto", actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveToken_CuandoLoginEsCorrecto() {
            // Preparación
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA12345!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario!));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false).Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Verifiación
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }
    }
}
