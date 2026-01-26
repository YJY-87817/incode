using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace incode2.Hubs
{
    public class ChatHub : Hub // SignalR Hub 類別
    {
        // ==========================================
        // 連線事件
        // ==========================================
        // 當使用者連線時觸發
        public override async Task OnConnectedAsync()
        {
            // 這裡可以記錄連線 ID (Context.ConnectionId)，目前先維持預設
            await base.OnConnectedAsync();
        }

        // 當使用者斷線時觸發
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // ==========================================
        // 1. 客服人員群組管理 (關鍵功能)
        // ==========================================
        // 當使用者(客服)進入後台介面時，前端呼叫此方法加入 "SupportAgents" 群組
        // 這樣 Controller 才能透過 _hubContext.Clients.Group("SupportAgents") 推播訊息給他們
        public async Task JoinSupportGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "SupportAgents");
        }


        // 當使用者離開或切換頁面時，退出群組
        public async Task LeaveSupportGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportAgents");
        }

        // ==========================================
        // 2. 一般訊息發送 (測試與備用)
        // ==========================================
        public async Task SendMessage(string user, string message)
        {
            // 廣播給所有人 (通常用於公開聊天室，這裡作為備用)
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // ==========================================
        // 3. 加入特定聊天室群組 (進階功能)
        // ==========================================
        // 如果未來要做「多裝置同步」或「多人同時觀看同一聊天室」，可使用此方法
        public async Task JoinSessionGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}