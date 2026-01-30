using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BibliotecaAPI.Utilidades
{
    // Clase de extensión para implementar la clase "LimitarPeticionesMiddleware" como un middleware (Se usa en la clase Program)
    public static class LimitarPeticionesMiddlewareExtensions {
        public static IApplicationBuilder UseLimitarPeticiones(this IApplicationBuilder app) {
            return app.UseMiddleware<LimitarPeticionesMiddleware>();
        }
    }

    public class LimitarPeticionesMiddleware {
        private readonly RequestDelegate next;
        private readonly IOptionsMonitor<LimitarPeticionesDTO> optionsMonitorLimitarPeticiones;

        public LimitarPeticionesMiddleware(RequestDelegate next, IOptionsMonitor<LimitarPeticionesDTO> optionsMonitorLimitarPeticiones) {
            this.next = next;
            this.optionsMonitorLimitarPeticiones = optionsMonitorLimitarPeticiones;
        }

        public async Task InvokeAsync(HttpContext httpContext, ApplicationDbContext context) {
            // Validar si la petición se realiza directamente a un endpoint, si no avanza al siguiente middleware
            var endpoint = httpContext.GetEndpoint();
            if (endpoint is null) {
                await next(httpContext);
                return;
            }

            // Validar si el endpoint consultado contiene el atributo "DeshabilitarLimitarPeticionesAttribute", si lo tiene avanza al siguiente middleware
            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor is not null) {
                var accionTieneAtributoIgnorarLimitarPeticiones =
                    actionDescriptor.MethodInfo
                    .GetCustomAttributes(typeof(DeshabilitarLimitarPeticionesAttribute), inherit: true)
                    .Any();
                
                var controladorTieneAtributoIgnorarLimitarPeticiones =
                    actionDescriptor.ControllerTypeInfo
                    .GetCustomAttributes(typeof(DeshabilitarLimitarPeticionesAttribute), inherit: true)
                    .Any();

                if (accionTieneAtributoIgnorarLimitarPeticiones || controladorTieneAtributoIgnorarLimitarPeticiones) {
                    await next(httpContext);
                    return;
                }
            }
            
            var limitarPeticionesDTO = optionsMonitorLimitarPeticiones.CurrentValue;
            var llaveStringValues = httpContext.Request.Headers["X-Api-key"];

            if (llaveStringValues.Count == 0) {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Debe proveer la llave en la cabecera X-Api-Key");
                return;
            }

            if (llaveStringValues.Count > 1) {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Solo una llave debe estar presente");
                return;
            }

            var llave = llaveStringValues[0];
            
            var llaveDB = await context.LlavesAPI
                .Include(x => x.RestriccionesDominio)
                .Include(x => x.RestriccionesIp)
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Llave == llave);
            if (llaveDB is null) {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("La llave no existe");
                return;
            }

            if (!llaveDB.Activa) {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("La llave se encuentra inactiva");
                return;
            }

            var restriccionesSuperadas = PeticionSuperaAlgunaDeLasRestricciones(llaveDB, httpContext);
            if (!restriccionesSuperadas) {
                httpContext.Response.StatusCode = 403;
                return;
            }

            if (llaveDB.TipoLlave == TipoLlave.Gratuita) {
                var hoy = DateTime.UtcNow.Date;
                var cantidadPeticionesRealizadasHoy = await context.Peticiones
                    .CountAsync(x => x.LlaveId == llaveDB.Id && x.FechaPeticion >= hoy);

                if (limitarPeticionesDTO.PeticionesPorDiaGratuito <= cantidadPeticionesRealizadasHoy) {
                    httpContext.Response.StatusCode = 429; // Too many request
                    await httpContext.Response.WriteAsync("Ha exedido el limite de peticiones por dia. Si desea realizar mas peticiones, actualice su suscripción a una cuenta profesional.");
                    return;
                }
            } else if (llaveDB.Usuario!.MalaPaga) {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("El usuario es un mala paga");
                return;
            }

                // Se genera un registro de la petición en la tabla "Peticion"
                var peticion = new Peticion()
                {
                    LlaveId = llaveDB.Id,
                    FechaPeticion = DateTime.UtcNow
                };
            context.Add(peticion);
            await context.SaveChangesAsync();

            await next(httpContext);
        }

        private bool PeticionSuperaAlgunaDeLasRestricciones(LlaveAPI llaveAPI, HttpContext httpContext) {
            var hayRestricciones = llaveAPI.RestriccionesDominio.Any() || llaveAPI.RestriccionesIp.Any();
            if (!hayRestricciones) {
                return true;
            }
            
            var peticionSuperaTodasLasRestriccionesDeDominio = PeticionSuperaTodasLasRestriccionesDeDominio(llaveAPI.RestriccionesDominio, httpContext);
            var peticionSuperaTodasLasRestriccionesDeIP = PeticionSuperaTodasLasRestriccionesDeIP(llaveAPI.RestriccionesIp, httpContext);

            return peticionSuperaTodasLasRestriccionesDeDominio || peticionSuperaTodasLasRestriccionesDeIP;
        }

        private bool PeticionSuperaTodasLasRestriccionesDeDominio(List<RestriccionDominio> restricciones, HttpContext httpContext) {
            if (restricciones is null || restricciones.Count == 0) {
                return false;
            }

            var referer = httpContext.Request.Headers["referer"].ToString();
            if (referer == string.Empty) {
                return false;
            }

            var miURI = new Uri(referer);
            var dominio = miURI.Host;

            var superaRestriccion = restricciones.Any(x => x.Dominio == dominio);
            return superaRestriccion;
        }

        private bool PeticionSuperaTodasLasRestriccionesDeIP(List<RestriccionIP> restricciones, HttpContext httpContext) {
            if (restricciones is null || restricciones.Count == 0) {
                return false;
            }

            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
            if (remoteIpAddress is null) {
                return false;
            }

            var IP = remoteIpAddress.ToString();
            if (IP == string.Empty) {
                return false;
            }

            var superaRestriccion = restricciones.Any(x => x.IP == IP);
            return superaRestriccion;
        }
    }
}
