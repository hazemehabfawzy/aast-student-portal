using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/grading-policy")]
[Authorize(Policy = "AdminOnly")]
public class GradingPolicyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGradingService _gradingService;

    public GradingPolicyController(AppDbContext context, IGradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicies()
    {
        var policies = await _context.GradingPolicies
            .Include(gp => gp.Course)
            .ToListAsync();
        return Ok(policies);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromBody] GradingPolicy model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var policy = await _context.GradingPolicies.FindAsync(id);
        if (policy == null)
        {
            return NotFound(new { message = "Grading policy not found." });
        }

        // Validate weights using GradingService
        var (isValid, reason) = _gradingService.ValidatePolicyWeights(
            model.Week7Weight, 
            model.Week12Weight, 
            model.PrefinalWeight, 
            model.FinalWeight
        );

        if (!isValid)
        {
            return BadRequest(new { message = reason ?? "Policy weights must sum to 1.0 (with 0.001 tolerance)." });
        }

        policy.Week7Weight = model.Week7Weight;
        policy.Week12Weight = model.Week12Weight;
        policy.PrefinalWeight = model.PrefinalWeight;
        policy.FinalWeight = model.FinalWeight;
        
        // CourseId cannot be modified for default policy (null), but can be set/modified for other policies if applicable
        if (policy.CourseId != null)
        {
            policy.CourseId = model.CourseId;
        }

        _context.GradingPolicies.Update(policy);
        await _context.SaveChangesAsync();

        return Ok(policy);
    }
}
