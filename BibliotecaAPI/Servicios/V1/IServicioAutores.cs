using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Servicios.V1
{
    public interface IServicioAutores
    {
        Task<IEnumerable<AutorDTO>> Get(PaginacionDTO paginacionDTO);
    }
}