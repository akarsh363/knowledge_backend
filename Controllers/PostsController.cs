////////using Microsoft.AspNetCore.Authorization;
////////using Microsoft.AspNetCore.Mvc;
////////using System.Security.Claims;
////////using Microsoft.EntityFrameworkCore;
////////using Project_Version1.Services;
////////using Project_Version1.DTOs;
////////using Project_Version1.Data;
////////using AutoMapper;
////////using System;
////////using Microsoft.AspNetCore.SignalR;
////////using Project_Version1.Hubs;
////////using System.Linq;
////////using System.Collections.Generic;
////////using System.Threading.Tasks;

////////namespace Project_Version1.Controllers
////////{
////////    [ApiController]
////////    [Route("api/[controller]")]
////////    public class PostsController : ControllerBase
////////    {
////////        private readonly PostService _postService;
////////        private readonly FnfKnowledgeBaseContext _db;
////////        private readonly IMapper _mapper;
////////        private readonly IHubContext<NotificationHub> _hub;

////////        public PostsController(PostService postService, FnfKnowledgeBaseContext db, IMapper mapper, IHubContext<NotificationHub> hub)
////////        {
////////            _postService = postService;
////////            _db = db;
////////            _mapper = mapper;
////////            _hub = hub;
////////        }

////////        [HttpGet("feed")]
////////        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
////////        {
////////            var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);
////////            return Ok(posts);
////////        }

////////        // Defensive GET action with manual mapping (avoids AutoMapper runtime issues)
////////        [HttpGet("{id}")]
////////        public async Task<IActionResult> Get(int id)
////////        {
////////            try
////////            {
////////                // load the post with related data required for the DTO
////////                var post = await _db.Posts
////////                    .AsNoTracking()
////////                    .Include(p => p.User)
////////                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
////////                    .Include(p => p.Comments).ThenInclude(c => c.User)
////////                    .Include(p => p.Attachments)
////////                    .Include(p => p.Reposts).ThenInclude(r => r.User)
////////                    .Include(p => p.Votes)
////////                    .FirstOrDefaultAsync(p => p.PostId == id);

////////                if (post == null)
////////                    return NotFound();

////////                // Prefetch department name safely (avoid await inside initializer)
////////                string? deptName = null;
////////                if (post.Dept != null)
////////                {
////////                    deptName = post.Dept.DeptName;
////////                }
////////                else if (post.DeptId != 0)
////////                {
////////                    var dept = await _db.Departments.FindAsync(post.DeptId);
////////                    deptName = dept?.DeptName;
////////                }

////////                // safe helpers for counts
////////                var upvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase));
////////                var downvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////////                var commentsCount = post.Comments?.Count ?? 0;

////////                // Manual, defensive mapping into PostDetailDto
////////                var dto = new PostDetailDto
////////                {
////////                    PostId = post.PostId,
////////                    Title = post.Title ?? string.Empty,
////////                    Body = post.Body ?? string.Empty,
////////                    UpvoteCount = upvoteCount,
////////                    DownvoteCount = downvoteCount,
////////                    CommentsCount = commentsCount,
////////                    CreatedAt = post.CreatedAt,
////////                    AuthorName = post.User?.FullName,
////////                    DepartmentName = deptName,
////////                    Tags = post.PostTags?.Select(pt => pt.Tag?.TagName).Where(tn => !string.IsNullOrEmpty(tn)).ToList() ?? new List<string>(),
////////                    Attachments = post.Attachments?.Select(a => new AttachmentDto
////////                    {
////////                        AttachmentId = a.AttachmentId,
////////                        FileName = a.FileName,
////////                        FilePath = a.FilePath,
////////                        FileType = a.FileType
////////                    }).ToList() ?? new List<AttachmentDto>(),
////////                    IsRepost = false,
////////                    RepostedBy = null,
////////                    Reposts = post.Reposts?.OrderByDescending(r => r.CreatedAt).Select(r => new RepostBriefDto
////////                    {
////////                        RepostId = r.RepostId,
////////                        UserId = r.UserId,
////////                        UserName = r.User?.FullName,
////////                        CreatedAt = r.CreatedAt
////////                    }).ToList()
////////                };

////////                return Ok(dto);
////////            }
////////            catch (Exception ex)
////////            {
////////                // Log full exception server-side for debugging
////////                Console.Error.WriteLine("Error in PostsController.Get:");
////////                Console.Error.WriteLine(ex.ToString());

////////                // Return helpful detail (useful in development) but keep shape stable
////////                var inner = ex.InnerException?.Message ?? ex.Message;
////////                return StatusCode(500, new { message = "Server error while loading post", detail = inner });
////////            }
////////        }

////////        [Authorize]
////////        [HttpGet("mine")]
////////        public async Task<IActionResult> GetMyPosts()
////////        {
////////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////////            // get entity posts (service already includes related data)
////////            var posts = await _postService.GetPostsByUserAsync(userId);

////////            // Map to PostBriefDto shape (same as feed mapping)
////////            var result = posts.Select(p =>
////////            {
////////                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////////                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////////                var commentsCount = p.Comments?.Count ?? 0;

////////                List<RepostBriefDto>? reposts = null;
////////                if (p.Reposts != null && p.Reposts.Any())
////////                {
////////                    reposts = p.Reposts
////////                        .OrderByDescending(r => r.CreatedAt)
////////                        .Select(r => new RepostBriefDto
////////                        {
////////                            RepostId = r.RepostId,
////////                            UserId = r.UserId,
////////                            UserName = r.User?.FullName,
////////                            CreatedAt = r.CreatedAt
////////                        })
////////                        .ToList();
////////                }

////////                return new PostBriefDto
////////                {
////////                    PostId = p.PostId,
////////                    Title = p.Title ?? string.Empty,
////////                    BodyPreview = p.Body ?? string.Empty,
////////                    UpvoteCount = upvoteCount,
////////                    DownvoteCount = downvoteCount,
////////                    CommentsCount = commentsCount,
////////                    CreatedAt = p.CreatedAt,
////////                    AuthorName = p.User?.FullName,
////////                    IsRepost = false,
////////                    RepostedBy = null,
////////                    Reposts = reposts
////////                };
////////            }).ToList();

////////            return Ok(result);
////////        }



////////        [Authorize]
////////        [HttpPut("{id}")]
////////        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
////////        {
////////            var post = await _db.Posts.FindAsync(id);
////////            if (post == null) return NotFound();

////////            var role = User.FindFirstValue(ClaimTypes.Role);
////////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////////            if (role == "Manager")
////////            {
////////                var manager = await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId)
////////                    ?? await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId);

////////                if (manager == null) return Forbid();

////////                if (!string.IsNullOrEmpty(commitMessage))
////////                {
////////                    _db.Commits.Add(new Commit
////////                    {
////////                        PostId = id,
////////                        ManagerId = manager.ManagerId,
////////                        Message = commitMessage,
////////                        CreatedAt = DateTime.UtcNow
////////                    });

////////                    if (post.UserId != null)
////////                    {
////////                        await _hub.Clients.User(post.UserId.ToString()!)
////////                            .SendAsync("ReceiveNotification", new
////////                            {
////////                                Type = "PostUpdate",
////////                                PostId = id,
////////                                CommitMessage = commitMessage,
////////                                Manager = manager.User.FullName,
////////                                Timestamp = DateTime.UtcNow
////////                            });
////////                    }
////////                }
////////            }
////////            else if (post.UserId != userId)
////////            {
////////                return Forbid();
////////            }

////////            if (!string.IsNullOrEmpty(dto.Title)) post.Title = dto.Title;
////////            if (!string.IsNullOrEmpty(dto.Body)) post.Body = dto.Body;

////////            post.UpdatedAt = DateTime.UtcNow;

////////            await _postService.UpdatePostAsync(post);
////////            await _db.SaveChangesAsync();

////////            return Ok(new { message = "Post updated successfully" });
////////        }

////////        [Authorize]
////////        [HttpDelete("{id}")]
////////        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
////////        {
////////            var post = await _db.Posts.FindAsync(id);
////////            if (post == null) return NotFound();

////////            var role = User.FindFirstValue(ClaimTypes.Role);
////////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////////            if (role != "Manager" && post.UserId != userId)
////////                return Forbid("Only managers can delete the posts");

////////            if (role == "Manager")
////////            {
////////                var manager = await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId)
////////                    ?? await _db.Managers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId);

////////                if (manager == null) return Forbid();

////////                using var transaction = await _db.Database.BeginTransactionAsync();
////////                try
////////                {
////////                    if (!string.IsNullOrEmpty(commitMessage))
////////                    {
////////                        // ✅ Insert commit via raw SQL so PostId never goes null
////////                        await _db.Database.ExecuteSqlInterpolatedAsync(
////////                            $"INSERT INTO Commits (PostId, ManagerId, Message, CreatedAt) VALUES ({post.PostId}, {manager.ManagerId}, {commitMessage}, {DateTime.UtcNow})"
////////                        );

////////                        if (post.UserId != null)
////////                        {
////////                            await _hub.Clients.User(post.UserId.ToString()!)
////////                                .SendAsync("ReceiveNotification", new
////////                                {
////////                                    Type = "PostDeletion",
////////                                    PostId = post.PostId,
////////                                    CommitMessage = commitMessage,
////////                                    Manager = manager.User.FullName,
////////                                    Timestamp = DateTime.UtcNow
////////                                });
////////                        }
////////                    }

////////                    await _postService.DeletePostAsync(post);
////////                    await _db.SaveChangesAsync();

////////                    await transaction.CommitAsync();
////////                    return NoContent();
////////                }
////////                catch (Exception ex)
////////                {
////////                    try { await transaction.RollbackAsync(); } catch { }
////////                    Console.Error.WriteLine(ex.ToString());
////////                    var inner = ex.InnerException?.Message ?? ex.Message;
////////                    return BadRequest(new { message = "Failed to delete post", detail = inner });
////////                }
////////            }
////////            else if (post.UserId == userId)
////////            {
////////                await _postService.DeletePostAsync(post);
////////                await _db.SaveChangesAsync();
////////                return NoContent();
////////            }

////////            return Forbid();
////////        }

////////        [Authorize]
////////        [HttpPost("{id}/repost")]
////////        public async Task<IActionResult> Repost(int id)
////////        {
////////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
////////            await _postService.RepostAsync(id, userId);
////////            return Ok();
////////        }




////////        [Authorize]
////////        [HttpPost]
////////        public async Task<IActionResult> Create([FromForm] PostCreateDto dto)
////////        {
////////            try
////////            {
////////                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////////                // If DeptId is missing, infer from user profile
////////                if (dto.DeptId == 0)
////////                {
////////                    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
////////                    if (user == null || user.DepartmentId == 0)
////////                    {
////////                        return BadRequest(new { message = "User department information is missing." });
////////                    }
////////                    dto.DeptId = user.DepartmentId;
////////                }

////////                var post = await _postService.CreatePostAsync(dto, userId, dto.DeptId);
////////                if (post == null)
////////                    return BadRequest(new { message = "Failed to create post" });

////////                // Return 201 Created with link to GET /api/Posts/{id}
////////                return CreatedAtAction(nameof(Get), new { id = post.PostId }, new { postId = post.PostId });
////////            }
////////            catch (Exception ex)
////////            {
////////                Console.Error.WriteLine("Error in PostsController.Create:");
////////                Console.Error.WriteLine(ex.ToString());
////////                var inner = ex.InnerException?.Message ?? ex.Message;
////////                return BadRequest(new { message = "Server error while saving post", detail = inner });
////////            }
////////        }

////////    }
////////}































//////using Microsoft.AspNetCore.Authorization;
//////using Microsoft.AspNetCore.Mvc;
//////using System.Security.Claims;
//////using Microsoft.EntityFrameworkCore;
//////using Project_Version1.Services;
//////using Project_Version1.DTOs;
//////using Project_Version1.Data;
//////using AutoMapper;
//////using System;
//////using Microsoft.AspNetCore.SignalR;
//////using Project_Version1.Hubs;
//////using System.Linq;
//////using System.Collections.Generic;
//////using System.Threading.Tasks;

//////namespace Project_Version1.Controllers
//////{
//////    [ApiController]
//////    [Route("api/[controller]")]
//////    public class PostsController : ControllerBase
//////    {
//////        private readonly PostService _postService;
//////        private readonly FnfKnowledgeBaseContext _db;
//////        private readonly IMapper _mapper;
//////        private readonly IHubContext<NotificationHub> _hub;

//////        public PostsController(PostService postService, FnfKnowledgeBaseContext db, IMapper mapper, IHubContext<NotificationHub> hub)
//////        {
//////            _postService = postService;
//////            _db = db;
//////            _mapper = mapper;
//////            _hub = hub;
//////        }

//////        [HttpGet("feed")]
//////        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//////        {
//////            var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);
//////            return Ok(posts);
//////        }

//////        [HttpGet("{id}")]
//////        public async Task<IActionResult> Get(int id)
//////        {
//////            try
//////            {
//////                var post = await _db.Posts
//////                    .AsNoTracking()
//////                    .Include(p => p.User)
//////                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
//////                    .Include(p => p.Comments).ThenInclude(c => c.User)
//////                    .Include(p => p.Attachments)
//////                    .Include(p => p.Reposts).ThenInclude(r => r.User)
//////                    .Include(p => p.Votes)
//////                    .FirstOrDefaultAsync(p => p.PostId == id);

//////                if (post == null)
//////                    return NotFound();

//////                string? deptName = null;
//////                if (post.Dept != null)
//////                {
//////                    deptName = post.Dept.DeptName;
//////                }
//////                else if (post.DeptId != 0)
//////                {
//////                    var dept = await _db.Departments.FindAsync(post.DeptId);
//////                    deptName = dept?.DeptName;
//////                }

//////                var upvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase));
//////                var downvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//////                var commentsCount = post.Comments?.Count ?? 0;

//////                var dto = new PostDetailDto
//////                {
//////                    PostId = post.PostId,
//////                    Title = post.Title ?? string.Empty,
//////                    Body = post.Body ?? string.Empty,
//////                    UpvoteCount = upvoteCount,
//////                    DownvoteCount = downvoteCount,
//////                    CommentsCount = commentsCount,
//////                    CreatedAt = post.CreatedAt,
//////                    AuthorName = post.User?.FullName,
//////                    DepartmentName = deptName,
//////                    Tags = post.PostTags?.Select(pt => pt.Tag?.TagName).Where(tn => !string.IsNullOrEmpty(tn)).ToList() ?? new List<string>(),
//////                    Attachments = post.Attachments?.Select(a => new AttachmentDto
//////                    {
//////                        AttachmentId = a.AttachmentId,
//////                        FileName = a.FileName,
//////                        FilePath = a.FilePath,
//////                        FileType = a.FileType
//////                    }).ToList() ?? new List<AttachmentDto>(),
//////                    IsRepost = false,
//////                    RepostedBy = null,
//////                    Reposts = post.Reposts?.OrderByDescending(r => r.CreatedAt).Select(r => new RepostBriefDto
//////                    {
//////                        RepostId = r.RepostId,
//////                        UserId = r.UserId,
//////                        UserName = r.User?.FullName,
//////                        CreatedAt = r.CreatedAt
//////                    }).ToList()
//////                };

//////                return Ok(dto);
//////            }
//////            catch (Exception ex)
//////            {
//////                Console.Error.WriteLine("Error in PostsController.Get:");
//////                Console.Error.WriteLine(ex.ToString());
//////                var inner = ex.InnerException?.Message ?? ex.Message;
//////                return StatusCode(500, new { message = "Server error while loading post", detail = inner });
//////            }
//////        }

//////        [Authorize]
//////        [HttpGet("mine")]
//////        public async Task<IActionResult> GetMyPosts()
//////        {
//////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//////            var posts = await _postService.GetPostsByUserAsync(userId);

//////            var result = posts.Select(p =>
//////            {
//////                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//////                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//////                var commentsCount = p.Comments?.Count ?? 0;

//////                List<RepostBriefDto>? reposts = null;
//////                if (p.Reposts != null && p.Reposts.Any())
//////                {
//////                    reposts = p.Reposts
//////                        .OrderByDescending(r => r.CreatedAt)
//////                        .Select(r => new RepostBriefDto
//////                        {
//////                            RepostId = r.RepostId,
//////                            UserId = r.UserId,
//////                            UserName = r.User?.FullName,
//////                            CreatedAt = r.CreatedAt
//////                        })
//////                        .ToList();
//////                }

//////                return new PostBriefDto
//////                {
//////                    PostId = p.PostId,
//////                    Title = p.Title ?? string.Empty,
//////                    BodyPreview = p.Body ?? string.Empty,
//////                    UpvoteCount = upvoteCount,
//////                    DownvoteCount = downvoteCount,
//////                    CommentsCount = commentsCount,
//////                    CreatedAt = p.CreatedAt,
//////                    AuthorName = p.User?.FullName,
//////                    IsRepost = false,
//////                    RepostedBy = null,
//////                    Reposts = reposts
//////                };
//////            }).ToList();

//////            return Ok(result);
//////        }

//////        [Authorize]
//////        [HttpPut("{id}")]
//////        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
//////        {
//////            var post = await _db.Posts.FindAsync(id);
//////            if (post == null) return NotFound();

//////            var role = User.FindFirstValue(ClaimTypes.Role);
//////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//////            // Owner can edit
//////            if (post.UserId == userId)
//////            {
//////                // owner editing: allow; commit messages only relevant for managers
//////            }
//////            else if (role == "Manager")
//////            {
//////                // Manager may edit posts in their own department only
//////                var manager = await _db.Managers.Include(m => m.User)
//////                                                .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

//////                if (manager == null)
//////                    return Forbid();

//////                // record commit if provided
//////                if (!string.IsNullOrEmpty(commitMessage))
//////                {
//////                    _db.Commits.Add(new Commit
//////                    {
//////                        PostId = id,
//////                        ManagerId = manager.ManagerId,
//////                        Message = commitMessage,
//////                        CreatedAt = DateTime.UtcNow
//////                    });

//////                    if (post.UserId != null)
//////                    {
//////                        await _hub.Clients.User(post.UserId.ToString()!)
//////                            .SendAsync("ReceiveNotification", new
//////                            {
//////                                Type = "PostUpdate",
//////                                PostId = id,
//////                                CommitMessage = commitMessage,
//////                                Manager = manager.User.FullName,
//////                                Timestamp = DateTime.UtcNow
//////                            });
//////                    }
//////                }
//////            }
//////            else
//////            {
//////                // not owner and not a manager of the post's dept
//////                return Forbid();
//////            }

//////            if (!string.IsNullOrEmpty(dto.Title)) post.Title = dto.Title;
//////            if (!string.IsNullOrEmpty(dto.Body)) post.Body = dto.Body;

//////            post.UpdatedAt = DateTime.UtcNow;

//////            await _postService.UpdatePostAsync(post);
//////            await _db.SaveChangesAsync();

//////            return Ok(new { message = "Post updated successfully" });
//////        }

//////        [Authorize]
//////        [HttpDelete("{id}")]
//////        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
//////        {
//////            var post = await _db.Posts.FindAsync(id);
//////            if (post == null) return NotFound();

//////            var role = User.FindFirstValue(ClaimTypes.Role);
//////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//////            // Only managers can delete posts (employees are not allowed to delete their own posts)
//////            if (role != "Manager")
//////            {
//////                return Forbid("Only managers can delete posts");
//////            }

//////            // Manager must have DeptId matching the post's DeptId
//////            var manager = await _db.Managers.Include(m => m.User)
//////                                            .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

//////            if (manager == null)
//////            {
//////                // Manager doesn't belong to the post's department -> forbid
//////                return Forbid();
//////            }

//////            // Manager is authorized to delete this post (their own posts or employees in their dept)
//////            using var transaction = await _db.Database.BeginTransactionAsync();
//////            try
//////            {
//////                if (!string.IsNullOrEmpty(commitMessage))
//////                {
//////                    // Insert commit (raw SQL used previously; keep similar approach)
//////                    await _db.Database.ExecuteSqlInterpolatedAsync(
//////                        $"INSERT INTO Commits (PostId, ManagerId, Message, CreatedAt) VALUES ({post.PostId}, {manager.ManagerId}, {commitMessage}, {DateTime.UtcNow})"
//////                    );

//////                    if (post.UserId != null)
//////                    {
//////                        await _hub.Clients.User(post.UserId.ToString()!)
//////                            .SendAsync("ReceiveNotification", new
//////                            {
//////                                Type = "PostDeletion",
//////                                PostId = post.PostId,
//////                                CommitMessage = commitMessage,
//////                                Manager = manager.User.FullName,
//////                                Timestamp = DateTime.UtcNow
//////                            });
//////                    }
//////                }

//////                await _postService.DeletePostAsync(post);
//////                await _db.SaveChangesAsync();

//////                await transaction.CommitAsync();
//////                return NoContent();
//////            }
//////            catch (Exception ex)
//////            {
//////                try { await transaction.RollbackAsync(); } catch { }
//////                Console.Error.WriteLine(ex.ToString());
//////                var inner = ex.InnerException?.Message ?? ex.Message;
//////                return BadRequest(new { message = "Failed to delete post", detail = inner });
//////            }
//////        }

//////        [Authorize]
//////        [HttpPost("{id}/repost")]
//////        public async Task<IActionResult> Repost(int id)
//////        {
//////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//////            await _postService.RepostAsync(id, userId);
//////            return Ok();
//////        }
//////    }
//////}


////using Microsoft.AspNetCore.Authorization;
////using Microsoft.AspNetCore.Mvc;
////using System.Security.Claims;
////using Microsoft.EntityFrameworkCore;
////using Project_Version1.Services;
////using Project_Version1.DTOs;
////using Project_Version1.Data;
////using AutoMapper;
////using System;
////using Microsoft.AspNetCore.SignalR;
////using Project_Version1.Hubs;
////using System.Linq;
////using System.Collections.Generic;
////using System.Threading.Tasks;

////namespace Project_Version1.Controllers
////{
////    [ApiController]
////    [Route("api/[controller]")]
////    public class PostsController : ControllerBase
////    {
////        private readonly PostService _postService;
////        private readonly FnfKnowledgeBaseContext _db;
////        private readonly IMapper _mapper;
////        private readonly IHubContext<NotificationHub> _hub;

////        public PostsController(PostService postService, FnfKnowledgeBaseContext db, IMapper mapper, IHubContext<NotificationHub> hub)
////        {
////            _postService = postService;
////            _db = db;
////            _mapper = mapper;
////            _hub = hub;
////        }

////        [HttpGet("feed")]
////        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
////        {
////            var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);
////            return Ok(posts);
////        }

////        [HttpGet("{id}")]
////        public async Task<IActionResult> Get(int id)
////        {
////            try
////            {
////                var post = await _db.Posts
////                    .AsNoTracking()
////                    .Include(p => p.User)
////                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
////                    .Include(p => p.Comments).ThenInclude(c => c.User)
////                    .Include(p => p.Attachments)
////                    .Include(p => p.Reposts).ThenInclude(r => r.User)
////                    .Include(p => p.Votes)
////                    .FirstOrDefaultAsync(p => p.PostId == id);

////                if (post == null)
////                    return NotFound();

////                string? deptName = null;
////                if (post.Dept != null)
////                {
////                    deptName = post.Dept.DeptName;
////                }
////                else if (post.DeptId != 0)
////                {
////                    var dept = await _db.Departments.FindAsync(post.DeptId);
////                    deptName = dept?.DeptName;
////                }

////                var upvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase));
////                var downvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////                var commentsCount = post.Comments?.Count ?? 0;

////                var dto = new PostDetailDto
////                {
////                    PostId = post.PostId,
////                    Title = post.Title ?? string.Empty,
////                    Body = post.Body ?? string.Empty,
////                    UpvoteCount = upvoteCount,
////                    DownvoteCount = downvoteCount,
////                    CommentsCount = commentsCount,
////                    CreatedAt = post.CreatedAt,
////                    AuthorName = post.User?.FullName,
////                    DepartmentName = deptName,
////                    // ✅ include userId & deptId so frontend can check owner/department
////                    UserId = post.UserId,
////                    DeptId = post.DeptId,
////                    Tags = post.PostTags?.Select(pt => pt.Tag?.TagName).Where(tn => !string.IsNullOrEmpty(tn)).ToList() ?? new List<string>(),
////                    Attachments = post.Attachments?.Select(a => new AttachmentDto
////                    {
////                        AttachmentId = a.AttachmentId,
////                        FileName = a.FileName,
////                        FilePath = a.FilePath,
////                        FileType = a.FileType
////                    }).ToList() ?? new List<AttachmentDto>(),
////                    IsRepost = false,
////                    RepostedBy = null,
////                    Reposts = post.Reposts?.OrderByDescending(r => r.CreatedAt).Select(r => new RepostBriefDto
////                    {
////                        RepostId = r.RepostId,
////                        UserId = r.UserId,
////                        UserName = r.User?.FullName,
////                        CreatedAt = r.CreatedAt
////                    }).ToList()
////                };

////                return Ok(dto);
////            }
////            catch (Exception ex)
////            {
////                Console.Error.WriteLine("Error in PostsController.Get:");
////                Console.Error.WriteLine(ex.ToString());
////                var inner = ex.InnerException?.Message ?? ex.Message;
////                return StatusCode(500, new { message = "Server error while loading post", detail = inner });
////            }
////        }

////        [Authorize]
////        [HttpGet("mine")]
////        public async Task<IActionResult> GetMyPosts()
////        {
////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////            var posts = await _postService.GetPostsByUserAsync(userId);

////            var result = posts.Select(p =>
////            {
////                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
////                var commentsCount = p.Comments?.Count ?? 0;

////                List<RepostBriefDto>? reposts = null;
////                if (p.Reposts != null && p.Reposts.Any())
////                {
////                    reposts = p.Reposts
////                        .OrderByDescending(r => r.CreatedAt)
////                        .Select(r => new RepostBriefDto
////                        {
////                            RepostId = r.RepostId,
////                            UserId = r.UserId,
////                            UserName = r.User?.FullName,
////                            CreatedAt = r.CreatedAt
////                        })
////                        .ToList();
////                }

////                return new PostBriefDto
////                {
////                    PostId = p.PostId,
////                    Title = p.Title ?? string.Empty,
////                    BodyPreview = p.Body ?? string.Empty,
////                    UpvoteCount = upvoteCount,
////                    DownvoteCount = downvoteCount,
////                    CommentsCount = commentsCount,
////                    CreatedAt = p.CreatedAt,
////                    AuthorName = p.User?.FullName,
////                    // include user & dept as strings here to match PostBriefDto signature
////                    UserId = p.UserId.ToString(),
////                    DeptId = p.DeptId.ToString(),
////                    IsRepost = false,
////                    RepostedBy = null,
////                    Reposts = reposts
////                };
////            }).ToList();

////            return Ok(result);
////        }

////        [Authorize]
////        [HttpPut("{id}")]
////        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
////        {
////            var post = await _db.Posts.FindAsync(id);
////            if (post == null) return NotFound();

////            var role = User.FindFirstValue(ClaimTypes.Role);
////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////            // Owner can edit
////            if (post.UserId == userId)
////            {
////                // owner editing: allow; commit messages only relevant for managers
////            }
////            else if (role == "Manager")
////            {
////                // Manager may edit posts in their own department only
////                var manager = await _db.Managers.Include(m => m.User)
////                                                .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

////                if (manager == null)
////                    return Forbid();

////                // record commit if provided
////                if (!string.IsNullOrEmpty(commitMessage))
////                {
////                    _db.Commits.Add(new Commit
////                    {
////                        PostId = id,
////                        ManagerId = manager.ManagerId,
////                        Message = commitMessage,
////                        CreatedAt = DateTime.UtcNow
////                    });

////                    if (post.UserId != null)
////                    {
////                        await _hub.Clients.User(post.UserId.ToString()!)
////                            .SendAsync("ReceiveNotification", new
////                            {
////                                Type = "PostUpdate",
////                                PostId = id,
////                                CommitMessage = commitMessage,
////                                Manager = manager.User.FullName,
////                                Timestamp = DateTime.UtcNow
////                            });
////                    }
////                }
////            }
////            else
////            {
////                // not owner and not a manager of the post's dept
////                return Forbid();
////            }

////            if (!string.IsNullOrEmpty(dto.Title)) post.Title = dto.Title;
////            if (!string.IsNullOrEmpty(dto.Body)) post.Body = dto.Body;

////            post.UpdatedAt = DateTime.UtcNow;

////            await _postService.UpdatePostAsync(post);
////            await _db.SaveChangesAsync();

////            return Ok(new { message = "Post updated successfully" });
////        }

////        [Authorize]
////        [HttpDelete("{id}")]
////        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
////        {
////            var post = await _db.Posts.FindAsync(id);
////            if (post == null) return NotFound();

////            var role = User.FindFirstValue(ClaimTypes.Role);
////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

////            // Only managers can delete posts (employees are not allowed to delete their own posts)
////            if (role != "Manager")
////            {
////                return Forbid("Only managers can delete posts");
////            }

////            // Manager must have DeptId matching the post's DeptId
////            var manager = await _db.Managers.Include(m => m.User)
////                                            .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

////            if (manager == null)
////            {
////                // Manager doesn't belong to the post's department -> forbid
////                return Forbid();
////            }

////            // Manager is authorized to delete this post (their own posts or employees in their dept)
////            using var transaction = await _db.Database.BeginTransactionAsync();
////            try
////            {
////                if (!string.IsNullOrEmpty(commitMessage))
////                {
////                    // Insert commit (raw SQL used previously; keep similar approach)
////                    await _db.Database.ExecuteSqlInterpolatedAsync(
////                        $"INSERT INTO Commits (PostId, ManagerId, Message, CreatedAt) VALUES ({post.PostId}, {manager.ManagerId}, {commitMessage}, {DateTime.UtcNow})"
////                    );

////                    if (post.UserId != null)
////                    {
////                        await _hub.Clients.User(post.UserId.ToString()!)
////                            .SendAsync("ReceiveNotification", new
////                            {
////                                Type = "PostDeletion",
////                                PostId = post.PostId,
////                                CommitMessage = commitMessage,
////                                Manager = manager.User.FullName,
////                                Timestamp = DateTime.UtcNow
////                            });
////                    }
////                }

////                await _postService.DeletePostAsync(post);
////                await _db.SaveChangesAsync();

////                await transaction.CommitAsync();
////                return NoContent();
////            }
////            catch (Exception ex)
////            {
////                try { await transaction.RollbackAsync(); } catch { }
////                Console.Error.WriteLine(ex.ToString());
////                var inner = ex.InnerException?.Message ?? ex.Message;
////                return BadRequest(new { message = "Failed to delete post", detail = inner });
////            }
////        }

////        [Authorize]
////        [HttpPost("{id}/repost")]
////        public async Task<IActionResult> Repost(int id)
////        {
////            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
////            await _postService.RepostAsync(id, userId);
////            return Ok();
////        }
////    }
////}


//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using Microsoft.EntityFrameworkCore;
//using Project_Version1.Services;
//using Project_Version1.DTOs;
//using Project_Version1.Data;
//using AutoMapper;
//using System;
//using Microsoft.AspNetCore.SignalR;
//using Project_Version1.Hubs;
//using System.Linq;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Project_Version1.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class PostsController : ControllerBase
//    {
//        private readonly PostService _postService;
//        private readonly FnfKnowledgeBaseContext _db;
//        private readonly IMapper _mapper;
//        private readonly IHubContext<NotificationHub> _hub;

//        public PostsController(PostService postService, FnfKnowledgeBaseContext db, IMapper mapper, IHubContext<NotificationHub> hub)
//        {
//            _postService = postService;
//            _db = db;
//            _mapper = mapper;
//            _hub = hub;
//        }

//        [HttpGet("feed")]
//        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//        {
//            try
//            {
//                var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);

//                // Compute permission flags per post based on current user (if any)
//                var userIdClaim = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
//                var role = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Role) : null;
//                int? currentUserId = null;
//                if (int.TryParse(userIdClaim, out var uid)) currentUserId = uid;

//                HashSet<int> managerDeptIds = new();
//                if (currentUserId.HasValue)
//                {
//                    var mgrs = await _db.Managers.Where(m => m.UserId == currentUserId.Value).Select(m => m.DeptId).ToListAsync();
//                    managerDeptIds = new HashSet<int>(mgrs);
//                }

//                var result = posts.Select(p =>
//                {
//                    // p.UserId and p.DeptId are strings (PostBriefDto shape) — try parse safely
//                    var parsedDeptId = 0;
//                    var parsedUserId = 0;
//                    int.TryParse(p.DeptId ?? "", out parsedDeptId);
//                    int.TryParse(p.UserId ?? "", out parsedUserId);

//                    var isOwner = currentUserId.HasValue && parsedUserId == currentUserId.Value;
//                    var isManager = string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
//                    var managerOfDept = isManager && currentUserId.HasValue && managerDeptIds.Contains(parsedDeptId);

//                    p.CanEdit = isOwner || managerOfDept;
//                    p.CanDelete = managerOfDept;

//                    return p;
//                }).ToList();

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                Console.Error.WriteLine("Error in PostsController.Feed:");
//                Console.Error.WriteLine(ex.ToString());
//                var inner = ex.InnerException?.Message ?? ex.Message;
//                return StatusCode(500, new { message = "Server error while loading posts", detail = inner });
//            }
//        }

//        [HttpGet("{id}")]
//        public async Task<IActionResult> Get(int id)
//        {
//            try
//            {
//                var post = await _db.Posts
//                    .AsNoTracking()
//                    .Include(p => p.User)
//                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
//                    .Include(p => p.Comments).ThenInclude(c => c.User)
//                    .Include(p => p.Attachments)
//                    .Include(p => p.Reposts).ThenInclude(r => r.User)
//                    .Include(p => p.Votes)
//                    .FirstOrDefaultAsync(p => p.PostId == id);

//                if (post == null)
//                    return NotFound();

//                string? deptName = null;
//                if (post.Dept != null)
//                {
//                    deptName = post.Dept.DeptName;
//                }
//                else if (post.DeptId != 0)
//                {
//                    var dept = await _db.Departments.FindAsync(post.DeptId);
//                    deptName = dept?.DeptName;
//                }

//                var upvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase));
//                var downvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//                var commentsCount = post.Comments?.Count ?? 0;

//                var dto = new PostDetailDto
//                {
//                    PostId = post.PostId,
//                    Title = post.Title ?? string.Empty,
//                    Body = post.Body ?? string.Empty,
//                    UpvoteCount = upvoteCount,
//                    DownvoteCount = downvoteCount,
//                    CommentsCount = commentsCount,
//                    CreatedAt = post.CreatedAt,
//                    AuthorName = post.User?.FullName,
//                    DepartmentName = deptName,
//                    // ✅ include userId & deptId so frontend can check owner/department
//                    UserId = post.UserId,
//                    DeptId = post.DeptId,
//                    Tags = post.PostTags?.Select(pt => pt.Tag?.TagName).Where(tn => !string.IsNullOrEmpty(tn)).ToList() ?? new List<string>(),
//                    Attachments = post.Attachments?.Select(a => new AttachmentDto
//                    {
//                        AttachmentId = a.AttachmentId,
//                        FileName = a.FileName,
//                        FilePath = a.FilePath,
//                        FileType = a.FileType
//                    }).ToList() ?? new List<AttachmentDto>(),
//                    IsRepost = false,
//                    RepostedBy = null,
//                    Reposts = post.Reposts?.OrderByDescending(r => r.CreatedAt).Select(r => new RepostBriefDto
//                    {
//                        RepostId = r.RepostId,
//                        UserId = r.UserId,
//                        UserName = r.User?.FullName,
//                        CreatedAt = r.CreatedAt
//                    }).ToList()
//                };

//                // compute permission flags for this single post
//                var userIdClaim = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
//                var role = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Role) : null;
//                if (int.TryParse(userIdClaim, out var currentUserId))
//                {
//                    var isOwner = currentUserId == post.UserId;
//                    var isManager = string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
//                    var manager = isManager ? await _db.Managers.FirstOrDefaultAsync(m => m.UserId == currentUserId && m.DeptId == post.DeptId) : null;
//                    var managerOfDept = manager != null;

//                    dto.CanEdit = isOwner || managerOfDept;
//                    dto.CanDelete = managerOfDept;
//                }
//                else
//                {
//                    dto.CanEdit = false;
//                    dto.CanDelete = false;
//                }

//                return Ok(dto);
//            }
//            catch (Exception ex)
//            {
//                Console.Error.WriteLine("Error in PostsController.Get:");
//                Console.Error.WriteLine(ex.ToString());
//                var inner = ex.InnerException?.Message ?? ex.Message;
//                return StatusCode(500, new { message = "Server error while loading post", detail = inner });
//            }
//        }

//        [Authorize]
//        [HttpGet("mine")]
//        public async Task<IActionResult> GetMyPosts()
//        {
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//            var posts = await _postService.GetPostsByUserAsync(userId);

//            // fetch all departments where this user is a manager (likely only 0 or 1 but handle multiple)
//            var managerDeptIds = await _db.Managers.Where(m => m.UserId == userId).Select(m => m.DeptId).ToListAsync();
//            var isManagerRole = string.Equals(User.FindFirstValue(ClaimTypes.Role), "Manager", StringComparison.OrdinalIgnoreCase);

//            var result = posts.Select(p =>
//            {
//                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
//                var commentsCount = p.Comments?.Count ?? 0;

//                List<RepostBriefDto>? reposts = null;
//                if (p.Reposts != null && p.Reposts.Any())
//                {
//                    reposts = p.Reposts
//                        .OrderByDescending(r => r.CreatedAt)
//                        .Select(r => new RepostBriefDto
//                        {
//                            RepostId = r.RepostId,
//                            UserId = r.UserId,
//                            UserName = r.User?.FullName,
//                            CreatedAt = r.CreatedAt
//                        })
//                        .ToList();
//                }

//                var dto = new PostBriefDto
//                {
//                    PostId = p.PostId,
//                    Title = p.Title ?? string.Empty,
//                    BodyPreview = p.Body ?? string.Empty,
//                    UpvoteCount = upvoteCount,
//                    DownvoteCount = downvoteCount,
//                    CommentsCount = commentsCount,
//                    CreatedAt = p.CreatedAt,
//                    AuthorName = p.User?.FullName,
//                    // include user & dept as strings here to match PostBriefDto signature
//                    UserId = p.UserId.ToString(),
//                    DeptId = p.DeptId.ToString(),
//                    IsRepost = false,
//                    RepostedBy = null,
//                    Reposts = reposts
//                };

//                // owner (always true here) and manager-of-dept if applicable
//                var parsedDeptId = 0;
//                int.TryParse(dto.DeptId ?? "", out parsedDeptId);
//                var managerOfDept = isManagerRole && managerDeptIds.Contains(parsedDeptId);

//                dto.CanEdit = true || managerOfDept; // owner = true
//                dto.CanDelete = managerOfDept;

//                return dto;
//            }).ToList();

//            return Ok(result);
//        }

//        [Authorize]
//        [HttpPut("{id}")]
//        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
//        {
//            var post = await _db.Posts.FindAsync(id);
//            if (post == null) return NotFound();

//            var role = User.FindFirstValue(ClaimTypes.Role);
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//            // Owner can edit
//            if (post.UserId == userId)
//            {
//                // owner editing: allow; commit messages only relevant for managers
//            }
//            else if (role == "Manager")
//            {
//                // Manager may edit posts in their own department only
//                var manager = await _db.Managers.Include(m => m.User)
//                                                .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

//                if (manager == null)
//                    return Forbid();

//                // record commit if provided
//                if (!string.IsNullOrEmpty(commitMessage))
//                {
//                    _db.Commits.Add(new Commit
//                    {
//                        PostId = id,
//                        ManagerId = manager.ManagerId,
//                        Message = commitMessage,
//                        CreatedAt = DateTime.UtcNow
//                    });

//                    if (post.UserId != null)
//                    {
//                        await _hub.Clients.User(post.UserId.ToString()!)
//                            .SendAsync("ReceiveNotification", new
//                            {
//                                Type = "PostUpdate",
//                                PostId = id,
//                                CommitMessage = commitMessage,
//                                Manager = manager.User.FullName,
//                                Timestamp = DateTime.UtcNow
//                            });
//                    }
//                }
//            }
//            else
//            {
//                // not owner and not a manager of the post's dept
//                return Forbid();
//            }

//            if (!string.IsNullOrEmpty(dto.Title)) post.Title = dto.Title;
//            if (!string.IsNullOrEmpty(dto.Body)) post.Body = dto.Body;

//            post.UpdatedAt = DateTime.UtcNow;

//            await _postService.UpdatePostAsync(post);
//            await _db.SaveChangesAsync();

//            return Ok(new { message = "Post updated successfully" });
//        }

//        [Authorize]
//        [HttpDelete("{id}")]
//        public async Task<IActionResult> Delete(int id, [FromQuery] string? commitMessage = null)
//        {
//            var post = await _db.Posts.FindAsync(id);
//            if (post == null) return NotFound();

//            var role = User.FindFirstValue(ClaimTypes.Role);
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

//            // Only managers can delete posts (employees are not allowed to delete their own posts)
//            if (role != "Manager")
//            {
//                return Forbid("Only managers can delete posts");
//            }

//            // Manager must have DeptId matching the post's DeptId
//            var manager = await _db.Managers.Include(m => m.User)
//                                            .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

//            if (manager == null)
//            {
//                // Manager doesn't belong to the post's department -> forbid
//                return Forbid();
//            }

//            // Manager is authorized to delete this post (their own posts or employees in their dept)
//            using var transaction = await _db.Database.BeginTransactionAsync();
//            try
//            {
//                if (!string.IsNullOrEmpty(commitMessage))
//                {
//                    // Insert commit (raw SQL used previously; keep similar approach)
//                    await _db.Database.ExecuteSqlInterpolatedAsync(
//                        $"INSERT INTO Commits (PostId, ManagerId, Message, CreatedAt) VALUES ({post.PostId}, {manager.ManagerId}, {commitMessage}, {DateTime.UtcNow})"
//                    );

//                    if (post.UserId != null)
//                    {
//                        await _hub.Clients.User(post.UserId.ToString()!)
//                            .SendAsync("ReceiveNotification", new
//                            {
//                                Type = "PostDeletion",
//                                PostId = post.PostId,
//                                CommitMessage = commitMessage,
//                                Manager = manager.User.FullName,
//                                Timestamp = DateTime.UtcNow
//                            });
//                    }
//                }

//                await _postService.DeletePostAsync(post);
//                await _db.SaveChangesAsync();

//                await transaction.CommitAsync();
//                return NoContent();
//            }
//            catch (Exception ex)
//            {
//                try { await transaction.RollbackAsync(); } catch { }
//                Console.Error.WriteLine(ex.ToString());
//                var inner = ex.InnerException?.Message ?? ex.Message;
//                return BadRequest(new { message = "Failed to delete post", detail = inner });
//            }
//        }

//        [Authorize]
//        [HttpPost("{id}/repost")]
//        public async Task<IActionResult> Repost(int id)
//        {
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//            await _postService.RepostAsync(id, userId);
//            return Ok();
//        }
//    }
//}


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
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // --------------------------
        // NEW: Create post endpoint
        // --------------------------
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] PostCreateDto dto)
        {
            // Get authenticated user id
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // DepartmentId is provided in token as "DepartmentId" in your payload
            var deptClaim = User.FindFirst("DepartmentId")?.Value ?? User.FindFirstValue("DepartmentId");
            int deptId = 0;
            int.TryParse(deptClaim, out deptId); // if parsing fails, pass 0 (service may handle)

            try
            {
                var post = await _postService.CreatePostAsync(dto, userId, deptId);

                // Map to PostDetailDto for response (mapping exists in MappingProfile)
                var resultDto = _mapper.Map<PostDetailDto>(post);

                return CreatedAtAction(nameof(Get), new { id = post.PostId }, resultDto);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Failed to create post", detail = inner });
            }
        }

        [HttpGet("feed")]
        public async Task<IActionResult> Feed([FromQuery] int? deptId, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var posts = await _postService.GetPostsFeedAsync(deptId, tag, page, pageSize);

                // Compute permission flags per post based on current user (if any)
                var userIdClaim = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                var role = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Role) : null;
                int? currentUserId = null;
                if (int.TryParse(userIdClaim, out var uid)) currentUserId = uid;

                HashSet<int> managerDeptIds = new();
                if (currentUserId.HasValue)
                {
                    var mgrs = await _db.Managers.Where(m => m.UserId == currentUserId.Value).Select(m => m.DeptId).ToListAsync();
                    managerDeptIds = new HashSet<int>(mgrs);
                }

                var result = posts.Select(p =>
                {
                    // p.UserId and p.DeptId are strings (PostBriefDto shape) — try parse safely
                    var parsedDeptId = 0;
                    var parsedUserId = 0;
                    int.TryParse(p.DeptId ?? "", out parsedDeptId);
                    int.TryParse(p.UserId ?? "", out parsedUserId);

                    var isOwner = currentUserId.HasValue && parsedUserId == currentUserId.Value;
                    var isManager = string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
                    var managerOfDept = isManager && currentUserId.HasValue && managerDeptIds.Contains(parsedDeptId);

                    p.CanEdit = isOwner || managerOfDept;
                    p.CanDelete = managerOfDept;

                    return p;
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error in PostsController.Feed:");
                Console.Error.WriteLine(ex.ToString());
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Server error while loading posts", detail = inner });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var post = await _db.Posts
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                    .Include(p => p.Comments).ThenInclude(c => c.User)
                    .Include(p => p.Attachments)
                    .Include(p => p.Reposts).ThenInclude(r => r.User)
                    .Include(p => p.Votes)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound();

                string? deptName = null;
                if (post.Dept != null)
                {
                    deptName = post.Dept.DeptName;
                }
                else if (post.DeptId != 0)
                {
                    var dept = await _db.Departments.FindAsync(post.DeptId);
                    deptName = dept?.DeptName;
                }

                var upvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase));
                var downvoteCount = post.Votes?.Count(v => v.PostId == post.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
                var commentsCount = post.Comments?.Count ?? 0;

                var dto = new PostDetailDto
                {
                    PostId = post.PostId,
                    Title = post.Title ?? string.Empty,
                    Body = post.Body ?? string.Empty,
                    UpvoteCount = upvoteCount,
                    DownvoteCount = downvoteCount,
                    CommentsCount = commentsCount,
                    CreatedAt = post.CreatedAt,
                    AuthorName = post.User?.FullName,
                    DepartmentName = deptName,
                    // ✅ include userId & deptId so frontend can check owner/department
                    UserId = post.UserId,
                    DeptId = post.DeptId,
                    Tags = post.PostTags?.Select(pt => pt.Tag?.TagName).Where(tn => !string.IsNullOrEmpty(tn)).ToList() ?? new List<string>(),
                    Attachments = post.Attachments?.Select(a => new AttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType
                    }).ToList() ?? new List<AttachmentDto>(),
                    IsRepost = false,
                    RepostedBy = null,
                    Reposts = post.Reposts?.OrderByDescending(r => r.CreatedAt).Select(r => new RepostBriefDto
                    {
                        RepostId = r.RepostId,
                        UserId = r.UserId,
                        UserName = r.User?.FullName,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                };

                // compute permission flags for this single post
                var userIdClaim = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                var role = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Role) : null;
                if (int.TryParse(userIdClaim, out var currentUserId))
                {
                    var isOwner = currentUserId == post.UserId;
                    var isManager = string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
                    var manager = isManager ? await _db.Managers.FirstOrDefaultAsync(m => m.UserId == currentUserId && m.DeptId == post.DeptId) : null;
                    var managerOfDept = manager != null;

                    dto.CanEdit = isOwner || managerOfDept;
                    dto.CanDelete = managerOfDept;
                }
                else
                {
                    dto.CanEdit = false;
                    dto.CanDelete = false;
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error in PostsController.Get:");
                Console.Error.WriteLine(ex.ToString());
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Server error while loading post", detail = inner });
            }
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var posts = await _postService.GetPostsByUserAsync(userId);

            // fetch all departments where this user is a manager (likely only 0 or 1 but handle multiple)
            var managerDeptIds = await _db.Managers.Where(m => m.UserId == userId).Select(m => m.DeptId).ToListAsync();
            var isManagerRole = string.Equals(User.FindFirstValue(ClaimTypes.Role), "Manager", StringComparison.OrdinalIgnoreCase);

            var result = posts.Select(p =>
            {
                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
                var commentsCount = p.Comments?.Count ?? 0;

                List<RepostBriefDto>? reposts = null;
                if (p.Reposts != null && p.Reposts.Any())
                {
                    reposts = p.Reposts
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new RepostBriefDto
                        {
                            RepostId = r.RepostId,
                            UserId = r.UserId,
                            UserName = r.User?.FullName,
                            CreatedAt = r.CreatedAt
                        })
                        .ToList();
                }

                var dto = new PostBriefDto
                {
                    PostId = p.PostId,
                    Title = p.Title ?? string.Empty,
                    BodyPreview = p.Body ?? string.Empty,
                    UpvoteCount = upvoteCount,
                    DownvoteCount = downvoteCount,
                    CommentsCount = commentsCount,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User?.FullName,
                    // include user & dept as strings here to match PostBriefDto signature
                    UserId = p.UserId.ToString(),
                    DeptId = p.DeptId.ToString(),
                    IsRepost = false,
                    RepostedBy = null,
                    Reposts = reposts
                };

                // owner (always true here) and manager-of-dept if applicable
                var parsedDeptId = 0;
                int.TryParse(dto.DeptId ?? "", out parsedDeptId);
                var managerOfDept = isManagerRole && managerDeptIds.Contains(parsedDeptId);

                dto.CanEdit = true || managerOfDept; // owner = true
                dto.CanDelete = managerOfDept;

                return dto;
            }).ToList();

            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostUpdateDto dto, [FromQuery] string? commitMessage = null)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Owner can edit
            if (post.UserId == userId)
            {
                // owner editing: allow; commit messages only relevant for managers
            }
            else if (role == "Manager")
            {
                // Manager may edit posts in their own department only
                var manager = await _db.Managers.Include(m => m.User)
                                                .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

                if (manager == null)
                    return Forbid();

                // record commit if provided
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
            else
            {
                // not owner and not a manager of the post's dept
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

            // Only managers can delete posts (employees are not allowed to delete their own posts)
            if (role != "Manager")
            {
                return Forbid("Only managers can delete posts");
            }

            // Manager must have DeptId matching the post's DeptId
            var manager = await _db.Managers.Include(m => m.User)
                                            .FirstOrDefaultAsync(m => m.UserId == userId && m.DeptId == post.DeptId);

            if (manager == null)
            {
                // Manager doesn't belong to the post's department -> forbid
                return Forbid();
            }

            // Manager is authorized to delete this post (their own posts or employees in their dept)
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (!string.IsNullOrEmpty(commitMessage))
                {
                    // Insert commit (raw SQL used previously; keep similar approach)
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
