using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITest.PruebasUnitarias.Servicios
{
    [TestClass]
    public class ServiciosUsuariosPruebas {
        private UserManager<Usuario> userManager = null!;
        private IHttpContextAccessor contextAccesor = null!;
        private ServiciosUsuarios serviciosUsuarios = null!;

        [TestInitialize]
        public void Setup() {
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);
            contextAccesor = Substitute.For<IHttpContextAccessor>();
            serviciosUsuarios = new ServiciosUsuarios(userManager, contextAccesor);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornaNulo_CuandoNoHayClaimEmail() {
            // Preparación
            var httpContext = new DefaultHttpContext();
            contextAccesor.HttpContext.Returns(httpContext);

            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNull(usuario);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornaUsuario_CuandoHayClaimEmail() {
            // Preparación
            var email = "prueba@email.com";
            var usuarioEsperado = new Usuario { Email = email };
            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(usuarioEsperado));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contextAccesor.HttpContext.Returns(httpContext);

            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNotNull(usuario);
            Assert.AreEqual(expected: email, actual: usuario.Email);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornaNulo_CuandoUsuarioNoExiste() {
            // Preparación
            var email = "prueba@email.com";
            var usuarioEsperado = new Usuario { Email = email };
            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<Usuario>(null!));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contextAccesor.HttpContext.Returns(httpContext);

            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNull(usuario);
        }
    }
}
