using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Common;

public interface IKey<T>
{
	[Key]
	public T Id { get; set; }
}
