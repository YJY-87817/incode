using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.ApiControllers.Content
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentNotificationApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public ContentNotificationApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/ContentNotificationApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContentNotification>>> GetContentNotification()
        {
            return await _context.ContentNotification.ToListAsync();
        }

        // GET: api/ContentNotificationApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContentNotification>> GetContentNotification(int id)
        {
            var contentNotification = await _context.ContentNotification.FindAsync(id);

            if (contentNotification == null)
            {
                return NotFound();
            }

            return contentNotification;
        }

        // PUT: api/ContentNotificationApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContentNotification(int id, ContentNotification contentNotification)
        {
            if (id != contentNotification.ContentNotificationId)
            {
                return BadRequest();
            }

            _context.Entry(contentNotification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContentNotificationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ContentNotificationApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ContentNotification>> PostContentNotification(ContentNotification contentNotification)
        {
            _context.ContentNotification.Add(contentNotification);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetContentNotification", new { id = contentNotification.ContentNotificationId }, contentNotification);
        }

        // DELETE: api/ContentNotificationApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContentNotification(int id)
        {
            var contentNotification = await _context.ContentNotification.FindAsync(id);
            if (contentNotification == null)
            {
                return NotFound();
            }

            _context.ContentNotification.Remove(contentNotification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContentNotificationExists(int id)
        {
            return _context.ContentNotification.Any(e => e.ContentNotificationId == id);
        }
    }
}
