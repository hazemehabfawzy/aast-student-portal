using System.IO;
using System.Threading.Tasks;
using StudentPortal.Api.Models.DTOs.Responses;

namespace StudentPortal.Api.Services.Interfaces;

public interface IImportService
{
    Task<BulkImportResponse> ImportStudentsAsync(Stream fileStream, string contentType);
    Task<BulkImportResponse> ImportInstructorsAsync(Stream fileStream, string contentType);
}
