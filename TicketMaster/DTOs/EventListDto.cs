namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour GET liste d'Events (version ALLÉGÉE)
    /// POURQUOI ?
    /// - Plus léger pour afficher une liste (pas besoin de toutes les stats)
    /// - Réduit la bande passante
    /// - Améliore les performances
    /// </summary>
    public class EventListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? ImageEvent { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Only essential stats for the list
        public int AvailableSeats { get; set; }
        public decimal FillRate { get; set; }
    }
}
