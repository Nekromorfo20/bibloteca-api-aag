using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class EditarClaimDTO
    {
        [EmailAddress]
        public required string Email { get; set; }
    }
}
