using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Chat
{
    /// <summary>
    /// 客服頁：查看對話與即時回覆（回覆走 SignalR，見 ChatHub）。
    /// 訊息只能查看，不能在後台新增 / 修改 / 刪除（會員與 AI 的對話紀錄不可竄改）。
    /// </summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ChatMessagesController : Controller
    {
        private readonly IAdminChatService _chat;

        public ChatMessagesController(IAdminChatService chat)
        {
            _chat = chat;
        }

        public async Task<IActionResult> Index(int? id)
        {
            return View(await _chat.GetIndexAsync(id));
        }
    }
}
