using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Project_Version1.Data;

namespace Project_Version1.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;
        public JwtHelper(IConfiguration config) => _config = config;

        public string GenerateToken(User user)
        {
            var jwt = _config.GetSection("Jwt");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Fix for CS0023: Remove null-conditional operator from int property
            // Fix for CS1503: Ensure correct argument types for Claim constructor

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "Employee"),
                new Claim("DeptId", user.DepartmentId.ToString()) // Fixed: int does not need null check
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"] ?? "100")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}