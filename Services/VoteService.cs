// Services/VoteService.cs - Updated to handle comment voting
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Project_Version1.Data;
using Project_Version1.DTOs;

namespace Project_Version1.Services
{
    public class VoteService
    {
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;

        public VoteService(FnfKnowledgeBaseContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<bool> VoteAsync(VoteDto dto, int userId)
        {
            if (dto.PostId == null && dto.CommentId == null) return false;

            var existing = await _db.Votes.FirstOrDefaultAsync(v =>
                v.UserId == userId &&
                v.PostId == dto.PostId &&
                v.CommentId == dto.CommentId);

            if (existing != null)
            {
                if (existing.VoteType == dto.VoteType)
                {
                    // Remove vote if clicking same vote type
                    _db.Votes.Remove(existing);
                }
                else
                {
                    // Change vote type
                    existing.VoteType = dto.VoteType;
                    existing.CreatedAt = DateTime.UtcNow;
                    _db.Votes.Update(existing);
                }
            }
            else
            {
                var vote = _mapper.Map<Vote>(dto);
                vote.UserId = userId;
                vote.CreatedAt = DateTime.UtcNow;
                _db.Votes.Add(vote);
            }

            await _db.SaveChangesAsync();

            // Update counters for both posts and comments
            if (dto.PostId.HasValue)
                await UpdatePostVoteCountersAsync(dto.PostId.Value);

            if (dto.CommentId.HasValue)
                await UpdateCommentVoteCountersAsync(dto.CommentId.Value);

            return true;
        }

        private async Task UpdatePostVoteCountersAsync(int postId)
        {
            var upvotes = await _db.Votes.CountAsync(v => v.PostId == postId && v.VoteType == "upvote");
            var downvotes = await _db.Votes.CountAsync(v => v.PostId == postId && v.VoteType == "downvote");

            var post = await _db.Posts.FindAsync(postId);
            if (post != null)
            {
                post.UpvoteCount = upvotes;
                post.DownvoteCount = downvotes;
                _db.Posts.Update(post);
                await _db.SaveChangesAsync();
            }
        }

        private async Task UpdateCommentVoteCountersAsync(int commentId)
        {
            // Since Comment entity doesn't have vote count fields, 
            // we'll calculate them dynamically when needed
            // This method exists for consistency but doesn't update entity

            // If you want to cache counts, you could add them to a separate table
            // or calculate them in real-time in the service methods
        }

        // New method to get comment vote counts
        public async Task<(int upvotes, int downvotes)> GetCommentVoteCountsAsync(int commentId)
        {
            var upvotes = await _db.Votes.CountAsync(v => v.CommentId == commentId && v.VoteType == "upvote");
            var downvotes = await _db.Votes.CountAsync(v => v.CommentId == commentId && v.VoteType == "downvote");
            return (upvotes, downvotes);
        }

        // Get user's vote on a comment
        public async Task<string?> GetUserVoteOnCommentAsync(int commentId, int userId)
        {
            var vote = await _db.Votes.FirstOrDefaultAsync(v =>
                v.CommentId == commentId && v.UserId == userId);
            return vote?.VoteType;
        }
    }
}