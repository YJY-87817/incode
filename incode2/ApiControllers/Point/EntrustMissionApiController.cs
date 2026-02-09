using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Point;
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

namespace incode2.ApiControllers.Point
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

    // POST: api/EntrustMissionApi/{id}/claim  - 接取委託任務（重點新增）
    [HttpPost("{id}/entrustMissionClaim")]
    [Authorize]
    public async Task<ActionResult<ResultDTO>> ClaimEntrustMission(int id)
    {
      int userId = User.GetId();

      // 額外防呆：確保 userId 是正數
      if (userId <= 0)
      {
        return Unauthorized(new ResultDTO
        {
          Ok = false,
          Code = 401,
          Message = "使用者 ID 無效"
        });
      }

      var result = await _entrustMissionService.ClaimEntrustMissionAsync(id, userId);

      if (!result.Ok)
      {
        return result.Code switch
        {
          400 => BadRequest(result),
          401 => Unauthorized(result),
          404 => NotFound(result),
          409 => Conflict(result),
          410 => StatusCode(410, result),   // Gone - 任務已下架或過期
          _ => StatusCode(500, new ResultDTO
          {
            Ok = false,
            Code = 500,
            Message = result.Message ?? "伺服器內部錯誤，請稍後再試"
          })
        };
      }

      return Ok(result);
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
        MaxParticipants = em.MaxParticipants,
        Status = em.Status,
        IsActive = em.IsActive,
        CreatedAt = em.CreatedAt,
      }).FirstOrDefaultAsync(em => em.EntrustMissionId == id);

      return EmDTO;
    }

    // PUT: api/EntrustMissionApi/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<ResultDTO> PutEntrustMission(int id, EntrustMissionDTO EmDTO)
    {
      if (id != EmDTO.EntrustMissionId)
      {
        return new ResultDTO { Ok = false, Code = 400 };
      }

      EntrustMission Em = await _context.EntrustMission.FindAsync(id);
      if (Em != null)
      {
        Em.UserId = EmDTO.UserId;
        Em.ContentId = EmDTO.ContentId;
        Em.Title = EmDTO.Title;
        Em.Detail = EmDTO.Detail;
        Em.RewardPoints = EmDTO.RewardPoints;
        Em.MaxParticipants = EmDTO.MaxParticipants;
        Em.Status = EmDTO.Status;
        Em.IsActive = EmDTO.IsActive;
        // 只有當前端真的有傳新時間才更新，否則保留資料庫原始時間
        Em.CreatedAt = EmDTO.CreatedAt ?? Em.CreatedAt;

        _context.Entry(Em).State = EntityState.Modified;

        try
        {
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          return new ResultDTO { Ok = false, Code = 500 }; //Server端的錯誤
        }

        return new ResultDTO { Ok = true, Code = 204 };
      }
      else
      {
        return new ResultDTO { Ok = false, Code = 404 };
      }

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
        return result.Code switch
        {
          400 => BadRequest(result),
          404 => NotFound(result),
          500 => StatusCode(500, result),
          _ => StatusCode(500, new ResultDTO
          {
            Ok = false,
            Code = 500,
            Message = result.Message ?? "伺服器內部錯誤，請稍後再試"
          })
        };
      }

      return CreatedAtAction(nameof(GetEntrustMission), new { id = result.Data }, result);
    }

    // DELETE: api/EntrustMissionApi/5
    [HttpDelete("{id}")]
    public async Task<ResultDTO> DeleteEntrustMission(int id)
    {
      var entrustMission = await _context.EntrustMission.FindAsync(id);
      if (entrustMission == null)
      {
        return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
      }
      try
      {
        _context.EntrustMission.Remove(entrustMission);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateException ex)
      {
        return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示50
      }

      return new ResultDTO { Ok = true, Code = 204 };
    }

    //private bool EntrustMissionExists(int id)
    //{
    //    return _context.EntrustMission.Any(e => e.EntrustMissionId == id);
    //}
  }
}
