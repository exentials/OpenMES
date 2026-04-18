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

## Seeding rules
- Create `Plant` before `Warehouse`.
- Create `Warehouse` before `StorageLocation`.
- Ensure baseline presence shifts exist where tests rely on declaration flow.

## Validation
- Run build after entity/DTO/resource changes.
- Run declaration/worksession/operator shift tests after domain rule changes.
