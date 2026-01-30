using BibliotecaAPI.Datos;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Jobs
{
    public class FacturasBackgroundService : BackgroundService {
        private readonly IServiceProvider services;

        public FacturasBackgroundService(IServiceProvider services) {
            this.services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                while (!stoppingToken.IsCancellationRequested) {
                    using (var scope = services.CreateScope()) {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        Console.WriteLine("¡EJECUTANDO PROCESO DE EMISIÓN DE FACTURAS!");
                        await EmitirFacturas(context);
                        Console.WriteLine("¡EJECUTANDO PROCESO DE COMPROBACIÓN USUARIOS MALA PAGA!");
                        await SetearUsuariosMalaPaga(context);
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    }
                }
            } catch (OperationCanceledException) {
                // Aquí podemos ejecutar un código personalizado al detener la ejecución del Job
            }
        }

        private async Task SetearUsuariosMalaPaga(ApplicationDbContext context) {
            await context.Database.ExecuteSqlAsync($"EXEC Usuarios_SetearMalaPaga");
        }

        private async Task EmitirFacturas(ApplicationDbContext context) {
            var hoy = DateTime.Today;
            var fechaComparacion = hoy.AddMonths(-1);

            var facturasDelMesYaFueronEmitidas = await context.FacturasEmitidas
                .AnyAsync(x => x.Ano == fechaComparacion.Year &&
                x.Mes == fechaComparacion.Month);
            if (!facturasDelMesYaFueronEmitidas) {
                var fechaInicio = new DateTime(fechaComparacion.Year, fechaComparacion.Month, 1);
                var fechaFin = fechaInicio.AddMonths(1);
                // Aqui se llama al StoreProcedure "Facturas_Crear"
                await context.Database.ExecuteSqlAsync($"EXEC Facturas_Crear {fechaInicio.ToString("yyyy-MM-dd")}, {fechaFin.ToString("yyyy-MM-dd")}");
            }
        }
    }
}
