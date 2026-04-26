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

# Run database migrations (if configured)
dotnet ef database update --project src/JustGoAPI.API
```

---

### Configuration

Configure `appsettings.json` or set environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=JustGoDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "JustGoAPI",
    "Audience": "JustGoClients",
    "ExpiryInMinutes": 60
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

**Environment Variables:**
```bash
ConnectionStrings__Default=Server=localhost;Database=JustGoDB;...
Jwt__Secret=your-secret-key
```

---

### Run Application

```bash
# Run the API
dotnet run --project src/JustGoAPI.API

# Or using Visual Studio
# Set JustGoAPI.API as startup project and press F5
```

The API will be available at: `http://localhost:5000`

---

## 🌐 Usage

**Base URL:** `http://localhost:5000`

**Swagger/OpenAPI Documentation:** `http://localhost:5000/swagger`

**Health Check:** `http://localhost:5000/health`

**Example API Requests:**

```http
# Get all assets
GET /api/assets

# Create a new booking
POST /api/bookings
Content-Type: application/json

{
  "classId": "123",
  "memberId": "456",
  "bookingDate": "2026-04-27"
}

# Get member profile
GET /api/members/{id}/profile

# Get financial transactions
GET /api/finance/transactions?fromDate=2026-01-01
```

**Authentication:**
```http
# Login
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "your-password"
}

# Use the returned JWT token in subsequent requests
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

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