# KitaKo — Run Guide (Windows)

Simple steps to run KitaKo locally using PowerShell.

1) Open PowerShell inside the project folder

```powershell
cd C:\Users\charm\Documents\GitHub\KitaKo\KitaKo
```

2) Create or edit `appsettings.Development.json`

- If the file is missing, copy from `appsettings.json`:

```powershell
Copy-Item .\appsettings.json .\appsettings.Development.json
```

- Open the file and set your PostgreSQL connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=kitako_dev;Username=DB_USER;Password=DB_PASS"
}
```

3) Install EF Core tool if needed

```powershell
dotnet tool install --global dotnet-ef
```

4) Apply database migrations

```powershell
dotnet ef database update
```

5) Run the app

```powershell
$Env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

6) Open the browser

- Use the URL shown in the terminal.
- Most likely: `https://localhost:5001`

### If `dotnet ef` does not run

```powershell
$env:PATH += ";" + $env:USERPROFILE + "\.dotnet\tools"
dotnet ef database update
```

### Quick built-in commands

```powershell
dotnet build
dotnet run
```

