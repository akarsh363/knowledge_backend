using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Project_Version1.DTOs
{
    public class PostCreateDto
    {
        public int DeptId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public List<string>? Tags { get; set; }

        // ✅ New: Attachments (multiple files allowed)
        public List<IFormFile>? Attachments { get; set; }
    }

    public class PostUpdateDto
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public List<string>? Tags { get; set; }
        public List<IFormFile>? Attachments { get; set; } // New files to add
    }

    public class PostBriefDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string BodyPreview { get; set; } = null!;
        public int? UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public bool IsRepost { get; set; }
        public string? RepostedBy { get; set; }
    }
    public class PostDetailDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;  // Full body, not preview
        public int? UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public string? DepartmentName { get; set; }
        public List<string>? Tags { get; set; }
        public List<AttachmentDto>? Attachments { get; set; }
        public bool IsRepost { get; set; }
        public string? RepostedBy { get; set; }
    }

    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!;
    }
}