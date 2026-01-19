using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.Controllers 
{
    [RequireHttps] // 屬性：強制要求此控制器的所有動作必須使用HTTPS協議，提升安全性
    public class ChatSessionsController : Controller // 定義ChatSessions控制器，繼承自ASP.NET Core的基礎Controller類別
    {
        private readonly incodedatabaseContext _context; // 宣告唯讀的資料庫上下文欄位，用於操作資料庫

        public ChatSessionsController(incodedatabaseContext context) // 控制器的建構函式，透過依賴注入接收資料庫上下文實例
        {
            _context = context; // 將注入的資料庫上下文實例賦值給類別欄位，供後續所有動作方法使用
        }

        // ==========================================
        //  MVC 後台管理畫面 (回傳 View)
        // ==========================================

        // GET: ChatSessions
        public async Task<IActionResult> Index() // 處理GET請求的Index動作方法，非同步執行，回傳IActionResult類型結果
        {
            var sessions = _context.ChatSession // 從資料庫上下文取得ChatSession資料表的查詢入口
                                   .Include(c => c.User) // 加載關聯的User導覽屬性，避免延遲加載帶來的效能問題
                                   .OrderByDescending(s => s.CreatedAt); // 依CreatedAt欄位降序排序，最新的會話排在前面
            return View(await sessions.ToListAsync()); // 將查詢結果非同步轉換為列表，並傳遞給Index視圖作為模型
        }

        // GET: ChatSessions/Details/5
        public async Task<IActionResult> Details(int? id) // 處理GET請求的Details動作方法，接收可為空的int類型id參數（會話ID）
        {
            if (id == null) return NotFound(); // 判斷id參數是否為null，若是則回傳404 Not Found回應

            var chatSession = await _context.ChatSession // 從資料庫上下文取得ChatSession查詢入口，非同步查詢
                .Include(c => c.User) // 加載關聯的User導覽屬性
                .FirstOrDefaultAsync(m => m.ChatSessionId == id); // 依ChatSessionId等於id的條件，取得第一筆符合的記錄，無則回傳null

            if (chatSession == null) return NotFound(); // 判斷查詢結果是否為null，若是則回傳404 Not Found回應

            return View(chatSession); // 將查詢到的ChatSession物件傳遞給Details視圖
        }

        // GET: ChatSessions/Create
        public IActionResult Create() // 處理GET請求的Create動作方法，用於顯示建立會話的表單頁面
        {
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account"); // 建立使用者下拉選單的資料來源：從User表取得資料，Value為UserId，Text為Account，並存入ViewData供視圖使用
            return View(); // 回傳Create視圖，顯示建立表單
        }

        // POST: ChatSessions/Create
        [HttpPost] // 屬性：指定此方法僅處理HTTP POST請求
        [ValidateAntiForgeryToken] // 屬性：驗證防偽令牌，防止CSRF跨站請求偽造攻擊
        public async Task<IActionResult> Create([Bind("ChatSessionId,UserId,Title,CreatedAt,UpdatedAt")] ChatSession chatSession) // 處理POST請求的Create方法，透過Bind屬性限定只綁定指定欄位到chatSession物件，避免過度綁定風險
        {
            if (ModelState.IsValid) // 驗證綁定後的ChatSession模型是否符合資料驗證規則（如必填、格式等）
            {
                _context.Add(chatSession); // 將新的ChatSession物件加入資料庫上下文的追蹤狀態（新增狀態）
                await _context.SaveChangesAsync(); // 非同步將變更儲存到資料庫，完成新增操作
                return RedirectToAction(nameof(Index)); // 新增成功後，重新導向到Index動作方法，顯示會話列表
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId); // 驗證失敗時，重新建立使用者下拉選單，並保留使用者已選擇的UserId值
            return View(chatSession); // 回傳Create視圖，並帶入驗證失敗的chatSession物件，保留使用者已輸入的表單資料
        }

        // GET: ChatSessions/Edit/5
        public async Task<IActionResult> Edit(int? id) // 處理GET請求的Edit動作方法，接收可為空的int類型id參數（要編輯的會話ID）
        {
            if (id == null) return NotFound(); // 判斷id是否為null，若是則回傳404

            var chatSession = await _context.ChatSession.FindAsync(id); // 非同步根據id查詢ChatSession記錄，FindAsync比FirstOrDefaultAsync效能更高（優先查快取）
            if (chatSession == null) return NotFound(); // 判斷查詢結果是否為null，若是則回傳404

            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId); // 建立使用者下拉選單，並預選當前會話的UserId
            return View(chatSession); // 將查詢到的chatSession傳遞給Edit視圖，顯示編輯表單
        }

        // POST: ChatSessions/Edit/5
        [HttpPost] // 指定僅處理POST請求
        [ValidateAntiForgeryToken] // 驗證防偽令牌
        public async Task<IActionResult> Edit(int id, [Bind("ChatSessionId,UserId,Title,CreatedAt,UpdatedAt")] ChatSession chatSession) // 處理POST請求的Edit方法，接收URL中的id和表單綁定的chatSession物件
        {
            if (id != chatSession.ChatSessionId) return NotFound(); // 驗證URL中的id與表單綁定的ChatSessionId是否一致，防止修改錯誤的記錄

            if (ModelState.IsValid) // 驗證編輯後的模型是否有效
            {
                try // 捕捉資料庫並發行異常
                {
                    _context.Update(chatSession); // 將chatSession物件標記為已修改狀態，加入資料庫上下文追蹤
                    await _context.SaveChangesAsync(); // 非同步儲存變更到資料庫，完成更新操作
                }
                catch (DbUpdateConcurrencyException) // 捕捉並發更新異常（多使用者同時修改同一筆記錄）
                {
                    if (!ChatSessionExists(chatSession.ChatSessionId)) return NotFound(); // 檢查記錄是否還存在，不存在則回傳404
                    else throw; // 記錄存在則重新拋出異常，交由全域例外處理
                }
                return RedirectToAction(nameof(Index)); // 更新成功後，重新導向到Index列表頁
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId); // 驗證失敗時，重新建立下拉選單並保留已選值
            return View(chatSession); // 回傳Edit視圖，保留使用者已輸入的資料
        }

        // GET: ChatSessions/Delete/5
        public async Task<IActionResult> Delete(int? id) // 處理GET請求的Delete方法，接收可為空的id參數（要刪除的會話ID）
        {
            if (id == null) return NotFound(); // id為null則回傳404

            var chatSession = await _context.ChatSession // 非同步查詢指定id的ChatSession記錄
                .Include(c => c.User) // 加載關聯的User資訊，供刪除確認頁顯示
                .FirstOrDefaultAsync(m => m.ChatSessionId == id); // 取得第一筆符合條件的記錄

            if (chatSession == null) return NotFound(); // 記錄不存在則回傳404

            return View(chatSession); // 將chatSession傳遞給Delete視圖，顯示刪除確認頁
        }

        // POST: ChatSessions/Delete/5
        [HttpPost, ActionName("Delete")] // 指定處理POST請求，並將動作名稱指定為Delete（因方法名為DeleteConfirmed，避免與GET的Delete方法衝突）
        [ValidateAntiForgeryToken] // 驗證防偽令牌
        public async Task<IActionResult> DeleteConfirmed(int id) // 處理刪除確認的POST請求，接收要刪除的會話ID
        {
            var chatSession = await _context.ChatSession.FindAsync(id); // 非同步查詢指定id的ChatSession記錄
            if (chatSession != null) // 確認記錄存在
            {
                _context.ChatSession.Remove(chatSession); // 將記錄標記為刪除狀態，加入資料庫上下文追蹤
            }
            await _context.SaveChangesAsync(); // 非同步儲存變更到資料庫，完成刪除操作
            return RedirectToAction(nameof(Index)); // 刪除成功後，重新導向到Index列表頁
        }

        private bool ChatSessionExists(int id) // 私用輔助方法，檢查指定id的ChatSession記錄是否存在
        {
            return _context.ChatSession.Any(e => e.ChatSessionId == id); // 查詢ChatSession表是否存在ChatSessionId等於指定id的記錄，回傳布林值
        }
    }
}