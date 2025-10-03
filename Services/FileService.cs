
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Project_Version1.Data;

namespace Project_Version1.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _env;
        public FileService(IWebHostEnvironment env) => _env = env;

        public async Task<(string fileName, string filePath, string fileType)> SaveFileAsync(IFormFile file)
        {
            var rootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            var uploads = Path.Combine(rootPath, "uploads", "attachments");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            var relativePath = $"/uploads/attachments/{fileName}";
            var extension = Path.GetExtension(file.FileName).ToLower();
            string fileType = extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
                ".pdf" => "pdf",
                ".doc" or ".docx" => "word",
                ".xls" or ".xlsx" => "excel",
                ".ppt" or ".pptx" => "powerpoint",
                ".txt" => "text",
                _ => "other"
            };
            return (fileName, relativePath, fileType);
        }
    }
}