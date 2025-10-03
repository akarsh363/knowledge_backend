using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Data;
using Project_Version1.DTOs;
using Project_Version1.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace Project_Version1.Services

{

    public class AuthService

    {

        private readonly FnfKnowledgeBaseContext _db;

        private readonly JwtHelper _jwt;

        private readonly IMapper _mapper;

        private readonly IWebHostEnvironment _env;


        public AuthService(FnfKnowledgeBaseContext db, JwtHelper jwt, IMapper mapper, IWebHostEnvironment env)

        {

            _db = db;

            _jwt = jwt;

            _mapper = mapper;

            _env = env;

        }

        public interface IAuthService
        {
            Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
            Task<AuthResponseDto?> LoginAsync(LoginDto dto);
            Task<UserDto?> UpdateProfileAsync(int userId, UserUpdateDto dto);

        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)

        {

            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);

            if (exists) return null;

            var user = _mapper.Map<User>(dto);

            user.PasswordHash = PasswordHasher.Hash(dto.Password);

            user.CreatedAt = DateTime.UtcNow;

            // Handle profile picture upload

            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)

            {

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profile-pics");

                if (!Directory.Exists(uploadsFolder))

                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))

                {

                    await dto.ProfilePicture.CopyToAsync(stream);

                }

                user.ProfilePicture = $"/uploads/profile-pics/{fileName}";

            }

            // Save user

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            if (user.Role == "Manager" && user.DepartmentId > 0)

            {

                var manager = new Manager

                {

                    UserId = user.UserId,

                    DeptId = user.DepartmentId

                };

                _db.Managers.Add(manager);

                await _db.SaveChangesAsync();

            }

            // Generate JWT

            var token = _jwt.GenerateToken(user);

            return new AuthResponseDto

            {

                Token = token,

                UserId = user.UserId,

                Role = user.Role ?? "Employee",

                ProfilePicture = user.ProfilePicture

            };

        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)

        {

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))

                return null;

            var token = _jwt.GenerateToken(user);

            return new AuthResponseDto

            {

                Token = token,

                UserId = user.UserId,

                Role = user.Role ?? "Employee",

                ProfilePicture = user.ProfilePicture

            };

        }

        //public async Task<UserDto?> UpdateProfileAsync(int userId, UserUpdateDto dto)

        //{

        //    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        //    if (user == null) return null;

        //    // Update basic fields

        //    if (!string.IsNullOrEmpty(dto.FullName))

        //        user.FullName = dto.FullName;

        //    if (dto.DepartmentId.HasValue)

        //        user.DepartmentId = dto.DepartmentId.Value;

        //    if (!string.IsNullOrEmpty(dto.Email))

        //        user.Email = dto.Email;

        //    if (!string.IsNullOrEmpty(dto.Password))

        //        user.PasswordHash = PasswordHasher.Hash(dto.Password);

        //    if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)

        //    {

        //        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/profile-pics");

        //        if (!Directory.Exists(uploadsFolder))

        //            Directory.CreateDirectory(uploadsFolder);

        //        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";

        //        var filePath = Path.Combine(uploadsFolder, fileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))

        //        {

        //            await dto.ProfilePicture.CopyToAsync(stream);

        //        }

        //        if (!string.IsNullOrEmpty(user.ProfilePicture))

        //        {

        //            var oldFile = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/'));

        //            if (File.Exists(oldFile))

        //                File.Delete(oldFile);

        //        }

        //        user.ProfilePicture = $"/uploads/profile-pics/{fileName}";

        //    }

        //    await _db.SaveChangesAsync();

        //    return _mapper.Map<UserDto>(user);

        //}
        public async Task<UserDto?> UpdateProfileAsync(int userId, UserUpdateDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return null;

            // ... existing updates ...

            // Handle new upload
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profile-pics");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    user.PasswordHash = PasswordHasher.Hash(dto.Password);

                }
                // delete existing file if any
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var existingPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingPath)) System.IO.File.Delete(existingPath);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProfilePicture.CopyToAsync(stream);
                }
                user.ProfilePicture = $"/uploads/profile-pics/{fileName}";
            }

            // Handle explicit removal
            if (dto.RemoveProfilePicture == true)
            {
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var existingPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingPath)) System.IO.File.Delete(existingPath);
                }
                user.ProfilePicture = null;
            }

            await _db.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }
        public async Task<UserStatsDto> GetUserStatsAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Votes)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.Manager)
                    .ThenInclude(m => m.Commits)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return null;

            var stats = new UserStatsDto
            {
                TotalPosts = user.Posts.Count,
                TotalUpvotes = user.Posts.Sum(p => p.Votes.Count(v => v.VoteType == "Upvote")),
                TotalDownvotes = user.Posts.Sum(p => p.Votes.Count(v => v.VoteType == "Downvote")),
                TotalCommentsReceived = user.Posts.Sum(p => p.Comments.Count),
                TotalCommitsMade = user.Manager?.Commits.Count ?? 0
            };

            return stats;
        }

    }

}