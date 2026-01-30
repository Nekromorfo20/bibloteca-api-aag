using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    [PrimaryKey("Mes", "Ano")]
    public class FacturaEmitida {
         public int Mes { get; set; }
        public int Ano { get; set; }
    }
}
