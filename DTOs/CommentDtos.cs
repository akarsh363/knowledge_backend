// DTOs/CommentDtos.cs - Updated with vote information
using System;

namespace Project_Version1.DTOs
{
    public class CommentCreateDto
    {
        public int? PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public string CommentText { get; set; } = null!;
    }
    public class CommentUpdateDto
    {
        public string CommentText { get; set; } = null!;
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public int? PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public string CommentText { get; set; } = null!;
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }

        // Vote information
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public string? UserVote { get; set; } // "upvote", "downvote", or null
    }

    public class CommentWithRepliesDto
    {
        public int CommentId { get; set; }
        public int? PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public string CommentText { get; set; } = null!;
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommentWithRepliesDto> Replies { get; set; } = new List<CommentWithRepliesDto>();

        // Vote information
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public string? UserVote { get; set; } // "upvote", "downvote", or null
    }

    // Enhanced comment DTO with additional info
    public class CommentDetailDto : CommentDto
    {
        public bool IsDeleted { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ReplyCount { get; set; }
    }
}