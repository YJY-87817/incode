using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntrustMissionApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IEntrustMissionService _entrustMissionService;

        public EntrustMissionApiController(incodedatabaseContext context , IEntrustMissionService entrustMissionService)
        {
            _context = context;
            _entrustMissionService = entrustMissionService;
        }

        // GET: api/EntrustMissionApi
        [HttpGet]
        //[Authorize]
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

        // POST: api/EntrustMissionApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostEntrustMission(EntrustMissionDTO EmDTO)
        {
            EntrustMission em = new EntrustMission
            {
                UserId = EmDTO.UserId,
                ContentId = EmDTO.ContentId,
                Title = EmDTO.Title,
                Detail = EmDTO.Detail,
                RewardPoints = EmDTO.RewardPoints,
                MaxParticipants = EmDTO.MaxParticipants,
                Status = EmDTO.Status ?? "open", // 給予預設狀態
                IsActive = true,                     // 新增時預設啟用
                CreatedAt = DateTime.Now             // 強制後端時間
            };
            _context.EntrustMission.Add(em);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 204 //NoContent
            };
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
