
namespace Project_Version1.DTOs
{
    public class RegisterDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = "Employee";
        public int? DepartmentId { get; set; }
        public IFormFile? ProfilePicture { get; set; } // ✅

    }

    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
        public string DepartmentId { get; set; } = null!;
        public string? ProfilePicture { get; set; } // ✅ return stored path
    }
}