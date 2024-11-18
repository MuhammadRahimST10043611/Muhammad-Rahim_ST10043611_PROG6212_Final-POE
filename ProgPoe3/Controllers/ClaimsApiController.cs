using Microsoft.AspNetCore.Mvc;
using ProgPoe3.Data;
using ProgPoe3.Models;

namespace ProgPoe3.Controllers
{
    [ApiController]
    [Route("api/claims")]
    public class ClaimsApiController : ControllerBase
    {
        private readonly ClaimDbContext _context;

        public ClaimsApiController(ClaimDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAllClaims()
        {
            return Ok(_context.Claims.ToList());
        }

        [HttpPost]
        public IActionResult CreateClaim([FromBody] Claim claim)
        {
            if (claim == null) return BadRequest("Claim is null.");
            _context.Claims.Add(claim);
            _context.SaveChanges();
            return Ok(claim);
        }
    }
}
