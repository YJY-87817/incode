using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace incode2.Hubs 
{
    public class ChatHub : Hub //signalR連線
    {
        // 當使用者連線時觸發
        public override async Task OnConnectedAsync()
        {
            // 可以在這裡記錄 ConnectionId，或者只是單純建立連線，例如：將這個連線加入某個 Group (便於群發)
            // await Groups.AddToGroupAsync(Context.ConnectionId, "OnlineUsers");
            await base.OnConnectedAsync();
        }

        // 當使用者斷線時觸發
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}