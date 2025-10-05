
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Project_Version1.Services;
//using Project_Version1.DTOs;
//using Project_Version1.Data;
//using Project_Version1.Hubs;
//using Microsoft.AspNetCore.SignalR;

//using System;

//namespace Project_Version1.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class CommentsController : ControllerBase
//    {
//        private readonly CommentService _commentService;
//        private readonly FnfKnowledgeBaseContext _db;
//        private readonly IHubContext<NotificationHub> _hub;
//        public CommentsController(CommentService commentService, FnfKnowledgeBaseContext db, IHubContext<NotificationHub> hub)
//        {
//            _commentService = commentService;
//            _db = db;
//            _hub = hub;
//        }

//        [HttpGet("post/{postId}")]
//        public async Task<IActionResult> GetForPost(int postId, [FromQuery] bool hierarchical = false)
//        {
//            try
//            {
//                int? currentUserId = null;
//                if (User.Identity?.IsAuthenticated == true)
//                {
//                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
//                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
//                    {
//                        currentUserId = userId;
//                    }
//                }

//                if (hierarchical)
//                {
//                    var hierarchicalComments = await _commentService.GetCommentsHierarchyAsync(postId, currentUserId);
//                    return Ok(hierarchicalComments);
//                }
//                else
//                {
//                    var comments = await _commentService.GetCommentsForPostAsync(postId, currentUserId);
//                    return Ok(comments);
//                }
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [HttpGet("{commentId}")]
//        public async Task<IActionResult> GetComment(int commentId)
//        {
//            try
//            {
//                int? currentUserId = null;
//                if (User.Identity?.IsAuthenticated == true)
//                {
//                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
//                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
//                    {
//                        currentUserId = userId;
//                    }
//                }

//                var comment = await _commentService.GetCommentWithVotesAsync(commentId, currentUserId);
//                if (comment == null) return NotFound();

//                return Ok(comment);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [Authorize]
//        [HttpPost]
//        public async Task<IActionResult> Create([FromBody] CommentCreateDto dto)
//        {
//            try
//            {
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//                var comment = await _commentService.AddCommentAsync(dto, userId);
//                if (comment == null)
//                {
//                    return BadRequest(new { message = "Failed to create comment" });
//                }

//                var commentWithVotes = await _commentService.GetCommentWithVotesAsync(comment.CommentId, userId);
//                return CreatedAtAction(nameof(GetComment), new { commentId = comment.CommentId }, commentWithVotes);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//            catch (Exception)
//            {
//                return StatusCode(500, new { message = "An error occurred while creating the comment" });
//            }
//        }

//        // === MODIFIED DELETE: allow owner deletion; manager deletion with commit message ===
//        [Authorize]
//        [HttpDelete("{id}")]
//        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
//        {
//            try
//            {
//                var comment = await _commentService.GetCommentAsync(id);
//                if (comment == null) return NotFound();

//                var role = User.FindFirstValue(ClaimTypes.Role);
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//                if (role == "Manager")
//                {
//                    var manager = await _db.Managers.FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == comment.Post.DeptId);
//                    if (manager == null) return Forbid();

//                    if (!string.IsNullOrEmpty(commitMessage))
//                    {
//                        _db.Commits.Add(new Commit
//                        {
//                            PostId = comment.PostId,
//                            ManagerId = manager.ManagerId,
//                            Message = commitMessage,
//                            CreatedAt = DateTime.UtcNow
//                        });
//                        await _hub.Clients.User(comment.UserId.ToString())
//                           .SendAsync("ReceiveNotification", new
//                           {
//                               Type = "CommentDeletion",
//                               PostId = comment.PostId,
//                               CommentId = comment.CommentId,
//                               CommitMessage = commitMessage,
//                               Manager = manager.User.FullName,
//                               Timestamp = DateTime.UtcNow
//                           });
//                    }

//                    // Manager: force delete (includes replies if service handles that)
//                    await _commentService.ForceDeleteCommentAsync(comment);
//                    await _db.SaveChangesAsync();
//                    return NoContent();
//                }
//                else if (comment.UserId == userId)
//                {
//                    // Owner deletes own comment (soft or normal delete depending on service)
//                    await _commentService.DeleteCommentAsync(comment);
//                    //await _db.SaveChangesAsync();
//                    return NoContent();
//                }
//                else
//                {
//                    return Forbid();
//                }
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [Authorize]
//        [HttpPut("{id}")]
//        public async Task<IActionResult> Update(int id, [FromBody] CommentUpdateDto dto, [FromQuery] string? commitMessage = null)
//        {
//            try
//            {
//                var comment = await _commentService.GetCommentAsync(id);
//                if (comment == null) return NotFound();

//                var role = User.FindFirstValue(ClaimTypes.Role);
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//                if (role == "Manager")
//                {
//                    // Manager can update any comment in their dept
//                    var manager = await _db.Managers.FirstOrDefaultAsync(
//                        m => m.UserId == userId && m.DeptId == comment.Post.DeptId
//                    );
//                    if (manager == null) return Forbid();

//                    if (!string.IsNullOrEmpty(commitMessage))
//                    {
//                        _db.Commits.Add(new Commit
//                        {
//                            PostId = comment.PostId,
//                            ManagerId = manager.ManagerId,
//                            Message = commitMessage,
//                            CreatedAt = DateTime.UtcNow
//                        });
//                        await _hub.Clients.User(comment.UserId.ToString())
//                           .SendAsync("ReceiveNotification", new
//                           {
//                               Type = "CommentUpdate",
//                               PostId = comment.PostId,
//                               CommentId = comment.CommentId,
//                               CommitMessage = commitMessage,
//                               Manager = manager.User.FullName,
//                               Timestamp = DateTime.UtcNow
//                           });
//                    }
//                }
//                else if (comment.UserId != userId)
//                {
//                    // Normal users can only update their own comments
//                    return Forbid();
//                }

//                // Perform update
//                if (!string.IsNullOrEmpty(dto.CommentText))
//                {
//                    comment.CommentText = dto.CommentText;
//                    comment.UpdatedAt = DateTime.UtcNow;
//                }

//                await _db.SaveChangesAsync();
//                return Ok(new { message = "Comment updated successfully" });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }
//    }
//}

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Project_Version1.Services;
//using Project_Version1.DTOs;
//using Project_Version1.Data;
//using Project_Version1.Hubs;
//using Microsoft.AspNetCore.SignalR;
//using System;

//namespace Project_Version1.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class CommentsController : ControllerBase
//    {
//        private readonly CommentService _commentService;
//        private readonly FnfKnowledgeBaseContext _db;
//        private readonly IHubContext<NotificationHub> _hub;
//        public CommentsController(CommentService commentService, FnfKnowledgeBaseContext db, IHubContext<NotificationHub> hub)
//        {
//            _commentService = commentService;
//            _db = db;
//            _hub = hub;
//        }

//        [HttpGet("post/{postId}")]
//        public async Task<IActionResult> GetForPost(int postId, [FromQuery] bool hierarchical = false)
//        {
//            try
//            {
//                int? currentUserId = null;
//                if (User.Identity?.IsAuthenticated == true)
//                {
//                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
//                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
//                    {
//                        currentUserId = userId;
//                    }
//                }

//                if (hierarchical)
//                {
//                    var hierarchicalComments = await _commentService.GetCommentsHierarchyAsync(postId, currentUserId);
//                    return Ok(hierarchicalComments);
//                }
//                else
//                {
//                    var comments = await _commentService.GetCommentsForPostAsync(postId, currentUserId);
//                    return Ok(comments);
//                }
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [HttpGet("{commentId}")]
//        public async Task<IActionResult> GetComment(int commentId)
//        {
//            try
//            {
//                int? currentUserId = null;
//                if (User.Identity?.IsAuthenticated == true)
//                {
//                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
//                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
//                    {
//                        currentUserId = userId;
//                    }
//                }

//                var comment = await _commentService.GetCommentWithVotesAsync(commentId, currentUserId);
//                if (comment == null) return NotFound();

//                return Ok(comment);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [Authorize]
//        [HttpPost]
//        public async Task<IActionResult> Create([FromBody] CommentCreateDto dto)
//        {
//            try
//            {
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//                var comment = await _commentService.AddCommentAsync(dto, userId);
//                if (comment == null)
//                {
//                    return BadRequest(new { message = "Failed to create comment" });
//                }

//                var commentWithVotes = await _comment_service_get_with_votes_safe(comment.CommentId, userId);
//                return CreatedAtAction(nameof(GetComment), new { commentId = comment.CommentId }, commentWithVotes);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//            catch (Exception)
//            {
//                return StatusCode(500, new { message = "An error occurred while creating the comment" });
//            }
//        }

//        // small helper to avoid direct service field access naming mistake
//        private async Task<object?> _comment_service_get_with_votes_safe(int commentId, int userId)
//        {
//            return await _commentService.GetCommentWithVotesAsync(commentId, userId);
//        }

//        // === MODIFIED DELETE: allow owner deletion; manager deletion with commit message ===
//        [Authorize]
//        [HttpDelete("{id}")]
//        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
//        {
//            try
//            {
//                var comment = await _commentService.GetCommentAsync(id);
//                if (comment == null) return NotFound();

//                var role = User.FindFirstValue(ClaimTypes.Role);
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//                if (role == "Manager")
//                {
//                    // ensure manager belongs to post's dept
//                    var manager = await _db.Managers.FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == comment.Post.DeptId);
//                    if (manager == null) return Forbid();

//                    if (!string.IsNullOrEmpty(commitMessage))
//                    {
//                        _db.Commits.Add(new Commit
//                        {
//                            PostId = comment.PostId,
//                            ManagerId = manager.ManagerId,
//                            Message = commitMessage,
//                            CreatedAt = DateTime.UtcNow
//                        });
//                        await _hub.Clients.User(comment.UserId.ToString())
//                           .SendAsync("ReceiveNotification", new
//                           {
//                               Type = "CommentDeletion",
//                               PostId = comment.PostId,
//                               CommentId = comment.CommentId,
//                               CommitMessage = commitMessage,
//                               Manager = manager.User.FullName,
//                               Timestamp = DateTime.UtcNow
//                           });
//                    }

//                    // Manager: force delete (includes replies if service handles that)
//                    await _commentService.ForceDeleteCommentAsync(comment);
//                    await _db.SaveChangesAsync();
//                    return NoContent();
//                }
//                else if (comment.UserId == userId)
//                {
//                    // Owner deletes own comment (soft or normal delete depending on service)
//                    await _commentService.DeleteCommentAsync(comment);
//                    return NoContent();
//                }
//                else
//                {
//                    return Forbid();
//                }
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }

//        [Authorize]
//        [HttpPut("{id}")]
//        public async Task<IActionResult> Update(int id, [FromBody] CommentUpdateDto dto, [FromQuery] string? commitMessage = null)
//        {
//            try
//            {
//                var comment = await _commentService.GetCommentAsync(id);
//                if (comment == null) return NotFound();

//                var role = User.FindFirstValue(ClaimTypes.Role);
//                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//                if (role == "Manager")
//                {
//                    // Manager can update any comment in their dept
//                    var manager = await _db.Managers.FirstOrDefaultAsync(
//                        m => m.UserId == userId && m.DeptId == comment.Post.DeptId
//                    );
//                    if (manager == null) return Forbid();

//                    if (!string.IsNullOrEmpty(commitMessage))
//                    {
//                        _db.Commits.Add(new Commit
//                        {
//                            PostId = comment.PostId,
//                            ManagerId = manager.ManagerId,
//                            Message = commitMessage,
//                            CreatedAt = DateTime.UtcNow
//                        });
//                        await _hub.Clients.User(comment.UserId.ToString())
//                           .SendAsync("ReceiveNotification", new
//                           {
//                               Type = "CommentUpdate",
//                               PostId = comment.PostId,
//                               CommentId = comment.CommentId,
//                               CommitMessage = commitMessage,
//                               Manager = manager.User.FullName,
//                               Timestamp = DateTime.UtcNow
//                           });
//                    }
//                }
//                else if (comment.UserId != userId)
//                {
//                    // Normal users can only update their own comments
//                    return Forbid();
//                }

//                // Perform update
//                if (!string.IsNullOrEmpty(dto.CommentText))
//                {
//                    comment.CommentText = dto.CommentText;
//                    comment.UpdatedAt = DateTime.UtcNow;
//                }

//                await _db.SaveChangesAsync();
//                return Ok(new { message = "Comment updated successfully" });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//        }
//    }
//}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Services;
using Project_Version1.DTOs;
using Project_Version1.Data;
using Project_Version1.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly CommentService _commentService;
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IHubContext<NotificationHub> _hub;
        public CommentsController(CommentService commentService, FnfKnowledgeBaseContext db, IHubContext<NotificationHub> hub)
        {
            _commentService = commentService;
            _db = db;
            _hub = hub;
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetForPost(int postId, [FromQuery] bool hierarchical = false)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                    {
                        currentUserId = userId;
                    }
                }

                if (hierarchical)
                {
                    var hierarchicalComments = await _commentService.GetCommentsHierarchyAsync(postId, currentUserId);
                    return Ok(hierarchicalComments);
                }
                else
                {
                    var comments = await _commentService.GetCommentsForPostAsync(postId, currentUserId);
                    return Ok(comments);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{commentId}")]
        public async Task<IActionResult> GetComment(int commentId)
        {
            try
            {
                int? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                    {
                        currentUserId = userId;
                    }
                }

                var comment = await _commentService.GetCommentWithVotesAsync(commentId, currentUserId);
                if (comment == null) return NotFound();

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var comment = await _commentService.AddCommentAsync(dto, userId);
                if (comment == null)
                {
                    return BadRequest(new { message = "Failed to create comment" });
                }

                var commentWithVotes = await _comment_service_get_with_votes_safe(comment.CommentId, userId);
                return CreatedAtAction(nameof(GetComment), new { commentId = comment.CommentId }, commentWithVotes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while creating the comment" });
            }
        }

        // small helper to avoid direct service field access naming mistake
        private async Task<object?> _comment_service_get_with_votes_safe(int commentId, int userId)
        {
            return await _commentService.GetCommentWithVotesAsync(commentId, userId);
        }

        // === MODIFIED DELETE: allow owner deletion; manager deletion with commit message ===
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
        {
            try
            {
                // Safely load comment (may not include Post)
                var comment = await _comment_service_get_with_post_safe(id);
                if (comment == null) return NotFound();

                // Ensure we have the Post object (defensive)
                var post = comment.Post;
                if (post == null)
                {
                    post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == comment.PostId);
                    if (post == null)
                    {
                        return BadRequest(new { message = "The post for this comment does not exist." });
                    }
                }

                var role = User.FindFirstValue(ClaimTypes.Role);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (role == "Manager")
                {
                    // ensure manager belongs to post's dept; include User so FullName is available
                    var manager = await _db.Managers
                        .Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);
                    if (manager == null) return Forbid();

                    if (!string.IsNullOrEmpty(commitMessage))
                    {
                        _db.Commits.Add(new Commit
                        {
                            PostId = comment.PostId,
                            ManagerId = manager.ManagerId,
                            Message = commitMessage,
                            CreatedAt = DateTime.UtcNow
                        });
                        var managerName = manager.User?.FullName ?? "Manager";
                        await _hub.Clients.User(comment.UserId.ToString())
                           .SendAsync("ReceiveNotification", new
                           {
                               Type = "CommentDeletion",
                               PostId = comment.PostId,
                               CommentId = comment.CommentId,
                               CommitMessage = commitMessage,
                               Manager = managerName,
                               Timestamp = DateTime.UtcNow
                           });
                    }

                    // Manager: force delete (includes replies if service handles that)
                    await _commentService.ForceDeleteCommentAsync(comment);
                    await _db.SaveChangesAsync();
                    return NoContent();
                }
                else if (comment.UserId == userId)
                {
                    // Owner deletes own comment (soft or normal delete depending on service)
                    await _commentService.DeleteCommentAsync(comment);
                    return NoContent();
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CommentUpdateDto dto, [FromQuery] string? commitMessage = null)
        {
            try
            {
                // Safely load comment (may not include Post)
                var comment = await _comment_service_get_with_post_safe(id);
                if (comment == null) return NotFound();

                // Ensure we have the Post object (defensive)
                var post = comment.Post;
                if (post == null)
                {
                    post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == comment.PostId);
                    if (post == null)
                    {
                        return BadRequest(new { message = "The post for this comment does not exist." });
                    }
                }

                var role = User.FindFirstValue(ClaimTypes.Role);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (role == "Manager")
                {
                    // Manager can update any comment in their dept; include User for notification
                    var manager = await _db.Managers
                        .Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);
                    if (manager == null) return Forbid();

                    if (!string.IsNullOrEmpty(commitMessage))
                    {
                        _db.Commits.Add(new Commit
                        {
                            PostId = comment.PostId,
                            ManagerId = manager.ManagerId,
                            Message = commitMessage,
                            CreatedAt = DateTime.UtcNow
                        });
                        var managerName = manager.User?.FullName ?? "Manager";
                        await _hub.Clients.User(comment.UserId.ToString())
                           .SendAsync("ReceiveNotification", new
                           {
                               Type = "CommentUpdate",
                               PostId = comment.PostId,
                               CommentId = comment.CommentId,
                               CommitMessage = commitMessage,
                               Manager = managerName,
                               Timestamp = DateTime.UtcNow
                           });
                    }
                }
                else if (comment.UserId != userId)
                {
                    // Normal users can only update their own comments
                    return Forbid();
                }

                // Perform update
                if (!string.IsNullOrEmpty(dto.CommentText))
                {
                    comment.CommentText = dto.CommentText;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                return Ok(new { message = "Comment updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Helper to safely get comment with Post included if possible
        private async Task<Comment?> _comment_service_get_with_post_safe(int commentId)
        {
            // Try service first (keeps existing service behavior)
            var comment = await _commentService.GetCommentAsync(commentId);
            if (comment != null && comment.Post != null) return comment;

            // If Post was not included by the service, explicitly load comment with Post and User
            comment = await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            return comment;
        }
    }
}
