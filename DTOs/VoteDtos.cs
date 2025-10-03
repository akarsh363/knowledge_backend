
namespace Project_Version1.DTOs
{
    public class VoteDto
    {
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        public string VoteType { get; set; } // "Upvote" or "Downvote"
    }
}