using Microsoft.AspNetCore.SignalR;

namespace WebEdgeClassification.Hubs
{
    /// <summary>
    /// Class uses for SignalR
    /// </summary>
    public class DesignAutomationHub : Hub
    {
        public string GetConnectionId() { return Context.ConnectionId; }
    }
}