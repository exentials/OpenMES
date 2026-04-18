using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OpenMES.Data.Entities;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Contexts;

public class OpenMESDbContext : DbContext
{
	public OpenMESDbContext()
	{
	}

	public OpenMESDbContext(DbContextOptions<OpenMESDbContext> options) : base(options)
	{
	}

	public DbSet<ClientDevice> ClientDevices { get; set; }
	public DbSet<ClientMachine> ClientMachines { get; set; }
	public DbSet<Machine> Machines { get; set; }
	public DbSet<Material> Materials { get; set; }
	public DbSet<Operator> Operators { get; set; }
	public DbSet<Plant> Plants { get; set; }
	public DbSet<ProductionJob> ProductionJobs { get; set; }
	public DbSet<ProductionOrder> ProductionOrders { get; set; }
	public DbSet<ProductionOrderPhase> ProductionOrderPhases { get; set; }
	public DbSet<WorkCenter> WorkCenters { get; set; }

	// Production Tracking
	public DbSet<MachineStopReason>     MachineStopReasons     { get; set; }
	public DbSet<MachineStop>           MachineStops           { get; set; }
	public DbSet<ProductionDeclaration> ProductionDeclarations { get; set; }

	// Quality & Inspections
	public DbSet<InspectionPlan>    InspectionPlans    { get; set; }
	public DbSet<InspectionPoint>   InspectionPoints   { get; set; }
	public DbSet<InspectionReading> InspectionReadings { get; set; }
	public DbSet<NonConformity>     NonConformities    { get; set; }

	// Warehouse
	public DbSet<Warehouse> Warehouses { get; set; }
	public DbSet<StorageLocation> StorageLocations { get; set; }
	public DbSet<MaterialStock>   MaterialStocks   { get; set; }
	public DbSet<StockMovement>   StockMovements   { get; set; }

	// Picking
	public DbSet<PhasePickingList> PhasePickingLists { get; set; }
	public DbSet<PhasePickingItem> PhasePickingItems { get; set; }

	// Declarations
	public DbSet<OperatorShift>  OperatorShifts  { get; set; }
	public DbSet<WorkSession>    WorkSessions    { get; set; }
	public DbSet<MachineState>   MachineStates   { get; set; }
	public DbSet<MachinePhasePlacement> MachinePhasePlacements { get; set; }

	public override int SaveChanges()
	{
		ApplyBaseDates();
		return base.SaveChanges();
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		ApplyBaseDates();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		ApplyBaseDates();
		return base.SaveChangesAsync(cancellationToken);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		ApplyBaseDates();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	private void ApplyBaseDates()
	{
		var now = DateTimeOffset.UtcNow;

		foreach (var entry in ChangeTracker.Entries<IBaseDates>())
		{
			if (entry.State == EntityState.Added)
			{
				entry.Entity.CreatedAt = now;
				entry.Entity.UpdatedAt = now;
			}
			else if (entry.State == EntityState.Modified)
			{
				entry.Entity.UpdatedAt = now;
				entry.Property(x => x.CreatedAt).IsModified = false;
			}
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<ClientDevice>()
			.HasIndex(m => m.Name).IsUnique();

        // Master Data
		modelBuilder.Entity<Plant>()
			.HasIndex(m => m.Code).IsUnique();		
		modelBuilder.Entity<Material>()
			.HasIndex(m => m.PartNumber).IsUnique();
		modelBuilder.Entity<ProductionOrder>()
			.HasIndex(m => m.OrderNumber).IsUnique();
		modelBuilder.Entity<ProductionOrderPhase>()
			.HasIndex(m => new { m.ProductionOrderId, m.PhaseNumber }).IsUnique();
		modelBuilder.Entity<ProductionOrderPhase>()
			.HasAlternateKey(m => new { m.ExternalId });
		modelBuilder.Entity<WorkCenter>()
			.HasIndex(m => m.Code).IsUnique();
		modelBuilder.Entity<Machine>()
			.HasIndex(m => m.Code).IsUnique();

		// Production Tracking
		modelBuilder.Entity<MachineStopReason>()
			.HasIndex(m => m.Code).IsUnique();

		// Quality & Inspections
		modelBuilder.Entity<InspectionPlan>()
			.HasIndex(m => new { m.Code, m.Version }).IsUnique();
		modelBuilder.Entity<NonConformity>()
			.HasIndex(m => m.Code).IsUnique();

		// Warehouse
		modelBuilder.Entity<StorageLocation>()
			.HasIndex(m => m.Code).IsUnique();
		modelBuilder.Entity<MaterialStock>()
			.HasIndex(m => new { m.MaterialId, m.StorageLocationId }).IsUnique();

		// Declarations
		modelBuilder.Entity<MachinePhasePlacement>()
			.HasIndex(m => new { m.MachineId, m.ProductionOrderPhaseId, m.UnplacedAt });

		foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
		{
			relationship.DeleteBehavior = DeleteBehavior.NoAction;
		}
		//modelBuilder.Entity<ProductionOrderPhase>().HasOne(m => m.WorkCenter).WithMany().OnDelete(DeleteBehavior.NoAction);

	}
}
