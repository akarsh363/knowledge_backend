using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Project_Version1.Data;
using Project_Version1.DTOs;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly FnfKnowledgeBaseContext _db;
        private readonly IMapper _mapper;

        public CategoriesController(FnfKnowledgeBaseContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _db.Departments
                .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return Ok(departments);
        }
    }
}