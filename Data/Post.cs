using System;
using System.Collections.Generic;

namespace Project_Version1.Data
{
    public partial class Post
    {
        public Post()
        {
            Attachments = new HashSet<Attachment>();
            Comments = new HashSet<Comment>();
            Commits = new HashSet<Commit>();
            PostTags = new HashSet<PostTag>();
            Reposts = new HashSet<Repost>();
            Votes = new HashSet<Vote>();
        }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public int DeptId { get; set; }

        public string Title { get; set; } = null!;

        public string Body { get; set; } = null!;

        public int? UpvoteCount { get; set; }

        public int? DownvoteCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsRepost { get; set; }

        public virtual ICollection<Attachment> Attachments { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }

        public virtual ICollection<Commit> Commits { get; set; }

        public virtual Department Dept { get; set; } = null!;

        public virtual ICollection<PostTag> PostTags { get; set; }

        public virtual ICollection<Repost> Reposts { get; set; }

        public virtual User User { get; set; } = null!;

        public virtual ICollection<Vote> Votes { get; set; }
    }
}
