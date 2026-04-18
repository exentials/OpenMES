# OpenMES.Data and Migrations Instructions

## Scope
Covers:
- `OpenMES.Data`
- `OpenMES.Data.Dtos`
- `OpenMES.Data.Common`
- `OpenMES.Data.SqlServer`
- `OpenMES.Data.Pgsql`
- `OpenMES.MigrationService`

## Data modeling rules
- Prefer explicit FK relations over denormalized strings.
- Keep `AsDto`/`AsEntity` mappings aligned with schema.
- Maintain both providers' migration parity.

## Current model highlights
- Warehouse hierarchy is active: `Plant -> Warehouse -> StorageLocation`.
- Material supports optional defaults: `DefaultWarehouseId`, `DefaultStorageLocationId`.

## Migration rules
1. Generate both SQL Server and PostgreSQL migrations.
2. Ensure snapshots remain consistent.
3. Keep seeding order consistent with FK dependencies.

## Migration tools (PowerShell, Copilot-friendly)
Scripts are in repository root `tools/` and can run non-interactively.

### Data model migrations
- Command: `pwsh ./tools/migrateData.ps1 -MigrationName "YOUR_NAME"`
- Reset + init: `pwsh ./tools/migrateData.ps1 -Reset`

### Identity migrations
- Command: `pwsh ./tools/migrateIdentity.ps1 -MigrationName "YOUR_NAME"`
- Reset + init: `pwsh ./tools/migrateIdentity.ps1 -Reset`

### Behavior when `-MigrationName` is omitted
- With `-Reset`: migration name defaults to `INIT`
- Without `-Reset`: migration name is auto-generated as `AUTO_yyyyMMddHHmmss`
- `-Interactive` can be used to enable prompt mode

## Seeding rules
- Create `Plant` before `Warehouse`.
- Create `Warehouse` before `StorageLocation`.
- Ensure baseline presence shifts exist where tests rely on declaration flow.

## Validation
- Run build after entity/DTO/resource changes.
- Run declaration/worksession/operator shift tests after domain rule changes.
