using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class RestriccionIPDTO {
        public int Id { get; set; }
        [Required]
        public required string IP { get; set; }
    }
}
