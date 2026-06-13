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
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            return Ok(await _expensesService.GetExpensesAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Expenses>> GetExpense(int id)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var expense = await _expensesService.GetExpenseAsync(userId, id);
            return expense == null ? NotFound() : Ok(expense);
        }

        [HttpPost]
        public async Task<ActionResult<Expenses>> PostExpense(ExpenseRequest request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var expense = await _expensesService.CreateExpenseAsync(userId, request);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutExpense(int id, ExpenseRequest request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            return await _expensesService.UpdateExpenseAsync(userId, id, request)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            return await _expensesService.DeleteExpenseAsync(userId, id)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteExpenses()
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            await _expensesService.ClearExpensesAsync(userId);
            return NoContent();
        }

        /// <summary>
        /// POST /api/expenses/optimize  { "budget": 5000.00 }
        /// Returns the knapsack optimization result for the current user's unpaid expenses.
        /// </summary>
        [HttpPost("optimize")]
        public async Task<ActionResult<ExpenseOptimizationResult>> Optimize([FromBody] OptimizeRequest request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var expenses = await _expensesService.GetExpensesAsync(userId);
            var result = _knapsackService.OptimizeExpenses(expenses, request.Budget);
            return Ok(result);
        }
    }
}
