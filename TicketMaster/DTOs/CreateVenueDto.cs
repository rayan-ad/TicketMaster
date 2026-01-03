using System.ComponentModel.DataAnnotations;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour créer un venue avec zones tarifaires
    /// Les sièges seront générés automatiquement
    /// </summary>
    public class CreateVenueDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La capacité est requise")]
        [Range(1, 100000, ErrorMessage = "La capacité doit être entre 1 et 100000")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Au moins une zone tarifaire est requise")]
        [MinLength(1, ErrorMessage = "Au moins une zone tarifaire est requise")]
        public List<PricingZoneDto> PricingZones { get; set; } = new();
    }

    public class PricingZoneDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix doit être supérieur à 0")]
        public decimal Price { get; set; }

        [Required]
        public string Color { get; set; } = string.Empty;

        [Required]
        [Range(1, 50000, ErrorMessage = "Le nombre de sièges doit être entre 1 et 50000")]
        public int SeatCount { get; set; }
    }
}
