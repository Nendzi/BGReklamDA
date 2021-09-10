using Microsoft.AspNetCore.SignalR;

namespace forgesample.Hubs
{
    /// <summary>
    /// Class uses for SignalR
    /// </summary>
    public class DesignAutomationHub : Hub
    {
        public string GetConnectionId() { return Context.ConnectionId; }
    }
}