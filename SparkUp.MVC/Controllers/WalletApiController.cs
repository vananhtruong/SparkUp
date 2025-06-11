using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletApiController : Controller
    {
        private readonly AppDbContext _context;
        public WalletApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/walletApi/balance/123
        [HttpGet("balance/{accountId}")]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            if (accountId <= 0)
                return BadRequest("AccountId is required.");

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == accountId);

            decimal balance = wallet?.Balance ?? 0m;

            // Luôn trả về balance, không báo NotFound
            return Ok(new { balance });
        }

    }
}
