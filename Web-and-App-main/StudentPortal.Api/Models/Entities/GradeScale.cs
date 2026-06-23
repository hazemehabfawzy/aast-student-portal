namespace StudentPortal.Api.Models.Entities;

public class GradeScale
{
    public Guid Id { get; set; }
    public string Letter { get; set; } = string.Empty;
    public float? MinPercent { get; set; }
    public float? MaxPercent { get; set; }
    public float? GpaPoints { get; set; }
    public bool CountsTowardGpa { get; set; }
}
