namespace BibliotecaAPI.DTOs
{
    public record PaginacionDTO (int Pagina = 1, int RecordsPorPagina = 10) {
        private const int CantidadMaximaRecordsPorPagina = 50;

        // Evita ir a paginas negativas (como -1)
        public int Pagina { get; init; } = Math.Max(1, Pagina);
        // Si la pagina actual s -1 se retorna a la 1, si es mayor al total de paginas de retorna a la página actual
        public int RecordsPorPagina { get; init; } = Math.Clamp(RecordsPorPagina, 1, CantidadMaximaRecordsPorPagina);
    }
}
