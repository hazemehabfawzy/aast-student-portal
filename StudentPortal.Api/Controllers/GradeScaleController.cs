using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/grade-scale")]
[Authorize(Policy = "AdminOnly")]
public class GradeScaleController : ControllerBase
{
    private readonly AppDbContext _context;

    public GradeScaleController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all grade scale entries (A+, A, B+, ..., F).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllScales()
    {
        var scales = await _context.GradeScales
            .OrderByDescending(g => g.MinPercent)
            .ToListAsync();
        return Ok(scales);
    }

    /// <summary>
    /// Updates the MinPercent threshold of a single grade scale entry.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateScale(Guid id, [FromBody] GradeScale model)
    {
        var scale = await _context.GradeScales.FindAsync(id);
        if (scale == null)
        {
            return NotFound(new { message = "Grade scale entry not found." });
        }

        scale.MinPercent = model.MinPercent;
        scale.MaxPercent = model.MaxPercent;
        scale.GpaPoints = model.GpaPoints;
        scale.CountsTowardGpa = model.CountsTowardGpa;

        _context.GradeScales.Update(scale);
        await _context.SaveChangesAsync();

        return Ok(scale);
    }
}
