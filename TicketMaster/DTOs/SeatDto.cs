using TicketMaster.Enum;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour les sièges avec toutes les infos nécessaires pour l'affichage
    /// </summary>
    public class SeatDto
    {
        public int Id { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
        public string State { get; set; } = string.Empty;  // "Free", "ReservedTemp", "Paid"

        // Infos de la zone tarifaire (propriétés plates)
        public int PricingZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string ZoneColor { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Objet zone complet pour compatibilité frontend
        public ZoneDto? Zone { get; set; }
    }

    /// <summary>
    /// DTO pour la zone tarifaire (utilisé dans les réponses de sièges)
    /// </summary>
    public class ZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
