namespace StudentPortal.Api.Models.DTOs.Responses;

public class ConflictResponse
{
    public bool Conflict { get; set; }
    public string ConflictingWith { get; set; } = string.Empty;
    public List<AlternativeSectionDto> Alternatives { get; set; } = new();
}

public class AlternativeSectionDto
{
    public Guid SectionId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public int SeatsLeft { get; set; }
}
