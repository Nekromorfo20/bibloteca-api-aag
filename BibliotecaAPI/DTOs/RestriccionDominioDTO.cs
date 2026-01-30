using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class RestriccionDominioDTO {
        public int Id { get; set; }
        public required string Dominio { get; set; }
    }
}
