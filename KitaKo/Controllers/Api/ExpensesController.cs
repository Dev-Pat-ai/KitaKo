using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class ExpensesController : AuthenticatedApiController
    {
        private readonly ExpensesService _expensesService;
        private readonly KnapsackService _knapsackService;

        public ExpensesController(ExpensesService expensesService, KnapsackService knapsackService)
        {
            _expensesService = expensesService;
            _knapsackService = knapsackService;
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

        /// <summary>
        /// Optimizes expense payments based on available budget, priority, and due dates.
        /// Returns recommended expenses to pay within the budget.
        /// </summary>
        [HttpGet("optimize")]
        public async Task<ActionResult<ExpenseOptimizationResult>> OptimizeExpenses([FromQuery] decimal budget)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            if (budget < 0)
            {
                return BadRequest("Budget must be non-negative");
            }

            var userExpenses = await _expensesService.GetExpensesAsync(userId);
            var result = _knapsackService.OptimizeExpenses(userExpenses, budget);
            
            return Ok(result);
        }
    }
}
