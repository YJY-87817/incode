using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace incode.MVC.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntrustMissionApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IEntrustMissionService _entrustMissionService;

        public EntrustMissionApiController(incodedatabaseContext context, IEntrustMissionService entrustMissionService)
        {
            _context = context;
            _entrustMissionService = entrustMissionService;
        }

        // GET: api/EntrustMissionApi
        [HttpGet]
        public async Task<IEnumerable<EntrustMissionDTO>> GetEntrustMission()
        {
            var entrustmission = await _entrustMissionService.GetAllEntrustMissionAsync();
            return entrustmission;
        }

        // GET: api/EntrustMissionApi/5
        [HttpGet("{id}")]
        public async Task<EntrustMissionDTO> GetEntrustMission(int id)
        {
            EntrustMissionDTO? EmDTO = await _context.EntrustMission.Select(em => new EntrustMissionDTO
            {
                EntrustMissionId = em.EntrustMissionId,
                UserId = em.UserId,
                ContentId = em.ContentId,
                Title = em.Title,
                Detail = em.Detail,
                RewardPoints = em.RewardPoints,
                Category = em.Category,
                Deadline = em.Deadline,
                Status = em.Status,
                IsUrgent = em.IsUrgent ?? false,
                IsActive = em.IsActive,
                CreatedAt = em.CreatedAt,
                UpdatedAt = em.UpdatedAt,
            }).FirstOrDefaultAsync(em => em.EntrustMissionId == id);

            return EmDTO;
        }

        // 在 EntrustMissionApiController.cs 裡新增這個 endpoint
        [HttpGet("my-entrusts")]
        [Authorize]  // 需要登入
        public async Task<ActionResult<IEnumerable<EntrustMissionDTO>>> GetMyEntrustMissions()
        {
            int userId = User.GetId();  // 從 Claims 取得當前使用者 ID

            if (userId <= 0)
            {
                return Unauthorized(new { message = "無法取得使用者 ID" });
            }

            var missions = await _entrustMissionService.GetMyEntrustMissionsAsync(userId);

            return Ok(missions);
        }

        //tim修改內容
        // POST: api/EntrustMissionApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostEntrustMission([FromBody] EntrustMissionDTO emDTO)
        {
            // 基本模型驗證由 [ApiController] 自動執行
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultDTO
                {
                    Ok = false,
                    Code = 400,
                    Message = "請求資料驗證失敗",
                    Data = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList()
                });
            }

            // 取得當前登入使用者 ID
            int userId = User.GetId();

            // 防呆：確保 userId 是正數
            if (userId <= 0)
            {
                return Unauthorized(new ResultDTO
                {
                    Ok = false,
                    Code = 401,
                    Message = "使用者 ID 無效，請重新登入"
                });
            }

            // 設定 userId，覆蓋前端傳來的值（安全考量）
            emDTO.UserId = userId;

            // 調用 Service 層進行業務邏輯處理
            var result = await _entrustMissionService.CreateEntrustMissionAsync(emDTO);

            if (!result.Ok)
            {
                // 失敗時以 Code 作為 HTTP 狀態碼（400 / 404 / 409 / 500）
                return StatusCode(result.Code is >= 400 and <= 599 ? result.Code : 500, result);
            }

            return CreatedAtAction(nameof(GetEntrustMission), new { id = result.Data }, result);
        }


        // 取消委託（委託人）：狀態改為 cancelled 並退還預扣點數
        // POST api/EntrustMissionApi/{id}/cancel
        // 只有自己發起、徵求中、且還沒有其他會員回覆的委託可以取消；否則 404（不是自己的）/ 409
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<ResultDTO<object>>> CancelEntrustMission(int id)
        {
            var result = await _entrustMissionService.CancelEntrustMissionAsync(id, User.GetId());
            return this.ToActionResult<object>(result);
        }

        // 原本的 DELETE api/EntrustMissionApi/{id} 沒有登入與擁有者檢查，且直接刪除不會退還預扣點數，已移除（取消請用上面的 cancel）
    }
}
