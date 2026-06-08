using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class ExpensesController : AuthenticatedApiController
    {
        private readonly ExpensesService _expensesService;

        public ExpensesController(ExpensesService expensesService)
        {
            _expensesService = expensesService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expenses>>> GetExpenses()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _expensesService.GetExpensesAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Expenses>> GetExpense(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var expense = await _expensesService.GetExpenseAsync(userId, id);
            return expense == null ? NotFound() : Ok(expense);
        }

        [HttpPost]
        public async Task<ActionResult<Expenses>> PostExpense(ExpenseRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var expense = await _expensesService.CreateExpenseAsync(userId, request);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutExpense(int id, ExpenseRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _expensesService.UpdateExpenseAsync(userId, id, request)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _expensesService.DeleteExpenseAsync(userId, id)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteExpenses()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            await _expensesService.ClearExpensesAsync(userId);
            return NoContent();
        }
    }
}
