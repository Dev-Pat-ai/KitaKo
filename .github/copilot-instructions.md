# KitaKo AI Coding Agent Instructions

## Project Overview
KitaKo is an ASP.NET Core 8 MVC application for personal budget and sales management. It features multi-tenant user accounts, expense tracking with priority-based optimization, sales tracking, and debt management (Utang). Database: PostgreSQL with Entity Framework Core 8.

**Key Entry Points:** [Program.cs](../KitaKo/Program.cs), [HomeController.cs](../KitaKo/Controllers/HomeController.cs), [ApplicationDbContext.cs](../KitaKo/Data/ApplicationDbContext.cs)

---

## Architecture & Core Patterns

### Layered Structure
- **Models** (`KitaKo/Models/`): Domain entities + ViewModels (User, Expenses, Sale, Utang, LoginViewModel, etc.)
- **Data** (`KitaKo/Data/`): DbContext, generic Repository pattern
- **Controllers** (`KitaKo/Controllers/`): HomeController for views, Api/ for REST endpoints
- **Services** (`KitaKo/Services/`): AuthService (authentication), KnapsackService (expense optimization)
- **Views** (`KitaKo/Views/`): Razor templates organized by controller (Home/, Profile/, Shared/)

### Data Access Pattern
Generic async repository at [IRepository.cs](../KitaKo/Data/Repositories/IRepository.cs) with async/await throughout. **DO NOT** add synchronous LINQ queries—all data operations must use `await` and `GetAllAsync()`, `GetByIdAsync()`, etc.

### Authentication Flow
1. **AuthService** ([AuthorizationService.cs](../KitaKo/Services/AuthorizationService.cs)) handles registration & login
2. Passwords hashed with SHA256 (base64 encoded)
3. Session-based: UserId and Username stored in `HttpContext.Session` after successful login
4. All protected actions check `HttpContext.Session.GetString("UserId")` before proceeding
5. **Important:** No claim-based authorization—session-only

---

## Critical Conventions

### User Context & Multi-tenancy
Every entity (Expenses, Sale, Utang) has `UserId` field. **Always** filter queries by user:
```csharp
var userExpenses = expenses.Where(e => e.UserId == userId);  // MANDATORY
```

### Expense Optimization Algorithm
[KnapsackService.cs](../KitaKo/Services/KnapsackService.cs) uses **0/1 Knapsack DP** to select highest-value expenses within budget:
- Input: unpaid expenses, budget cap
- Value = `Priority * 100` (priority field 1-5)
- Weights = expense amounts
- Returns: selected expenses + remaining budget
- **Use case:** Dashboard optimization recommendations

### DateTime Handling
- All timestamps use `CURRENT_TIMESTAMP` SQL default (PostgreSQL UTC)
- When setting dates in code: `DateTime.UtcNow`
- For API: ensure `DueDate.Kind == DateTimeKind.Utc` before saving (see ExpensesController)

### Configuration
PostgreSQL connection in [appsettings.json](../KitaKo/appsettings.json):
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=KitaKo;Username=postgres;Password=postgres"
```
**Setup required:** Ensure PostgreSQL server running on localhost:5432 before dotnet run

---

## Dependency Injection & Initialization
- DbContext registered as scoped in [Program.cs](../KitaKo/Program.cs)
- Generic Repository `IRepository<T>` auto-resolved via DI
- AuthService instantiated manually in HomeController (anti-pattern—consider DI refactor)
- **Pattern violation:** HomeController creates `new KnapsackService()` and `new AuthService()` directly instead of injecting

---

## File Organization for New Features

When adding new domain entity (e.g., Budget):
1. **Model**: `KitaKo/Models/Budget.cs` with UserId field
2. **DbSet**: Add to `ApplicationDbContext.OnModelCreating()` with fluent configuration
3. **Repository**: No separate class needed—use `IRepository<Budget>` directly
4. **Migration**: `dotnet ef migrations add AddBudgetTable` then `dotnet ef database update`
5. **Controller**: `KitaKo/Controllers/Api/BudgetController.cs` or view logic in HomeController
6. **Service**: If complex logic (like optimization), create in `KitaKo/Services/`

---

## Testing & Running

### Build & Run
```bash
cd KitaKo
dotnet restore
dotnet build
dotnet run
```
App launches on HTTPS localhost (check launchSettings.json for port)

### Database
```bash
dotnet ef database drop  # Reset
dotnet ef migrations add MigrationName
dotnet ef database update
```

### API Testing
Endpoints follow RESTful pattern:
- `GET /api/expenses` → all user expenses (requires UserId filtering in controller)
- `POST /api/expenses` → create expense
- See [ExpensesController.cs](../KitaKo/Controllers/Api/ExpensesController.cs) and Api/SalesController, Api/UtangsController for patterns

---

## Known Issues & Improvements Needed

1. **Hardcoded Service Instantiation**: HomeController should inject AuthService & KnapsackService instead of `new`
2. **Authorization**: No attribute-based authorization—all auth checks manual in actions
3. **Password Hashing**: SHA256 is weak; consider bcrypt/PBKDF2
4. **Null Dereferences**: ViewModels use `string.Empty` defaults; validate all inputs
5. **Profile Photo**: Stored in wwwroot/uploads/profiles/—ensure directory exists

---

## Common AI Tasks

- **Add new expense field**: Update model → migration → controller validation
- **New optimization algorithm**: Extend KnapsackService with new method, keep signature similar
- **Auth improvements**: Check all session guards match pattern at HomeController.Dashboard()
- **API endpoint**: Copy pattern from ExpensesController, add UserId filter
- **Fix queries**: Convert `.ToList()` to `await GetAllAsync()`, add `.ConfigureAwait(false)` if UI thread risk
