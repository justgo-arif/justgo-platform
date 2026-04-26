# 🚀 JustGo Platform

## 📌 Overview

A multi-tenant SaaS backend platform powering JustGo, providing modular REST APIs for booking management, membership systems, asset management, and financial operations.

**What it does:**
- Provides a comprehensive backend platform for managing bookings, memberships, assets, and financial transactions
- Supports multi-tenancy with isolated data per organization
- Offers modular architecture allowing independent development and deployment of business domains

**Why it exists:**
- To provide a scalable, maintainable backend foundation for the JustGo ecosystem
- Enable rapid development of new features through modular architecture
- Ensure clean separation of concerns using Domain-Driven Design principles

**Who it's for:**
- Organizations needing a robust booking and membership management system
- Developers working on the JustGo platform
- Teams requiring a multi-tenant SaaS solution with asset and finance capabilities

---

## 🧾 Problem / Motivation

Traditional monolithic applications suffer from tight coupling, making it difficult to maintain and scale. JustGo Platform addresses this by implementing a **DDD Modular Monolith** architecture that provides:

- **Clear module boundaries** for independent development
- **Multi-tenancy support** for serving multiple organizations
- **Extensible domain model** for complex business logic
- **Scalable architecture** that can evolve to microservices if needed

---

## 🏗️ Architecture

JustGo Platform follows **Domain-Driven Design (DDD) with Modular Monolith** architecture:

* **API Layer** → Controllers, Swagger configuration, API versioning (JustGoAPI.API)
* **Application Layer** → Commands, queries, handlers using MediatR pattern
* **Domain Layer** → Aggregates, entities, value objects, domain events (pure C#, no infrastructure)
* **Infrastructure Layer** → Entity Framework DbContexts, repositories, external service adapters
* **SharedKernel** → Domain primitives, base classes, pagination, results

**Key Architectural Principles:**
- Each module represents one DDD domain
- Bounded Contexts are internal namespaces within Domain projects
- Inter-module communication through Contracts projects only
- Separate database schema per module

---

## 🛠️ Tech Stack

* **.NET 9.0** / ASP.NET Core Web API
* **Entity Framework Core 9.0.1** (ORM)
* **SQL Server** (Database)
* **JWT Authentication** (IdentityServer-like token generation)
* **ABAC Authorization** (Attribute-Based Access Control)
* **Serilog** (Structured logging)
* **Swashbuckle/Swagger** (API documentation)
* **MediatR** (CQRS pattern implementation)
* **NetArchTest** (Architecture validation)

---

## 📂 Project Structure

```text
JustGoAPI.sln
│
├── src/
│   ├── JustGoAPI.API/                    # Entry point - hosts all modules
│   ├── JustGo.Authentication/            # Shared authentication infrastructure
│   ├── JustGo.Functions/                 # Azure Functions integration
│   ├── JustGo.RuleEngine/                # Business rule evaluation engine
│   ├── JustGoAPI.Shared/                 # Shared utilities and DTOs
│   │
│   └── Modules/
│       ├── AssetManagementModule/        # Asset catalogue, register, leasing
│       ├── AuthModule/                   # Identity, tenancy, authorization
│       ├── BookingModule/                # Class schedules, attendees, transfers
│       ├── CredentialModule/             # Member credentialing
│       ├── FieldManagementModule/        # Extension schemas, user preferences
│       ├── FinanceModule/                # Financial transactions
│       ├── MemberProfileModule/          # Member profile management
│       ├── MembershipModule/             # Membership plans and subscriptions
│       ├── MobileAppsModule/             # Mobile app-specific APIs
│       ├── OrganisationModule/           # Organization management
│       └── ResultModule/                 # Results and assessments
│
├── tests/
│   ├── JustGo.ArchitectureTests/         # Enforces architectural rules
│   └── {Module}.UnitTests/               # Per-module unit tests
│
├── docs/
│   ├── DDD-Modular-Monolith-Architecture.md
│   ├── JustGoAPI_SRS.pdf
│   └── SRS/                             # Software Requirements Specification
│
└── scripts/                              # Build and deployment scripts
```

**Each module follows this internal structure:**
```
{ModuleName}/
├── {ModuleName}.Domain/          # Core business logic
├── {ModuleName}.Application/     # Use cases, commands, queries
├── {ModuleName}.Infrastructure/  # DB, external services
└── {ModuleName}.API/             # REST controllers
```

---

## ⚙️ Getting Started

### Prerequisites

* **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
* **SQL Server** (2019 or later) or **SQL Server Express**
* **Visual Studio 2022** (recommended) or **VS Code** with C# extension
* **Git** for version control

---

### Installation

```bash
# Clone the repository
git clone https://github.com/justgo-arif/justgo-platform.git
cd justgo-platform

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# (Optional) Apply pending EF Core migrations — skip if restoring from .bacpac
# dotnet ef database update --project src/JustGoAPI.API
```

---

## 🗄️ Database Setup

> **Note:** Ensure your **Azure VPN is connected** before attempting any database access.

### Databases

The platform uses two SQL Server databases:

| Database | Purpose |
|---|---|
| [`Development_286`](https://drive.google.com/file/d/1SV8ih1DBnPac-FIf7Ke_UM31ZXqw9zEM/view?usp=drive_link) | Main application database (bookings, members, assets, etc.) |
| [`restapi_common_db_v1`](https://drive.google.com/file/d/1jaMhvEMs7a4ktv5n1puOUewHKWZXnXPB/view?usp=sharing) | Shared API / tenant management database |

### Restore from Backup (.bacpac)

Download links are listed in the **Databases** table above.

> 🔑 **Zip extraction password:** `justgobd1234`

Use SQL Server Management Studio (SSMS) to restore each database from a `.bacpac` backup file:

1. Open **SQL Server Management Studio (SSMS)**
2. In Object Explorer, right-click **Databases**
3. Select **Import Data-tier Application...**
4. Browse to your `.bacpac` file and click **Next**
5. Set the **New Database Name**:
   - `Development_286`
   - `restapi_common_db_v1` *(repeat for the second database)*
6. Review settings and click **Finish**

### SQL Server User & Login Setup

Run the following script as a SQL Server administrator. Replace `YOUR_SQL_LOGIN` and the password placeholder with your actual values.

```sql
USE master;
GO

-- 1. Create login
CREATE LOGIN [YOUR_SQL_LOGIN]
WITH PASSWORD = 'YourStrongPassword@123',
     CHECK_POLICY = OFF;
GO

-- 2. Enable login
ALTER LOGIN [YOUR_SQL_LOGIN] ENABLE;
GO

-- 3. Grant access to Development_286
USE [Development_286];
GO

CREATE USER [YOUR_SQL_LOGIN] FOR LOGIN [YOUR_SQL_LOGIN];
ALTER ROLE [db_owner] ADD MEMBER [YOUR_SQL_LOGIN];
GO

-- 4. Grant access to restapi_common_db_v1
USE [restapi_common_db_v1];
GO

CREATE USER [YOUR_SQL_LOGIN] FOR LOGIN [YOUR_SQL_LOGIN];
ALTER ROLE [db_owner] ADD MEMBER [YOUR_SQL_LOGIN];
GO
```

> **Security:** `CHECK_POLICY = OFF` is for development convenience only. Use strong passwords and enforce password policy in production. Consider a less-privileged role than `db_owner` for production workloads.

### Verify Setup

**Check the login exists and is enabled (run on master):**

```sql
SELECT name, is_disabled
FROM sys.server_principals
WHERE name = 'YOUR_SQL_LOGIN';
```

**Check the user exists in each database:**

```sql
USE [Development_286];
SELECT name FROM sys.database_principals WHERE name = 'YOUR_SQL_LOGIN';

USE [restapi_common_db_v1];
SELECT name FROM sys.database_principals WHERE name = 'YOUR_SQL_LOGIN';
```

### Tenant Initialization

After restoring the databases, run these queries against `restapi_common_db_v1` to configure local tenant data:

```sql
-- View current tenant data
SELECT * FROM Tenants;
SELECT * FROM TenantDatabases;

-- Update API URL to match your local port
UPDATE Tenants
SET ApiUrl = 'https://localhost:7052/api/Account/GetMessage';

-- Update tenant domain URL
UPDATE Tenants
SET TenantDomainUrl = 'https://localhost:44347/';

-- Update database name reference
UPDATE TenantDatabases
SET DatabaseName = 'Development_286';

-- Update database credentials
-- DBUserId and DBPassword must be the *encrypted* values of the SQL login
-- and password created in the "SQL Server User & Login Setup" step above.
-- Use the application's encryption utility to generate these values first.
UPDATE TenantDatabases
SET DBUserId   = '<encrypted-sql-login>',
    DBPassword = '<encrypted-sql-password>';

-- Verify user data
SELECT TOP 1 * FROM [User];
```

> **Important:** `DBUserId` and `DBPassword` store **encrypted** text, not plain-text credentials. Generate the encrypted values using the platform's encryption utility (matching the same algorithm used by the application) before running the update above.

---

### Configuration

Update the `ConnectionStrings` section in `appsettings.Development.json` with your SQL Server instance and the login created in the setup step above:

```json
"ConnectionStrings": {
  "ApiConnection": "Data Source=YOUR_SERVER,1433;initial catalog=restapi_common_db_v1;User ID=YOUR_SQL_LOGIN;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=False;",
  "AzolveCentralDB": "Server=YOUR_SERVER;Database=YOUR_DB_NAME;User Id=YOUR_SQL_LOGIN;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=False;",
  "AddressPickerCore": "Server=YOUR_SERVER;Database=YOUR_DB_NAME;User Id=YOUR_SQL_LOGIN;Password=YOUR_PASSWORD;Encrypt=False;TrustServerCertificate=False;"
}
```

> **Tip:** Never commit real credentials to source control. Use [`dotnet user-secrets`](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables for local development.

---

### Run Application

```bash
# Run the API
dotnet run --project src/JustGoAPI.API

# Or using Visual Studio
# Set JustGoAPI.API as startup project and press F5
```

The API will be available at: `http://localhost:5000`

## 🌐 Usage

**Base URL:** `http://localhost:5000`

**Swagger/OpenAPI Documentation:** `http://localhost:5000/swagger`

**Health Check:** `http://localhost:5000/health`

**Example API Requests:**

**Authentication:**
```http
# Authenticate and retrieve a JWT token
POST /api/v1/accounts/authenticate
Content-Type: application/json

{
  "tenantClientId": "DTQ-01",
  "loginId": "admin",
  "password": "tes-S1sapphire@"
}

# Use the returned JWT token in subsequent requests
Authorization: Bearer <token-from-response>
```

> **Note:** The credentials above are for local development/testing only. Do not use them in any shared or production environment.

**Verify Token (subsequent call):**
```http
# Retrieve a user by their sync ID — use the JWT token from the authenticate response
GET /api/v1/users/user-by-guid?userSyncId=<USER_SYNC_ID>
Accept: */*
Authorization: Bearer <token-from-authenticate-response>
X-Tenant-Id: <TENANT_CLIENT_ID>
```

```bash
# cURL equivalent
curl -X GET \
  'http://localhost:5152/api/v1/users/user-by-guid?userSyncId=<USER_SYNC_ID>' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer <token-from-authenticate-response>' \
  -H 'X-Tenant-Id: <TENANT_CLIENT_ID>'
```

| Placeholder | Description |
|---|---|
| `<USER_SYNC_ID>` | The `userSyncId` GUID returned in the authenticate response |
| `<token-from-authenticate-response>` | The JWT token returned by the authenticate endpoint |
| `<TENANT_CLIENT_ID>` | Tenant identifier, e.g. `DTQ-01` |

---

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run specific module tests
dotnet test src/modules/AssetManagementModule/tests/JustGo.AssetManagement.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run architecture tests to verify module boundaries
dotnet test tests/JustGo.ArchitectureTests
```

---

## 📚 Documentation

* **Architecture Overview** → [`docs/DDD-Modular-Monolith-Architecture.md`](docs/DDD-Modular-Monolith-Architecture.md)
* **Software Requirements** → [`docs/JustGoAPI_SRS.pdf`](docs/JustGoAPI_SRS.pdf)
* **SRS Details** → [`docs/SRS/`](docs/SRS/)
* **Module Documentation** → See each module's `README.md` (if available)

---

## 🐞 Common Issues

**Issue: Port 5000 already in use**
```bash
# Change port in launchSettings.json or use:
dotnet run --project src/JustGoAPI.API --urls="http://localhost:5001"
```

**Issue: Database connection failed**
- Verify SQL Server is running
- Check connection string format
- Ensure `TrustServerCertificate=True` for development

**Issue: Migration errors**
```bash
# Reset and re-create database
dotnet ef database drop --force
dotnet ef database update
```

**Issue: JWT token validation errors**
- Verify JWT Secret matches between client and server
- Check token expiration time
- Ensure `Bearer` prefix is used in Authorization header

**Issue: Module dependency errors**
- Run architecture tests: `dotnet test tests/JustGo.ArchitectureTests`
- Verify module boundaries are not violated
- Check that Contracts projects are used for inter-module communication

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork** the repository
2. Create a **feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. Open a **Pull Request**

### Branching Strategy

Current running version: **V1**

| Branch | Purpose | Current Branch Name |
|---|---|---|
| `dev` | Developer branch | `Dev/V1/286_V1_JustGo_RestAPI` |
| `sandbox` | Testing environment / patch branch | `Sand/V1/286_V1_JustGo_RestAPI` |
| `prod` | Production branch / production patching | `Prod/V1/286_V1_JustGo_RestAPI` |

**Development Guidelines:**
- Follow the existing code style and naming conventions
- Add unit tests for new functionality
- Run architecture tests to ensure module boundaries are respected
- Update documentation as needed
- Ensure all tests pass before submitting PR

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Copyright © 2026 justgo-arif

---

## 📞 Contact

* **Author:** justgo-arif
* **GitHub:** [@justgo-arif](https://github.com/justgo-arif)
* **Repository:** [https://github.com/justgo-arif/justgo-platform](https://github.com/justgo-arif/justgo-platform)

