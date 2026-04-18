namespace OpenMES.WebClient.Models;

public class Wip
{
	public int PlannedQuantity { get; set; }
	public int ProcessedQuantity { get; set; }
	public int ScrapQuantity { get; set; }

	public double PlannedTime { get; set; }
	public double ProcessedTime { get; set; }


}
