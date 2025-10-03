// Services/CommentService.cs - Updated with vote functionality
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Project_Version1.Data;
using Project_Version1.DTOs;

namespace Project_Version1.Services
{
    public class CommentService
    {
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;
        private readonly VoteService _voteService;

        public CommentService(FnfKnowledgeBaseContext db, IMapper mapper, VoteService voteService)
        {
            _db = db;
            _mapper = mapper;
            _voteService = voteService;
        }

        public async Task<Comment?> AddCommentAsync(CommentCreateDto dto, int userId)
        {
            // Validate that the post exists
            var postExists = await _db.Posts.AnyAsync(p => p.PostId == dto.PostId);
            if (!postExists)
            {
                throw new ArgumentException($"Post with ID {dto.PostId} does not exist.");
            }

            // If this is a reply, validate that the parent comment exists and belongs to the same post
            if (dto.ParentCommentId.HasValue && dto.ParentCommentId.Value > 0)
            {
                var parentComment = await _db.Comments
                    .FirstOrDefaultAsync(c => c.CommentId == dto.ParentCommentId.Value);

                if (parentComment == null)
                {
                    throw new ArgumentException($"Parent comment with ID {dto.ParentCommentId.Value} does not exist.");
                }

                if (parentComment.PostId != dto.PostId)
                {
                    throw new ArgumentException("Parent comment must belong to the same post.");
                }
            }
            else
            {
                dto.ParentCommentId = null; // treat 0 as null
            }

            var comment = _mapper.Map<Comment>(dto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // Return the comment with user information
            return await _db.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);
        }

        public async Task<List<CommentDto>> GetCommentsForPostAsync(int postId, int? currentUserId = null)
        {
            var comments = await _db.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var commentDtos = new List<CommentDto>();

            foreach (var comment in comments)
            {
                var dto = _mapper.Map<CommentDto>(comment);

                // Get vote counts
                var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(comment.CommentId);
                dto.UpvoteCount = upvotes;
                dto.DownvoteCount = downvotes;

                // Get user's vote if user is logged in
                if (currentUserId.HasValue)
                {
                    dto.UserVote = await _voteService.GetUserVoteOnCommentAsync(comment.CommentId, currentUserId.Value);
                }

                commentDtos.Add(dto);
            }

            return commentDtos;
        }

        // Get hierarchical comments (nested structure) with vote information
        public async Task<List<CommentWithRepliesDto>> GetCommentsHierarchyAsync(int postId, int? currentUserId = null)
        {
            var allComments = await _db.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Build hierarchy with vote information
            var commentDtos = new List<CommentWithRepliesDto>();

            foreach (var comment in allComments)
            {
                var dto = _mapper.Map<CommentWithRepliesDto>(comment);

                // Get vote counts
                var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(comment.CommentId);
                dto.UpvoteCount = upvotes;
                dto.DownvoteCount = downvotes;

                // Get user's vote if user is logged in
                if (currentUserId.HasValue)
                {
                    dto.UserVote = await _voteService.GetUserVoteOnCommentAsync(comment.CommentId, currentUserId.Value);
                }

                commentDtos.Add(dto);
            }

            var commentDict = commentDtos.ToDictionary(c => c.CommentId);
            var rootComments = new List<CommentWithRepliesDto>();

            foreach (var comment in commentDtos)
            {
                if (comment.ParentCommentId.HasValue && commentDict.ContainsKey(comment.ParentCommentId.Value))
                {
                    // This is a reply
                    commentDict[comment.ParentCommentId.Value].Replies.Add(comment);
                }
                else
                {
                    // This is a root comment
                    rootComments.Add(comment);
                }
            }

            return rootComments;
        }

        public async Task<Comment?> GetCommentAsync(int commentId)
        {
            return await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }
        public async Task DeleteCommentAsync(Comment comment)
        {
            // Soft delete: just remove the comment if no replies
            var hasReplies = await _db.Comments.AnyAsync(c => c.ParentCommentId == comment.CommentId);
            if (hasReplies)
            {
                // If there are replies, we can choose to either prevent deletion or mark as deleted
                // Here, we'll just mark the comment as "[deleted]" and keep it
                comment.CommentText = "[deleted]";
                _db.Comments.Update(comment);
            }
            else
            {
                _db.Comments.Remove(comment);
            }

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }
        public async Task ForceDeleteCommentAsync(Comment comment)
        {
            // First delete all replies recursively
            await DeleteCommentAndRepliesRecursively(comment.CommentId);
            await _db.SaveChangesAsync();
        }

        private async Task DeleteCommentAndRepliesRecursively(int commentId)
        {
            var replies = await _db.Comments
                .Where(c => c.ParentCommentId == commentId)
                .ToListAsync();

            foreach (var reply in replies)
            {
                await DeleteCommentAndRepliesRecursively(reply.CommentId);
            }

            var comment = await _db.Comments.FindAsync(commentId);
            if (comment != null)
            {
                _db.Comments.Remove(comment);
            }
        }

        // Get comment with vote information
        public async Task<CommentDto?> GetCommentWithVotesAsync(int commentId, int? currentUserId = null)
        {
            var comment = await _db.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null) return null;

            var dto = _mapper.Map<CommentDto>(comment);

            // Get vote counts
            var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(commentId);
            dto.UpvoteCount = upvotes;
            dto.DownvoteCount = downvotes;

            // Get user's vote if user is logged in
            if (currentUserId.HasValue)
            {
                dto.UserVote = await _voteService.GetUserVoteOnCommentAsync(commentId, currentUserId.Value);
            }

            return dto;
        }
    }
}