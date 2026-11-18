using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Chat
{
    /// <summary>對話列表與詳情（唯讀）。對話由會員在前台建立，後台不能新增 / 修改 / 刪除。</summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    [RequireHttps]
    public class ChatSessionsController : Controller
    {
        private readonly IAdminChatService _chat;

        public ChatSessionsController(IAdminChatService chat)
        {
            _chat = chat;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _chat.GetSessionsAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var session = await _chat.GetSessionAsync(id.Value);
            if (session == null) return NotFound();
            return View(session);
        }
    }
}
