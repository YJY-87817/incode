using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using incode2.DTO;
using incode2.Dtos;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntrustMissionProgressApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public EntrustMissionProgressApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/EntrustMissionProgressApi
        [HttpGet]
        public async Task<IEnumerable<EntrustMissionProgressDTO>> GetEntrustMissionProgress()
        {
            return _context.EntrustMissionProgress.Select(emg=>new EntrustMissionProgressDTO
            {
                EntrustMissionProgressId = emg.EntrustMissionProgressId,
                UserId = emg.UserId,
                EntrustMissionId = emg.EntrustMissionId,
                Status = emg.Status,
                AcceptedAt = emg.AcceptedAt,
                CompletedAt = emg.CompletedAt,
                RewardedAt = emg.RewardedAt
            });
        }

        // GET: api/EntrustMissionProgressApi/5
        [HttpGet("{id}")]
        public async Task<EntrustMissionProgressDTO>GetEntrustMissionProgress(int id)
        {
            EntrustMissionProgressDTO? EmgDTO = await _context.EntrustMissionProgress.Select(emg=>new EntrustMissionProgressDTO
            {
                EntrustMissionProgressId = emg.EntrustMissionProgressId,
                UserId = emg.UserId,
                EntrustMissionId = emg.EntrustMissionId,
                Status = emg.Status,
                AcceptedAt = emg.AcceptedAt,
                CompletedAt = emg.CompletedAt,
                RewardedAt = emg.RewardedAt
            }).FirstOrDefaultAsync(emg=>emg.EntrustMissionProgressId == id);

            return EmgDTO;
        }

        // PUT: api/EntrustMissionProgressApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ResultDTO> PutEntrustMissionProgress(int id, EntrustMissionProgressDTO EmgDTO)
        {
            if (id != EmgDTO.EntrustMissionProgressId)
            {
                return new ResultDTO { Ok = false, Code = 400 };
            }

            EntrustMissionProgress emg = await _context.EntrustMissionProgress.FindAsync(id);
            if(emg != null)
            {
                emg.UserId = EmgDTO.UserId;
                emg.EntrustMissionId = EmgDTO.EntrustMissionId;
                emg.Status = EmgDTO.Status;
                // 2. 保護 AcceptedAt：如果前端沒傳，就保留資料庫原有的值
                emg.AcceptedAt = EmgDTO.AcceptedAt ?? emg.AcceptedAt;

                // 3. 智慧型時間處理：針對狀態自動補時
                // 如果改為 "completed" 且資料庫還沒存過完工時間
                if (EmgDTO.Status == "completed" && emg.CompletedAt == null)
                {
                    emg.CompletedAt = DateTime.Now;
                }
                else
                {
                    // 否則，依照前端傳入的值（可能是 null 或特定時間）更新
                    emg.CompletedAt = EmgDTO.CompletedAt ?? emg.CompletedAt;
                }

                // 4. 領獎時間邏輯同上
                if (EmgDTO.Status == "rewarded" && emg.RewardedAt == null)
                {
                    emg.RewardedAt = DateTime.Now;
                }
                else
                {
                    emg.RewardedAt = EmgDTO.RewardedAt ?? emg.RewardedAt;
                }

                _context.Entry(emg).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new ResultDTO { Ok = false, Code =  500 }; //   Server端的錯誤
                }

                return new ResultDTO { Ok = true, Code = 204 };
            }
            else
            {
                return new ResultDTO { Ok = false, Code = 404 };
            }
        }
        // POST: api/EntrustMissionProgressApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostEntrustMissionProgress(EntrustMissionProgressDTO EmgDTO)
        {
            EntrustMissionProgress emg = new EntrustMissionProgress
            {
                UserId = EmgDTO.UserId,
                EntrustMissionId = EmgDTO.EntrustMissionId,
                Status = "ongoing",     // 新增時強制設定為進行中
                AcceptedAt = DateTime.Now // 強制設定為現在時間
            };
            _context.EntrustMissionProgress.Add(emg);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 204 //NoContent
            };
        }

        // DELETE: api/EntrustMissionProgressApi/5
        [HttpDelete("{id}")]
        public async Task<ResultDTO> DeleteEntrustMissionProgress(int id)
        {
            var entrustMissionProgress = await _context.EntrustMissionProgress.FindAsync(id);
            if (entrustMissionProgress == null)
            {
                return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
            }
            try
            {
                _context.EntrustMissionProgress.Remove(entrustMissionProgress);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示500
            }
            return new ResultDTO { Ok = true, Code = 204 };
        }

        //private bool EntrustMissionProgressExists(int id)
        //{
        //    return _context.EntrustMissionProgress.Any(e => e.EntrustMissionProgressId == id);
        //}
    }
}
