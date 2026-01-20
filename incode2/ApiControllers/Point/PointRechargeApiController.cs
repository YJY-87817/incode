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
    public class PointRechargeApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public PointRechargeApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/PointRechargeApi
        [HttpGet]
        public async Task<IEnumerable<PointRechargeDTO>> GetPointRecharge()
        {
            return _context.PointRecharge.Select(pr=>new PointRechargeDTO
            {
                PointRechargeId=pr.PointRechargeId,
                UserId=pr.UserId,
                CashAmount=pr.CashAmount,
                PointsGranted=pr.PointsGranted,
                PaymentMethod=pr.PaymentMethod,
                Status =pr.Status,
                CreatedAt=pr.CreatedAt
            });
        }

        // GET: api/PointRechargeApi/5
        [HttpGet("{id}")]
        public async Task<PointRechargeDTO> GetPointRecharge(int id) //錯誤:不一致的存取範圍=>DTO:intenal=>public
        {
            PointRechargeDTO? PrDTO = await _context.PointRecharge.Select(pr=> new PointRechargeDTO
            {
                PointRechargeId=pr.PointRechargeId,
                UserId=pr.UserId,
                CashAmount=pr.CashAmount,
                PointsGranted=pr.PointsGranted,
                PaymentMethod=pr.PaymentMethod,
                Status =pr.Status,
                CreatedAt=pr.CreatedAt
            }).FirstOrDefaultAsync(pr => pr.PointRechargeId == id);

            return PrDTO;
        }

        // PUT: api/PointRechargeApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ResultDTO> PutPointRecharge(int id, PointRechargeDTO PrDTO)
        {
            if (id != PrDTO.PointRechargeId)
            {
                return new ResultDTO { Ok = false, Code = 400 };
            }

            PointRecharge Pr = await _context.PointRecharge.FindAsync(id);
            if (Pr != null)
            {
                Pr.UserId = PrDTO.UserId;
                Pr.CashAmount = PrDTO.CashAmount;
                Pr.PaymentMethod = PrDTO.PaymentMethod;
                Pr.Status = PrDTO.Status;

                _context.Entry(PrDTO).State = EntityState.Modified;

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

        // POST: api/PointRechargeApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostPointRecharge(PointRechargeDTO PrDTO)
        {
            if(PrDTO.CashAmount <= 0 || PrDTO.PointsGranted <= 0)
            {
                return new ResultDTO
                {
                    Ok = false,
                    Code = 400, //BadRequest
                    Message = "金額或點數必須大於 0"
                };
            }

            PointRecharge Pr = new PointRecharge
            {
                UserId = PrDTO.UserId,
                CashAmount = PrDTO.CashAmount,
                PointsGranted = PrDTO.PointsGranted,
                PaymentMethod = PrDTO.PaymentMethod,
                Status = PrDTO.Status,
                CreatedAt = DateTime.Now //後端程式碼產生
            };
            _context.PointRecharge.Add(Pr);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 204 //NoContent
            };
        }

            // DELETE: api/PointRechargeApi/5
            [HttpDelete("{id}")]
        public async Task<ResultDTO> DeletePointRecharge(int id)
        {
            var pointRecharge = await _context.PointRecharge.FindAsync(id);
            if (pointRecharge == null)
            {
                return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
            }
            try
            {
                _context.PointRecharge.Remove(pointRecharge);
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
            {
                return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示500
            }

            return new ResultDTO { Ok = true, Code = 204 };
        }

        //private bool PointRechargeExists(int id)
        //{
        //    return _context.PointRecharge.Any(e => e.PointRechargeId == id);
        //}
    }
}
