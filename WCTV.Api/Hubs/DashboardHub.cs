using Microsoft.AspNetCore.SignalR;

namespace WCTV.Api.Hubs;

public class DashboardHub : Hub
{
    // Clients connect here and receive push events
    // Server calls: Clients.All.SendAsync("ReceiveStatusUpdate", ...)
}
