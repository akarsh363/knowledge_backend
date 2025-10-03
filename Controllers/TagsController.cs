using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Data;
using Project_Version1.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;

        public TagsController(FnfKnowledgeBaseContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? deptId)
        {
            var query = _db.Tags.AsQueryable();
            if (deptId.HasValue)
                query = query.Where(t => t.DeptId == deptId.Value);

            var tags = await query
                .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return Ok(tags);
        }

        // ✅ Create or get existing tag for logged-in user's dept
        [Authorize]

        [HttpPost]

        public async Task<IActionResult> Create([FromBody] TagCreateDto dto)

        {

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _db.Users

                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)

                return Unauthorized(new { message = "User not found" });

            int deptId = user.DepartmentId;

            var existingTag = await _db.Tags

                .FirstOrDefaultAsync(t => t.TagName.ToLower() == dto.TagName.ToLower()

                                          && t.DeptId == deptId);

            if (existingTag != null)

            {

                var tagDto = _mapper.Map<TagDto>(existingTag);

                return Ok(tagDto);

            }

            var tag = new Tag

            {

                TagName = dto.TagName.Trim(),

                DeptId = deptId

            };

            _db.Tags.Add(tag);

            await _db.SaveChangesAsync();

            var createdDto = _mapper.Map<TagDto>(tag);

            return CreatedAtAction(nameof(Get), new { deptId = deptId }, createdDto);

        }
    }
}