using Microsoft.EntityFrameworkCore;
using TicketMaster.Enum;
using TicketMaster.Models;

namespace TicketMaster.DataAccess
{
    public class TicketMasterContext : DbContext
    {
        public TicketMasterContext(DbContextOptions<TicketMasterContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<PricingZone> PricingZones { get; set; }
        public DbSet<ReservationSeat> ReservationsSeat { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SeatReservationState> SeatReservationStates { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Définition de la clé composite ReservationSeat
            modelBuilder.Entity<ReservationSeat>()
                .HasKey(rs => new { rs.ReservationId, rs.SeatId });

            // Relation Reservation ↔ ReservationSeat (Cascade delete)
            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Reservation)
                .WithMany(r => r.ReservationSeats)
                .HasForeignKey(rs => rs.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation Seat ↔ ReservationSeat (Restrict pour éviter suppression accidentelle)
            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Seat)
                .WithMany(s => s.ReservationSeats)
                .HasForeignKey(rs => rs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation SeatReservationState (index unique Event + Seat)
            modelBuilder.Entity<SeatReservationState>()
                .HasIndex(srs => new { srs.EventId, srs.SeatId })
                .IsUnique();

            modelBuilder.Entity<SeatReservationState>()
                .HasOne(srs => srs.Event)
                .WithMany()
                .HasForeignKey(srs => srs.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SeatReservationState>()
                .HasOne(srs => srs.Seat)
                .WithMany(s => s.ReservationStates)
                .HasForeignKey(srs => srs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration Ticket (billet électronique)
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.TicketNumber)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Reservation)
                .WithMany(r => r.Tickets)
                .HasForeignKey(t => t.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat)
                .WithMany()
                .HasForeignKey(t => t.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // SEED DATA - Données initiales
            // ============================================================

            // --- 1. VENUES (Stades/Salles) ---
            modelBuilder.Entity<Venue>().HasData(
                new Venue { Id = 1, Name = "Stade National", Capacity = 1000 },  // Réduit pour générer moins de sièges
                new Venue { Id = 2, Name = "Salle Opéra Royale", Capacity = 500 }
            );

            // --- 2. PRICING ZONES (Zones tarifaires) ---
            // STADE NATIONAL (VenueId = 1)
            modelBuilder.Entity<PricingZone>().HasData(
                new PricingZone { Id = 1, Name = "VIP", Price = 120m, Color = "#FFD700", VenueId = 1 },
                new PricingZone { Id = 2, Name = "Standard", Price = 60m, Color = "#1E90FF", VenueId = 1 },
                new PricingZone { Id = 3, Name = "Eco", Price = 30m, Color = "#32CD32", VenueId = 1 }
            );

            // SALLE OPÉRA (VenueId = 2)
            modelBuilder.Entity<PricingZone>().HasData(
                new PricingZone { Id = 4, Name = "Orchestre", Price = 80m, Color = "#DAA520", VenueId = 2 },
                new PricingZone { Id = 5, Name = "Balcon", Price = 50m, Color = "#ADD8E6", VenueId = 2 }
            );

            // --- 3. EVENTS (avec images) ---
            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    Id = 1,
                    Name = "Match d'ouverture - Finale",
                    Type = "Sport",
                    Description = "Premier match de la saison dans le stade principal. Ambiance garantie !",
                    Date = new DateTime(2025, 6, 15, 19, 30, 00, DateTimeKind.Utc),
                    VenueId = 1,
                    ImageEvent = "https://images.unsplash.com/photo-1522778119026-d647f0596c20?w=800"
                },
                new Event
                {
                    Id = 2,
                    Name = "Concert Symphonique - Mozart",
                    Type = "Musique",
                    Description = "Grand concert de l'Orchestre Philharmonique. Répertoire Mozart.",
                    Date = new DateTime(2025, 7, 10, 20, 00, 00, DateTimeKind.Utc),
                    VenueId = 2,
                    ImageEvent = "https://images.unsplash.com/photo-1507838153414-b4b713384a76?w=800"
                },
                new Event
                {
                    Id = 3,
                    Name = "Gala de Danse Contemporaine",
                    Type = "Culturel",
                    Description = "Soirée de danse classique et contemporaine avec les meilleurs danseurs.",
                    Date = new DateTime(2025, 8, 20, 18, 00, 00, DateTimeKind.Utc),
                    VenueId = 2,
                    ImageEvent = "https://images.unsplash.com/photo-1508807526345-15e9b5f4eaff?w=800"
                },
                new Event
                {
                    Id = 4,
                    Name = "Coupe Nationale - Demi-finale",
                    Type = "Sport",
                    Description = "Demi-finale de la coupe nationale. Ne manquez pas ce match décisif !",
                    Date = new DateTime(2025, 9, 5, 20, 30, 00, DateTimeKind.Utc),
                    VenueId = 1,
                    ImageEvent = "https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=800"
                }
            );

            // --- 4. USERS (utilisateurs de test) ---
            // Tous les utilisateurs ont le mot de passe: "password123"
            // Hash BCrypt pré-généré avec workFactor = 11
            const string testPasswordHash = "$2a$11$bS6s6WHQI5a9/p66D.rWXe9841mxPwieaiKA.Kt6pn/TVgyzOMLCu";

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Admin Principal",
                    Email = "admin@test.com",
                    PasswordHash = testPasswordHash,
                    Role = UserRole.Admin
                },
                new User
                {
                    Id = 2,
                    Name = "Organisateur Pro",
                    Email = "orga@test.com",
                    PasswordHash = testPasswordHash,
                    Role = UserRole.Organisateur
                },
                new User
                {
                    Id = 3,
                    Name = "Client Test",
                    Email = "client@test.com",
                    PasswordHash = testPasswordHash,
                    Role = UserRole.Client
                }
            );

            // --- 5. SEATS (AUTO-GÉNÉRÉS) ---
            GenerateSeats(modelBuilder);

            // --- 6. SEAT RESERVATION STATES (État par event) ---
            GenerateSeatReservationStates(modelBuilder);
        }

        /// <summary>
        /// Génère automatiquement les sièges pour chaque venue avec leurs pricing zones
        /// STADE NATIONAL (1000 sièges) :
        ///   - VIP (Zone 1) : 100 sièges (10 rangées de 10)
        ///   - Standard (Zone 2) : 500 sièges (50 rangées de 10)
        ///   - Eco (Zone 3) : 400 sièges (40 rangées de 10)
        ///
        /// SALLE OPÉRA (500 sièges) :
        ///   - Orchestre (Zone 4) : 300 sièges (30 rangées de 10)
        ///   - Balcon (Zone 5) : 200 sièges (20 rangées de 10)
        /// </summary>
        private void GenerateSeats(ModelBuilder modelBuilder)
        {
            var seats = new List<Seat>();
            int seatId = 1;

            // ========================================
            // STADE NATIONAL (Venue 1)
            // ========================================

            // VIP (100 sièges - Zone 1)
            for (int row = 1; row <= 10; row++)
            {
                for (int number = 1; number <= 10; number++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        Row = $"V{row:D2}",
                        Number = number,
                        PricingZoneId = 1
                    });
                }
            }

            // Standard (500 sièges - Zone 2)
            for (int row = 1; row <= 50; row++)
            {
                for (int number = 1; number <= 10; number++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        Row = $"S{row:D2}",
                        Number = number,
                        PricingZoneId = 2
                    });
                }
            }

            // Eco (400 sièges - Zone 3)
            for (int row = 1; row <= 40; row++)
            {
                for (int number = 1; number <= 10; number++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        Row = $"E{row:D2}",
                        Number = number,
                        PricingZoneId = 3
                    });
                }
            }

            // ========================================
            // SALLE OPÉRA (Venue 2)
            // ========================================

            // Orchestre (300 sièges - Zone 4)
            for (int row = 1; row <= 30; row++)
            {
                for (int number = 1; number <= 10; number++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        Row = $"O{row:D2}",
                        Number = number,
                        PricingZoneId = 4
                    });
                }
            }

            // Balcon (200 sièges - Zone 5)
            for (int row = 1; row <= 20; row++)
            {
                for (int number = 1; number <= 10; number++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        Row = $"B{row:D2}",
                        Number = number,
                        PricingZoneId = 5
                    });
                }
            }

            // Ajout des sièges au modèle
            modelBuilder.Entity<Seat>().HasData(seats);
        }

        /// <summary>
        /// Génère les états de réservation pour chaque event
        /// ARCHITECTURE CORRIGÉE :
        /// - Chaque Event a ses propres états de sièges
        /// - Un siège peut être libre pour Event 1 et réservé pour Event 2
        /// - Les sièges sont des templates réutilisables
        /// </summary>
        private void GenerateSeatReservationStates(ModelBuilder modelBuilder)
        {
            var states = new List<SeatReservationState>();
            int stateId = 1;

            // Liste des events (IDs dans le seed data)
            int[] eventIds = { 1, 2, 3, 4 };

            // Pour chaque event
            foreach (var eventId in eventIds)
            {
                // Déterminer quels sièges sont disponibles pour cet event
                // Event 1 et 4 : Stade National (sièges 1-1000)
                // Event 2 et 3 : Salle Opéra (sièges 1001-1500)
                int startSeatId = (eventId == 1 || eventId == 4) ? 1 : 1001;
                int endSeatId = (eventId == 1 || eventId == 4) ? 1000 : 1500;

                // Créer un état "Free" pour chaque siège de cet event
                for (int seatId = startSeatId; seatId <= endSeatId; seatId++)
                {
                    states.Add(new SeatReservationState
                    {
                        Id = stateId++,
                        EventId = eventId,
                        SeatId = seatId,
                        State = SeatStatus.Free,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            modelBuilder.Entity<SeatReservationState>().HasData(states);
        }
    }
}
