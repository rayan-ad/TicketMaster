namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour GET Event détaillé
    /// POURQUOI ?
    /// - Évite les références circulaires (Event -> Venue -> Events -> ...)
    /// - Ajoute des propriétés CALCULÉES (TotalSeats, AvailableSeats, FillRate)
    /// - Ne renvoie QUE ce dont le client a besoin
    /// </summary>
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageEvent { get; set; }

        // Venue information only (not the full object)
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int VenueCapacity { get; set; }

        // Calculated properties by Service (not stored in DB)
        public int TotalSeats { get; set; }           // Nombre total de sièges
        public int AvailableSeats { get; set; }       // Sièges libres
        public int ReservedSeats { get; set; }        // Sièges réservés temporairement
        public int SoldSeats { get; set; }            // Sièges payés
        public decimal FillRate { get; set; }         // Taux de remplissage en %
        public decimal PotentialRevenue { get; set; } // Revenus potentiels si tout vendu
        public decimal ActualRevenue { get; set; }    // Revenus actuels (sièges payés)
    }
}
