using TaskManager.Models;
using Microsoft.AspNetCore.SignalR;
namespace TaskManager.Hubs
{
    public class NotificationsHub: Hub
    {
        // Kad se korisnik poveže, poveži ga sa njegovim userId
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // mora da bude postavljen u Startup/Program
            Console.WriteLine($"User connected to NotificationsHub: {userId}");
            Console.WriteLine($"[DEBUG] Connected: {userId}");
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }


        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
