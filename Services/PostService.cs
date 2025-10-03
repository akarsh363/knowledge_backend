using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Project_Version1.Data;
using Project_Version1.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Version1.Services
{
    public class PostService
    {
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly FileService _fileService;

        public PostService(
            FnfKnowledgeBaseContext db,
            IMapper mapper,
            IWebHostEnvironment env,
            FileService fileService)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
            _fileService = fileService;
        }

        public async Task<Post> CreatePostAsync(PostCreateDto dto, int userId, int deptId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var post = _mapper.Map<Post>(dto);
                post.UserId = userId;
                post.DeptId = deptId;
                post.CreatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(dto.Body))
                    post.Body = dto.Body;

                _db.Posts.Add(post);
                await _db.SaveChangesAsync();

                var tagNames = dto.Tags?
                    .Select(t => t?.Trim().ToLower())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToList();

                if (tagNames != null && tagNames.Count > 0)
                {
                    var existingTags = await _db.Tags
                        .Where(t => tagNames.Contains(t.TagName.ToLower()) && t.DeptId == deptId)
                        .ToListAsync();

                    var existingNames = new HashSet<string>(existingTags.Select(t => t.TagName.ToLower()));
                    var missingNames = tagNames.Where(n => !existingNames.Contains(n)).ToList();

                    var newTags = new List<Tag>();
                    foreach (var missing in missingNames)
                    {
                        var newTag = new Tag
                        {
                            TagName = missing,
                            DeptId = deptId
                        };
                        _db.Tags.Add(newTag);
                        newTags.Add(newTag);
                    }

                    if (newTags.Any())
                    {
                        await _db.SaveChangesAsync();
                        existingTags.AddRange(newTags);
                    }

                    foreach (var tag in existingTags)
                    {
                        var exists = await _db.PostTags.AnyAsync(pt => pt.PostId == post.PostId && pt.TagId == tag.TagId);
                        if (!exists)
                        {
                            _db.PostTags.Add(new PostTag
                            {
                                PostId = post.PostId,
                                TagId = tag.TagId
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                }

                if (dto.Attachments != null && dto.Attachments.Any())
                {
                    foreach (var file in dto.Attachments)
                    {
                        if (file.Length == 0) continue;
                        if (file.Length > 5 * 1024 * 1024)
                            throw new InvalidOperationException("File too large (max 5MB).");

                        var (fileName, filePath, fileType) = await _fileService.SaveFileAsync(file);

                        var attachment = new Attachment
                        {
                            PostId = post.PostId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            FileType = fileType,
                            UploadedAt = DateTime.UtcNow
                        };

                        _db.Attachments.Add(attachment);
                    }

                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return post;
            }
            catch
            {
                try { await _db.Database.RollbackTransactionAsync(); } catch { /* ignore */ }
                throw;
            }
        }

        public IQueryable<Post> QueryPosts() =>
            _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Reposts).ThenInclude(r => r.User);

        public async Task<Post?> GetPostAsync(int id) =>
            await _db.Posts
                .Include(p => p.User)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Include(p => p.Attachments)
                .Include(p => p.Reposts).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.PostId == id);

        public async Task<List<PostBriefDto>> GetPostsFeedAsync(int? deptId, string? tag, int page, int pageSize)
        {
            // Build base query
            var query = _db.Posts.AsQueryable();

            if (deptId.HasValue)
                query = query.Where(p => p.DeptId == deptId.Value);

            if (!string.IsNullOrEmpty(tag))
                query = query.Where(p => p.PostTags.Any(pt => pt.Tag.TagName == tag));

            query = query.OrderByDescending(p => p.CreatedAt);

            // Apply paging and include related collections we need
            var paged = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Reposts).ThenInclude(r => r.User)
                .Include(p => p.Votes); // include votes to compute up/down counts

            var posts = await paged.ToListAsync();

            // Project into PostBriefDto and include Reposts
            var result = posts.Select(p =>
            {
                // safe helpers for counts
                var upvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Upvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
                var downvoteCount = p.Votes?.Count(v => v.PostId == p.PostId && v.CommentId == null && string.Equals(v.VoteType, "Downvote", StringComparison.OrdinalIgnoreCase)) ?? 0;
                var commentsCount = p.Comments?.Count ?? 0;

                // Map reposts collection into RepostBriefDto list (if any)
                List<RepostBriefDto>? reposts = null;
                if (p.Reposts != null && p.Reposts.Any())
                {
                    reposts = p.Reposts
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new RepostBriefDto
                        {
                            RepostId = r.RepostId,
                            UserId = r.UserId,
                            UserName = r.User?.FullName ?? null,
                            CreatedAt = r.CreatedAt
                        })
                        .ToList();
                }

                return new PostBriefDto
                {
                    PostId = p.PostId,
                    Title = p.Title ?? string.Empty,
                    BodyPreview = p.Body ?? string.Empty,
                    UpvoteCount = upvoteCount,
                    DownvoteCount = downvoteCount,
                    CommentsCount = commentsCount,
                    CreatedAt = p.CreatedAt,
                    AuthorName = p.User?.FullName,
                    // ✅ include userId & deptId so frontend can evaluate ownership/department
                    UserId = p.UserId.ToString(),
                    DeptId = p.DeptId.ToString(),
                    IsRepost = false, // this DTO represents the original post row; repost entries are represented via the Reposts list
                    RepostedBy = null,
                    Reposts = reposts
                };
            }).ToList();

            return result;
        }

        public async Task UpdatePostAsync(Post post)
        {
            post.UpdatedAt = DateTime.UtcNow;
            _db.Posts.Update(post);
            // save performed by caller
        }

        public async Task DeletePostAsync(Post post)
        {
            async Task RemoveRelatedAndPostAsync()
            {
                var commentIds = await _db.Comments
                    .Where(c => c.PostId == post.PostId)
                    .Select(c => c.CommentId)
                    .ToListAsync();

                if (commentIds.Any())
                {
                    _db.Votes.RemoveRange(_db.Votes.Where(v => v.CommentId != null && commentIds.Contains(v.CommentId.Value)));
                    _db.Attachments.RemoveRange(_db.Attachments.Where(a => a.CommentId != null && commentIds.Contains(a.CommentId.Value)));
                    _db.Comments.RemoveRange(_db.Comments.Where(c => commentIds.Contains(c.CommentId)));
                }

                _db.Votes.RemoveRange(_db.Votes.Where(v => v.PostId == post.PostId));
                _db.Reposts.RemoveRange(_db.Reposts.Where(r => r.PostId == post.PostId));
                _db.PostTags.RemoveRange(_db.PostTags.Where(pt => pt.PostId == post.PostId));
                _db.Attachments.RemoveRange(_db.Attachments.Where(a => a.PostId == post.PostId));
                _db.Posts.Remove(post);
            }

            if (_db.Database.CurrentTransaction == null)
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    await RemoveRelatedAndPostAsync();
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { }
                    throw;
                }
            }
            else
            {
                await RemoveRelatedAndPostAsync();
                await _db.SaveChangesAsync();
            }
        }

        public async Task RepostAsync(int postId, int userId)
        {
            var exists = await _db.Reposts.AnyAsync(r => r.PostId == postId && r.UserId == userId);
            if (exists) return;

            var repost = new Repost
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reposts.Add(repost);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Post>> GetPostsByUserAsync(int userId)
        {
            return await _db.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Reposts).ThenInclude(r => r.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
