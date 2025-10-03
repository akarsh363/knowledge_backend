//using Microsoft.AspNetCore.Http;
//using System;
//using System.Collections.Generic;

//namespace Project_Version1.DTOs
//{
//    public class PostCreateDto
//    {
//        public int DeptId { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Body { get; set; } = string.Empty;
//        public List<string>? Tags { get; set; }
//        public List<IFormFile>? Attachments { get; set; }
//    }

//    public class PostUpdateDto
//    {
//        public string? Title { get; set; }
//        public string? Body { get; set; }
//        public List<string>? Tags { get; set; }
//        public List<IFormFile>? Attachments { get; set; }
//    }

//    public class PostBriefDto
//    {
//        public int PostId { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string UserId { get; set; } = string.Empty;
//        public string DeptId { get; set; } = string.Empty;
//        public string BodyPreview { get; set; } = string.Empty;
//        public int? UpvoteCount { get; set; }
//        public int DownvoteCount { get; set; }
//        public int CommentsCount { get; set; }
//        public DateTime CreatedAt { get; set; }
//        public string? AuthorName { get; set; }
//        public bool IsRepost { get; set; }
//        public string? RepostedBy { get; set; }
//        public List<RepostBriefDto>? Reposts { get; set; }
//    }

//    public class PostDetailDto
//    {
//        public int PostId { get; set; }
//        public string Title { get; set; } = string.Empty;
//        public string Body { get; set; } = string.Empty;
//        public int? UpvoteCount { get; set; }
//        public int DownvoteCount { get; set; }
//        public int CommentsCount { get; set; }
//        public DateTime CreatedAt { get; set; }
//        public string? AuthorName { get; set; }
//        public string? DepartmentName { get; set; }
//        public int UserId { get; set; }
//        public int DeptId { get; set; }
//        public List<string>? Tags { get; set; }
//        public List<AttachmentDto>? Attachments { get; set; }
//        public bool IsRepost { get; set; }
//        public string? RepostedBy { get; set; }
//        public List<RepostBriefDto>? Reposts { get; set; }
//    }

//    public class AttachmentDto
//    {
//        public int AttachmentId { get; set; }
//        public string FileName { get; set; } = string.Empty;
//        public string FilePath { get; set; } = string.Empty;
//        public string FileType { get; set; } = string.Empty;
//    }
//}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Project_Version1.DTOs
{
    public class PostCreateDto
    {
        public int DeptId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string>? Tags { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }

    public class PostUpdateDto
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public List<string>? Tags { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }

    public class PostBriefDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DeptId { get; set; } = string.Empty;
        public string BodyPreview { get; set; } = string.Empty;
        public int? UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public bool IsRepost { get; set; }
        public string? RepostedBy { get; set; }
        public List<RepostBriefDto>? Reposts { get; set; }

        // Permission flags provided by backend (true/false)
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class PostDetailDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int? UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public string? DepartmentName { get; set; }
        public int UserId { get; set; }
        public int DeptId { get; set; }
        public List<string>? Tags { get; set; }
        public List<AttachmentDto>? Attachments { get; set; }
        public bool IsRepost { get; set; }
        public string? RepostedBy { get; set; }
        public List<RepostBriefDto>? Reposts { get; set; }

        // Permission flags provided by backend (true/false)
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }
}
