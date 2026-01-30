using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreaStoreProcedure_FacturasCrear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE Facturas_Crear
					-- Add the parameters for the stored procedure here
					@fechaInicio datetime,
					@fechaFin datetime
				AS
				BEGIN
					-- SET NOCOUNT ON added to prevent extra result sets from
					-- interfering with SELECT statements.
					SET NOCOUNT ON;

					-- Insert statements for procedure here
					DECLARE @montoCadaPeticion decimal(4,4) = 1.0/2; -- 1 dolar por cada 2 peticiones

					INSERT INTO Facturas(UsuarioId, Monto, FechaEmision, FechaLimiteDePago, Pagada) 
					SELECT
					UsuarioId,
					COUNT (*) * @montoCadaPeticion as Monto,
					GETDATE() AS FechaEmision,
					DATEADD(d, 60, GETDATE()) as FechaLimiteDePago,
					0 as Pagada
					FROM Peticiones
					INNER JOIN LlavesAPI
					ON LlavesAPI.Id = Peticiones.LlaveId 
					WHERE LlavesAPI.TipoLlave != 1 AND FechaPeticion >= @fechaInicio AND FechaPeticion <= @fechaFin
					GROUP BY UsuarioId;

					INSERT INTO FacturasEmitidas(Mes, Ano)
					SELECT
						CASE MONTH(GetDate())
						WHEN 1 THEN 12
						ELSE MONTH(GetDate()) - 1 END AS MES,
						CASE MONTH(GetDate())
						WHEN 1 THEN YEAR(GetDate()) - 1
						ELSE YEAR(GetDate()) END AS ANO;
				END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("DROP PROCEDURE Facturas_Crear");
        }
    }
}
