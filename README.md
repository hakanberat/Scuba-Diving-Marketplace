# Scuba Diving Marketplace

A scuba diving marketplace where divers can find activities, explore the centers that offer them and request a reservation. Dive center owners manage their listings and incoming requests, while administrators review applications from centers that want to join the marketplace.

## Features

| User | Available features |
| --- | --- |
| Visitor | Search activities by keyword, country, city, category and price range. Explore center profiles, instructors and photo galleries. |
| Customer | Request a reservation, choose how the center can get in touch and track the request in **My Reservations**. |
| Dive center owner | Apply to list a center, track the application and, once approved, manage the center profile, instructors, activities, photos and reservation requests. |
| Administrator | Approve or reject center applications, manage users and activity categories, assign users to centers and activate or deactivate center listings. |

## Technology

| Component | Implementation |
| --- | --- |
| Web application | C#, .NET 10, ASP.NET Core MVC and Razor views |
| Database | Microsoft SQL Server |
| Data access | Dapper and Microsoft.Data.SqlClient |
| Authentication | Cookie authentication with the ASP.NET Core password hasher |
| Authorization | Role-based access for customers, dive center owners and administrators |
| Testing | PowerShell smoke tests for core user workflows |

## Run locally on Windows

### 1. Clone the repository

Install a .NET 10 SDK compatible with [global.json](global.json), SQL Server Express, SQL Server Management Studio and the `sqlcmd` command-line utility.

```powershell
git clone https://github.com/hakanberat/Scuba-Diving-Marketplace.git
cd Scuba-Diving-Marketplace
dotnet restore
```

### 2. Prepare the database

The default connection in [appsettings.json](appsettings.json) uses Windows authentication, the `localhost\SQLEXPRESS` instance and a database named `DiveMarketplaceDb`.

The scripts in [Database](Database/) expect `dbo.DiveCenters` to exist before script `002` runs. For a fresh local installation, connect to `localhost\SQLEXPRESS` in SQL Server Management Studio and run the following SQL. Script `012` adds the map location column later.

```sql
USE master;
GO

IF DB_ID(N'DiveMarketplaceDb') IS NULL
    CREATE DATABASE DiveMarketplaceDb;
GO

USE DiveMarketplaceDb;
GO

IF OBJECT_ID(N'dbo.DiveCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiveCenters (
        Id          int IDENTITY(1,1) PRIMARY KEY,
        Name        nvarchar(200) NOT NULL,
        Description nvarchar(max) NULL,
        Country     nvarchar(100) NULL,
        City        nvarchar(100) NULL,
        Address     nvarchar(500) NULL,
        Phone       nvarchar(50) NULL,
        Email       nvarchar(200) NULL,
        Website     nvarchar(500) NULL,
        LogoUrl     nvarchar(500) NULL,
        IsActive    bit NOT NULL DEFAULT 1,
        CreatedAt   datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO
```

From the repository root, run the numbered scripts in PowerShell. The command skips `009_PromoteFirstAdmin.sql` because that script requires a registered account.

```powershell
$scripts = Get-ChildItem .\Database\*.sql |
    Where-Object { $_.Name -notlike '009_*' } |
    Sort-Object Name

foreach ($script in $scripts) {
    sqlcmd -S 'localhost\SQLEXPRESS' -d DiveMarketplaceDb -E -C -I -b -i $script.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "Database setup failed at $($script.Name)"
    }
}
```

### 3. Start the application

```powershell
dotnet run --project .\DiveMarketplace.csproj --launch-profile http
```

Open [http://localhost:5258](http://localhost:5258).

### 4. Try the reservation workflow

The seed script creates activity categories such as **Try Dive**, **Boat Dive** and **Wreck Dive**. To take a listing from a center application to a customer reservation:

1. Register a customer account. To make it the first administrator, follow the instructions in [009_PromoteFirstAdmin.sql](Database/009_PromoteFirstAdmin.sql), then sign in again.
2. Using a separate account, submit a dive center application.
3. Sign in as the administrator and approve the application.
4. Sign in as the center owner, complete the center profile and publish an activity.
5. As a customer, open the activity, submit a reservation request and check its status under **My Reservations**.
6. Return to the center owner account to review the request and update its status.

## Smoke tests

With the application running, open a second PowerShell terminal in the repository root. Use a local test database because the smoke test creates records and connects directly to `DiveMarketplaceDb` on `localhost\SQLEXPRESS`.

```powershell
.\Tests\SmokeTest.ps1 -BaseUrl 'http://localhost:5258'
```

The [Tests](Tests/) directory also includes scripts for photo management, notifications, profile updates, reservation contact details and reservation revenue.
