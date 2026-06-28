using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Services.Implementations;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    // Moved to SectionController to avoid route conflict on GET /api/sections
    // [HttpGet("sections")]
    // [Authorize(Policy = "StudentOnly")]
    // public async Task<IActionResult> GetSections([FromQuery] Guid semesterId)
    // {
    //     try
    //     {
    //         var keycloakId = GetCurrentKeycloakId();
    //         var response = await _registrationService.GetAvailableSectionsAsync(keycloakId, semesterId);
    //         return Ok(response);
    //     }
    //     catch (UnauthorizedAccessException ex)
    //     {
    //         return Forbid(ex.Message);
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(new { message = ex.Message });
    //     }
    // }

    [HttpPost("enrollments")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _registrationService.EnrollAsync(keycloakId, request.SectionId);
            return Created(string.Empty, response);
        }
        catch (ScheduleConflictException ex)
        {
            return Conflict(ex.ConflictDetails);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("enrollments/{id:guid}")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> DropEnrollment(Guid id)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            await _registrationService.DropEnrollmentAsync(keycloakId, id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // closed registration period returns 403
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("students/me/schedule")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMySchedule()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _registrationService.GetStudentScheduleAsync(keycloakId);
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
}
