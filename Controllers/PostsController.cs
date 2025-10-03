using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Services;
using Project_Version1.DTOs;
using Project_Version1.Data;
using AutoMapper;
using System;
using Microsoft.AspNetCore.SignalR;
using Project_Version1.Hubs;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly PostService _postService;
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hub;

        public PostsController(PostService postService, FnfKnowledgeBaseContext db, IMapper mapper, IHubContext<NotificationHub> hub)
        {
            _postService = postService;
            _db = db;
            _mapper = mapper;
            _hub = hub;
        }

        [HttpGet("feed")]
        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var post = await _postService.GetPostAsync(id);
            if (post == null) return NotFound();

            var postDto = _mapper.Map<PostDetailDto>(post);
            return Ok(postDto);
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var posts = await _postService.GetPostsByUserAsync(userId);
            return Ok(posts);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] PostCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (dto.DeptId == 0)
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null || user.DepartmentId == 0) return BadRequest("User department information is missing.");
                    dto.DeptId = user.DepartmentId;
                }

                var post = await _postService.CreatePostAsync(dto, userId, dto.DeptId);
                if (post == null) return BadRequest(new { message = "Failed to create post" });

                return CreatedAtAction(nameof(Get), new { id = post.PostId }, post);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Server error while saving post", detail = inner });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (role == "Manager")
            {
                var manager = await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId)
                    ?? await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId);

                if (manager == null) return Forbid();

                if (!string.IsNullOrEmpty(commitMessage))
                {
                    _db.Commits.Add(new Commit
                    {
                        PostId = id,
                        ManagerId = manager.ManagerId,
                        Message = commitMessage,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (post.UserId != null)
                    {
                        await _hub.Clients.User(post.UserId.ToString()!)
                            .SendAsync("ReceiveNotification", new
                            {
                                Type = "PostUpdate",
                                PostId = id,
                                CommitMessage = commitMessage,
                                Manager = manager.User.FullName,
                                Timestamp = DateTime.UtcNow
                            });
                    }
                }
            }
            else if (post.UserId != userId)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(dto.Title)) post.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Body)) post.Body = dto.Body;

            post.UpdatedAt = DateTime.UtcNow;

            await _postService.UpdatePostAsync(post);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Post updated successfully" });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (role != "Manager" && post.UserId != userId)
                return Forbid("Only managers can delete the posts");

            if (role == "Manager")
            {
                var manager = await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId)
                    ?? await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId);

                if (manager == null) return Forbid();

                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    if (!string.IsNullOrEmpty(commitMessage))
                    {
                        // ✅ Insert commit via raw SQL so PostId never goes null
                        await _db.Database.ExecuteSqlInterpolatedAsync(
                            $"INSERT INTO Commits (PostId, ManagerId, Message, CreatedAt) VALUES ({post.PostId}, {manager.ManagerId}, {commitMessage}, {DateTime.UtcNow})"
                        );

                        if (post.UserId != null)
                        {
                            await _hub.Clients.User(post.UserId.ToString()!)
                                .SendAsync("ReceiveNotification", new
                                {
                                    Type = "PostDeletion",
                                    PostId = post.PostId,
                                    CommitMessage = commitMessage,
                                    Manager = manager.User.FullName,
                                    Timestamp = DateTime.UtcNow
                                });
                        }
                    }

                    await _postService.DeletePostAsync(post);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch (Exception ex)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    Console.Error.WriteLine(ex.ToString());
                    var inner = ex.InnerException?.Message ?? ex.Message;
                    return BadRequest(new { message = "Failed to delete post", detail = inner });
                }
            }
            else if (post.UserId == userId)
            {
                await _postService.DeletePostAsync(post);
                await _db.SaveChangesAsync();
                return NoContent();
            }

            return Forbid();
        }

        [Authorize]
        [HttpPost("{id}/repost")]
        public async Task<IActionResult> Repost(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _postService.RepostAsync(id, userId);
            return Ok();
        }
    }
}
