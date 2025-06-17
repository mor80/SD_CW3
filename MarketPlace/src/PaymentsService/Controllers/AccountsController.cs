using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentsDbContext _db;
        
        public AccountsController(PaymentsDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (await _db.Accounts.AnyAsync(a => a.UserId == request.UserId))
                return Conflict("Account already exists");
                
            var account = new Account { 
                Id = Guid.NewGuid(), 
                UserId = request.UserId, 
                Balance = 0 
            };
            
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.UserId == request.UserId);
                
            if (account == null) 
                return NotFound();
                
            account.Balance += request.Amount;
            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromQuery] string userId)
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);
                
            if (account == null) 
                return NotFound();
                
            return Ok(new { account.UserId, account.Balance });
        }
    }
} 