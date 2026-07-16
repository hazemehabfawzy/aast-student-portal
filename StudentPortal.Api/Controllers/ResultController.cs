using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Services.Implementations;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultController : ControllerBase
{
    private readonly IResultService _resultService;
    private readonly GradePredictionClient _prediction;

    public ResultController(IResultService resultService, GradePredictionClient prediction)
    {
        _resultService = resultService;
        _prediction    = prediction;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpGet("students/me/results")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyResults()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _resultService.GetMyResultsAsync(keycloakId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("students/me/results/{semesterId:guid}")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyResultsForSemester(Guid semesterId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _resultService.GetMyResultsAsync(keycloakId, semesterId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sections/{sectionId:guid}/results")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetSectionResults(Guid sectionId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _resultService.GetSectionResultsAsync(keycloakId, sectionId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("results/{enrollmentId:guid}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateResult(Guid enrollmentId, [FromBody] UpdateScoresRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate score ranges
        if (request.Week7Score.HasValue && (request.Week7Score < 0 || request.Week7Score > 30))
            return BadRequest("Week 7 score must be between 0 and 30");

        if (request.Week12Score.HasValue && (request.Week12Score < 0 || request.Week12Score > 20))
            return BadRequest("Week 12 score must be between 0 and 20");

        if (request.PrefinalScore.HasValue && (request.PrefinalScore < 0 || request.PrefinalScore > 10))
            return BadRequest("Prefinal score must be between 0 and 10");

        if (request.FinalScore.HasValue && (request.FinalScore < 0 || request.FinalScore > 40))
            return BadRequest("Final score must be between 0 and 40");

        try
        {
            var keycloakId = GetCurrentKeycloakId();
            await _resultService.UpdateResultAsync(keycloakId, enrollmentId, request);
            return Ok(new { message = "Result updated successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("results/{enrollmentId:guid}/publish")]
    [Authorize(Roles = "instructor,admin")]
    public async Task<IActionResult> PublishResult(Guid enrollmentId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var role = User.IsInRole("admin") ? "admin" : "instructor";
            await _resultService.PublishResultAsync(keycloakId, role, enrollmentId);
            return Ok(new { message = "Result published successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sections/{sectionId:guid}/publish")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> PublishSectionResults(Guid sectionId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            await _resultService.PublishSectionResultsAsync(keycloakId, sectionId);
            return Ok(new { message = "Section results published successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sections/{sectionId:guid}/at-risk")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetAtRiskStudents(Guid sectionId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var atRisk = await _resultService.GetAtRiskStudentsAsync(keycloakId, sectionId, _prediction);
            return Ok(atRisk);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
