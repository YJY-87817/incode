using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace incode.MVC.Hubs
{
    /// <summary>
    /// 客服聊天室。訊息一律透過 ChatMessagesApiController 存檔後由 IHubContext 推播，
    /// Hub 本身只負責加入 / 離開群組。
    /// 群組：每個對話以 ChatSessionId 為群組名稱；"SupportAgents" 為客服人員群組 (收到真人客服請求)。
    /// </summary>
    [Authorize] // 需登入 (前台會員 Cookie 或後台管理員 Cookie)
    public class ChatHub : Hub
    {
        public const string SupportGroup = "SupportAgents";

        private readonly IChatService _chat;

        public ChatHub(IChatService chat)
        {
            _chat = chat;
        }

        private bool IsAdmin => Context.User?.IsInRole("Admin") == true;

        // 客服人員登入後台時呼叫 (僅限管理員)
        public async Task JoinSupportGroup()
        {
            if (!IsAdmin)
                throw new HubException("沒有權限");

            await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroup);
        }

        public async Task LeaveSupportGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SupportGroup);
        }

        // 使用者進入對話，或客服點擊某個對話時呼叫：只能加入自己的對話 (管理員可加入任何對話)
        public async Task JoinSessionGroup(string sessionId)
        {
            if (!int.TryParse(sessionId, out var id))
                throw new HubException("無效的對話");

            if (!IsAdmin)
            {
                var userId = Context.User!.GetId();
                var isOwner = await _chat.IsOwnerAsync(id, userId);
                if (!isOwner)
                    throw new HubException("找不到該對話");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        }

        public async Task LeaveSessionGroup(string sessionId)
        {
            if (int.TryParse(sessionId, out var id))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString());
        }

        // 原本的 SendMessage(sessionId, user, message) 可指定任意發送者名稱並推播到任何群組 (冒充客服)，
        // 前後台都沒有使用，已移除；訊息請走 ChatMessagesApiController
    }
}
