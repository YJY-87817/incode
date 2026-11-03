using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace incode2.Hubs
{
    // [Authorize] // 視需求開啟
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // 連線時可以做紀錄 (選用)
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // ==========================================
        // 1. 客服專區 (配合 Controller)
        // ==========================================

        // 客服人員登入後台時呼叫此方法
        public async Task JoinSupportGroup()
        {
            // ★ 修改 1: 名稱統一改為 "SupportAgents"，跟 Controller 一樣
            await Groups.AddToGroupAsync(Context.ConnectionId, "SupportAgents");
        }

        public async Task LeaveSupportGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportAgents");
        }

        // ==========================================
        // 2. 聊天室分流 (核心功能)
        // ==========================================

        // 使用者進入頁面，或客服點擊某個對話時呼叫
        public async Task JoinSessionGroup(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            }
        }

        public async Task LeaveSessionGroup(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            }
        }

        // ==========================================
        // 3. (選用) 僅轉發不存檔的訊息
        // ==========================================

        // 如果你的所有對話都走 API (存檔+推播)，其實不需要這個方法。
        // 但保留它可以做為打字狀態提示 (Typing Indicator) 等不需存檔的功能。
        public async Task SendMessage(string sessionId, string user, string message)
        {
            // ★ 修改 2: 只發給該房間的人，而不是 Clients.All
            await Clients.Group(sessionId).SendAsync("ReceiveMessage", user, message);
        }
    }
}