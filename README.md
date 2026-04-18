# OpenMES

**OpenMES** is an open-source **Manufacturing Execution System (MES)** built with **.NET 10** for small and medium manufacturing companies.

It helps teams digitalize shop-floor operations with one platform for production tracking, machine downtime, quality checks, and stock movements.

---

## Why OpenMES

OpenMES connects operators, supervisors, and administrators through two Blazor applications and a central API:

- **WebAdmin**: back-office management and configuration
- **WebClient**: operator terminal for real-time declarations on the shop floor
- **WebApi**: secure REST API for all business operations

The solution is orchestrated with **.NET Aspire** and supports both **SQL Server** and **PostgreSQL**.

---

## Core capabilities

- **Production management**: orders, phases, declared quantities, scrap
- **Machine monitoring**: stop events, categorized causes, downtime analysis
- **Quality control**: inspection plans, readings, non-conformities
- **Warehouse tracking**: storage locations, stock balances, immutable movement ledger
- **Terminal workflows**: fast touchscreen-friendly operations for operators
- **Lookup endpoints for admin forms**: standardized `keyvalue` API responses (`Id/Key/Value`) for reusable Blazor select components

---

## Technology at a glance

- **Backend**: ASP.NET Core, Entity Framework Core
- **Frontend**: Blazor Server + Microsoft FluentUI
- **Orchestration**: .NET Aspire
- **Security**: ASP.NET Core Identity + JWT
- **Deployment model**: container-friendly, service-based architecture

---

## Solution structure

```text
src/
  OpenMES.AppHost/            Aspire orchestration
  OpenMES.MigrationService/   Database migration worker
  OpenMES.WebApi/             REST API
  OpenMES.WebAdmin/           Admin Blazor app
  OpenMES.WebClient/          Shop-floor Blazor app
  OpenMES.Data*/              Data model, DTOs, provider migrations
  OpenMES.WebApiClient/       Typed API client
  OpenMES.ServiceDefaults/    Shared Aspire defaults
tools/                        Repository automation and maintenance scripts
```

---

## Documentation

- **Product/functional docs**: `docs/`
- **Contributing**: See LICENSE and community guidelines in the repository

---

## Quick start

### Prerequisites

- .NET 10 SDK
- .NET Aspire workload
- Docker Desktop
- SQL Server or PostgreSQL

### Run

```bash
dotnet run --project src/OpenMES.AppHost
```

Then open the Aspire dashboard and start using the platform.
