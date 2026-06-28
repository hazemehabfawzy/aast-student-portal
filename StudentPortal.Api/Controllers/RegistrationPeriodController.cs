using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/registration-periods")]
[Authorize(Policy = "AdminOnly")]
public class RegistrationPeriodController : ControllerBase
{
    private readonly AppDbContext _context;

    public RegistrationPeriodController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRegistrationPeriods()
    {
        var periods = await _context.RegistrationPeriods
            .Include(rp => rp.Semester)
            .ToListAsync();
        return Ok(periods);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRegistrationPeriod([FromBody] RegistrationPeriod model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate Semester exists
        var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == model.SemesterId);
        if (!semesterExists)
        {
            return BadRequest(new { message = "Semester does not exist." });
        }

        model.Id = Guid.NewGuid();

        await _context.RegistrationPeriods.AddAsync(model);
        await _context.SaveChangesAsync();

        return Created($"/api/registration-periods/{model.Id}", model);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRegistrationPeriod(Guid id, [FromBody] RegistrationPeriod model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var period = await _context.RegistrationPeriods.FindAsync(id);
        if (period == null)
        {
            return NotFound(new { message = "Registration period not found." });
        }

        var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == model.SemesterId);
        if (!semesterExists)
        {
            return BadRequest(new { message = "Semester does not exist." });
        }

        period.SemesterId = model.SemesterId;
        period.StartDate = model.StartDate;
        period.EndDate = model.EndDate;
        period.IsOpen = model.IsOpen;

        _context.RegistrationPeriods.Update(period);
        await _context.SaveChangesAsync();

        return Ok(period);
    }
}
