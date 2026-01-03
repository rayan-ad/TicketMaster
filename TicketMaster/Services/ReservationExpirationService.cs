using Microsoft.AspNetCore.SignalR;
using TicketMaster.Hubs;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service en arrière-plan qui libère automatiquement les réservations expirées.
    /// S'exécute toutes les minutes pour vérifier et libérer les sièges.
    /// </summary>
    public class ReservationExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<SeatHub> _hubContext;
        private readonly ILogger<ReservationExpirationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public ReservationExpirationService(
            IServiceProvider serviceProvider,
            IHubContext<SeatHub> hubContext,
            ILogger<ReservationExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Tâche exécutée en arrière-plan
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service d'expiration des réservations démarré.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ReleaseExpiredReservationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la libération des réservations expirées.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Service d'expiration des réservations arrêté.");
        }

        /// <summary>
        /// Libère les réservations expirées et notifie via SignalR
        /// </summary>
        private async Task ReleaseExpiredReservationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

            _logger.LogInformation("Vérification des réservations expirées...");

            await reservationService.ReleaseExpiredReservationsAsync();

            _logger.LogInformation("Libération des réservations expirées terminée.");
        }
    }
}
