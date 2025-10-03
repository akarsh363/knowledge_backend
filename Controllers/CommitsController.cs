
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Data;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CommitsController : ControllerBase
{
    private readonly FnfKnowledgeBaseContext _db;
    public CommitsController(FnfKnowledgeBaseContext db) => _db = db;

    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetForPost(int postId)
    {
        var commits = await _db.Commits
            .Where(c => c.PostId == postId)
            .Include(c => c.Manager).ThenInclude(m => m.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(commits);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetForManager()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var commits = await _db.Commits
            .Where(c => c.Manager.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(commits);
    }
}