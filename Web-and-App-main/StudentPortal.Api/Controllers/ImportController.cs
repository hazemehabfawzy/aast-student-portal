using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("students/bulk-import")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> BulkImportStudents(IFormFile? file)
    {
        var uploadedFile = file;
        if (uploadedFile == null && Request.HasFormContentType)
        {
            uploadedFile = Request.Form.Files.FirstOrDefault();
        }
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded or file is empty." });
        }

        try
        {
            using var stream = uploadedFile.OpenReadStream();
            var response = await _importService.ImportStudentsAsync(stream, uploadedFile.ContentType);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("instructors/bulk-import")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> BulkImportInstructors(IFormFile? file)
    {
        var uploadedFile = file;
        if (uploadedFile == null && Request.HasFormContentType)
        {
            uploadedFile = Request.Form.Files.FirstOrDefault();
        }
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded or file is empty." });
        }

        try
        {
            using var stream = uploadedFile.OpenReadStream();
            var response = await _importService.ImportInstructorsAsync(stream, uploadedFile.ContentType);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
