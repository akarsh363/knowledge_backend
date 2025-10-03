//namespace Project_Version1.DTOs
//{
//    //public class RepostBriefDto
//    //{
//    //    public int RepostId { get; set; }
//    //    public int UserId { get; set; }            // who reposted
//    //    public string? UserName { get; set; }     // reposting user's display name
//    //    public DateTime CreatedAt { get; set; }   // repost time
//   // }
//}


using System;

namespace Project_Version1.DTOs
{
    public class RepostBriefDto
    {
        public int RepostId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
