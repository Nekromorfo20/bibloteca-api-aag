using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Datos
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Autor> Autores { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<AutorLibro> AutoresLibros { get; set; }
        public DbSet<Error> Errores { get; set; }
        public DbSet<LlaveAPI> LlavesAPI { get; set; }
        public DbSet<Peticion> Peticiones { get; set; }
        public DbSet<RestriccionDominio> RestriccionesDominio { get; set; }
        public DbSet<RestriccionIP> RestriccionesIp { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaEmitida> FacturasEmitidas { get; set; }

    }
}
