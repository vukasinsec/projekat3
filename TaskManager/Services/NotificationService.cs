using MongoDB.Driver;
using TaskManager.Data;
using TaskManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Hubs;
using Microsoft.AspNetCore.SignalR;


namespace TaskManager.Services
{
    public class NotificationService
    {
        private readonly IMongoCollection<Notification> _notifications;
        private readonly ProjectService _projectService;
        private readonly IHubContext<NotificationsHub> _hubContext;  // SignalR hub context

        public NotificationService(MongoDbContext context, ProjectService projectService, IHubContext<NotificationsHub> hubContext)
        {
            _notifications = context.GetCollection<Notification>("Notifications");
            _projectService = projectService;
            _hubContext = hubContext;
        }



        // Dohvati sve notifikacije za korisnika
        public async Task<List<Notification>> GetByUserIdAsync(string userId)
        {
            return await _notifications.Find(n => n.UserId == userId).ToListAsync();
        }

        // Dohvati samo nepročitane notifikacije
        public async Task<List<Notification>> GetUnreadByUserIdAsync(string userId)
        {
            return await _notifications.Find(n => n.UserId == userId && !n.IsRead).ToListAsync();
        }

        // Kreiraj novu notifikaciju
        public async Task CreateAsync(Notification notification)
        {
            await _notifications.InsertOneAsync(notification);
            // Pošalji notifikaciju korisniku u realnom vremenu
            await _hubContext.Clients.User(notification.UserId)
                .SendAsync("ReceiveNotification", notification);


        }

        // Obeleži notifikaciju kao pročitanu
        public async Task MarkAsReadAsync(string id)
        {
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            await _notifications.UpdateOneAsync(n => n.Id == id, update);
        }

        // Obriši notifikaciju (ako želiš)
        public async Task DeleteAsync(string id)
        {
            await _notifications.DeleteOneAsync(n => n.Id == id);
        }

        // Obriši sve notifikacije korisnika (opciono)
        public async Task DeleteAllForUserAsync(string userId)
        {
            await _notifications.DeleteManyAsync(n => n.UserId == userId);
        }
        public async Task AcceptCollaborationRequestAsync(string notificationId)
        {
            var notification = await _notifications.Find(n => n.Id == notificationId).FirstOrDefaultAsync();
            if (notification == null) throw new Exception("Notification not found");
            if (notification.Type != NotificationType.CollaborationRequest)
                throw new Exception("Invalid notification type");

            // Dodaj korisnika kao kolaboratora na projekat
            if (notification.ProjectId != null && notification.SenderUserId != null)
            {
                await _projectService.AddCollaboratorAsync(notification.ProjectId, notification.SenderUserId);
                // Ukloni iz PendingCollaboratorIds
                await _projectService.RemovePendingCollaboratorAsync(notification.ProjectId, notification.SenderUserId);

            }

            // Obeleži notifikaciju kao pročitanu ili je obriši
            await MarkAsReadAsync(notificationId);
            await CreateAsync(new Notification
            {
                UserId = notification.SenderUserId, // osoba koja je poslala zahtev
                SenderUserId = notification.UserId, // vlasnik projekta (ko je prihvatio)
                ProjectId = notification.ProjectId,
                Type = NotificationType.CollaborationAccepted,
                Message = "Vaš zahtev za saradnju je prihvaćen.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            // Obavesti korisnika koji je slao zahtev da je prihvaćen
            await _hubContext.Clients.User(notification.SenderUserId)
                .SendAsync("CollaborationRequestAccepted", notification.ProjectId);

        }

        public async Task RejectCollaborationRequestAsync(string notificationId)
        {
            var notification = await _notifications.Find(n => n.Id == notificationId).FirstOrDefaultAsync();
            if (notification == null) throw new Exception("Notification not found");

            if (notification.ProjectId != null && notification.SenderUserId != null)
            {
                // Samo ukloni iz PendingCollaboratorIds
                await _projectService.RemovePendingCollaboratorAsync(notification.ProjectId, notification.SenderUserId);
            }

            await MarkAsReadAsync(notificationId);
            await CreateAsync(new Notification
            {
                UserId = notification.SenderUserId, // osoba koja je poslala zahtev
                SenderUserId = notification.UserId, // vlasnik projekta
                ProjectId = notification.ProjectId,
                Type = NotificationType.CollaborationRejected,
                Message = "Vaš zahtev za saradnju je odbijen.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            // Obavesti korisnika koji je slao zahtev da je odbijen
            await _hubContext.Clients.User(notification.SenderUserId)
                .SendAsync("CollaborationRequestRejected", notification.ProjectId);

        }
        public async Task<List<Notification>> GetForUserAsync(string userId)
        {
            return await _notifications.Find(n => n.UserId == userId).ToListAsync();
        }

    }
}
