using incode2.DTO;
using incode2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntrustMissionApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public EntrustMissionApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/EntrustMissionApi
        [HttpGet]
        public async Task<IEnumerable<EntrustMissionDTO>> GetEntrustMission()
        {
            return _context.EntrustMission.Select(em => new EntrustMissionDTO
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
            });
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
                Em.EntrustMissionId = EmDTO.EntrustMissionId;
                Em.UserId = EmDTO.UserId;
                Em.ContentId = EmDTO.ContentId;
                Em.Title = EmDTO.Title;
                Em.Detail = EmDTO.Detail;
                Em.RewardPoints = EmDTO.RewardPoints;
                Em.MaxParticipants = EmDTO.MaxParticipants;
                Em.Status = EmDTO.Status;
                Em.IsActive = EmDTO.IsActive;
                Em.CreatedAt = EmDTO.CreatedAt;

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
                EntrustMissionId = EmDTO.EntrustMissionId,
                UserId = EmDTO.UserId,
                ContentId = EmDTO.ContentId,
                Title = EmDTO.Title,
                Detail = EmDTO.Detail,
                RewardPoints = EmDTO.RewardPoints,
                MaxParticipants = EmDTO.MaxParticipants,
                Status = EmDTO.Status,
                IsActive = EmDTO.IsActive,
                CreatedAt = DateTime.Now,
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
