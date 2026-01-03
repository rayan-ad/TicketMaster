using Microsoft.AspNetCore.SignalR;

namespace TicketMaster.Hubs
{
    /// <summary>
    /// Hub SignalR pour les mises à jour en temps réel des sièges.
    /// Permet de notifier tous les clients connectés quand un siège change d'état.
    /// </summary>
    public class SeatHub : Hub
    {
        /// <summary>
        /// Notifie tous les clients qu'un siège a été réservé temporairement
        /// </summary>
        public async Task NotifySeatReserved(int eventId, int seatId, int userId)
        {
            await Clients.All.SendAsync("SeatReserved", new
            {
                eventId,
                seatId,
                userId,
                state = "ReservedTemp",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifie tous les clients qu'un siège a été libéré
        /// </summary>
        public async Task NotifySeatReleased(int eventId, int seatId)
        {
            await Clients.All.SendAsync("SeatReleased", new
            {
                eventId,
                seatId,
                state = "Free",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifie tous les clients qu'un siège a été payé
        /// </summary>
        public async Task NotifySeatPaid(int eventId, int seatId)
        {
            await Clients.All.SendAsync("SeatPaid", new
            {
                eventId,
                seatId,
                state = "Paid",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifie tous les clients de plusieurs sièges réservés
        /// </summary>
        public async Task NotifySeatsReserved(int eventId, List<int> seatIds, int userId)
        {
            await Clients.All.SendAsync("SeatsReserved", new
            {
                eventId,
                seatIds,
                userId,
                state = "ReservedTemp",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifie tous les clients de plusieurs sièges libérés
        /// </summary>
        public async Task NotifySeatsReleased(int eventId, List<int> seatIds)
        {
            await Clients.All.SendAsync("SeatsReleased", new
            {
                eventId,
                seatIds,
                state = "Free",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifie tous les clients de plusieurs sièges payés
        /// </summary>
        public async Task NotifySeatsPaid(int eventId, List<int> seatIds)
        {
            await Clients.All.SendAsync("SeatsPaid", new
            {
                eventId,
                seatIds,
                state = "Paid",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Appelé quand un client se connecte au hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"Client connecté: {Context.ConnectionId}");
        }

        /// <summary>
        /// Appelé quand un client se déconnecte du hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"Client déconnecté: {Context.ConnectionId}");
        }
    }
}
