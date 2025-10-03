namespace Project_Version1.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? ProfilePicture { get; set; }
    }

    public class UserCreateDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = "Employee";
        public int? DepartmentId { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }

    public class UserUpdateDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; } = null!;
        public string? Password { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public IFormFile? ProfilePicture { get; set; } // optional new upload
        public bool? RemoveProfilePicture { get; set; } // NEW - if true, remove existing file

    }
    public class UserStatsDto
    {
        public int TotalPosts { get; set; }
        public int TotalUpvotes { get; set; }
        public int TotalDownvotes { get; set; }
        public int TotalCommentsReceived { get; set; }
        public int TotalCommitsMade { get; set; }
    }
}