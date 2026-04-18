using OpenMES.Data.Common;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Join entity that associates a shop floor terminal (<see cref="ClientDevice"/>)
/// with one or more machines (<see cref="Machine"/>).
///
/// A terminal can manage multiple machines (e.g. a panel serving a cell of 3 lathes).
/// This record defines which machines are reachable from a given terminal, controlling
/// which machines an operator can select when opening a work session from that device.
/// </summary>
public class ClientMachine : IKey<int>, IBaseDates
{
	public int Id { get; set; }
	public int ClientDeviceId { get; set; }
	public DateTimeOffset CreatedAt { get;set; } 
	public DateTimeOffset UpdatedAt { get; set; }
	public int MachineId { get; set; }

	public virtual ClientDevice ClientDevice { get; set; } = null!;
	public virtual Machine Machine { get; set; } = null!;
}
