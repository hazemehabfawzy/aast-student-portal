using System.Collections.Generic;

namespace StudentPortal.Api.Models.DTOs.Responses;

public class BulkImportResponse
{
    public int TotalRows { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<BulkImportError> Errors { get; set; } = new();
}

public class BulkImportError
{
    public int RowNumber { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
