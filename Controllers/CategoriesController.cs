//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;
//using AutoMapper;
//using AutoMapper.QueryableExtensions;
//using Microsoft.EntityFrameworkCore;
//using Project_Version1.Data;
//using Project_Version1.DTOs;

//namespace Project_Version1.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class CategoriesController : ControllerBase
//    {
//        private readonly FnfKnowledgeBaseContext _db;
//        private readonly IMapper _mapper;

//        public CategoriesController(FnfKnowledgeBaseContext db, IMapper mapper)
//        {
//            _db = db;
//            _mapper = mapper;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            var departments = await _db.Departments
//                .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
//                .ToListAsync();
//            return Ok(departments);
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(FnfKnowledgeBaseContext db, IMapper mapper, ILogger<CategoriesController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var departments = await _db.Departments
                    .AsNoTracking()
                    .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return Ok(departments);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to load departments for /api/Categories");
                // Return a helpful error payload while preserving status code 500
                return StatusCode(500, new { message = "Server error while loading categories (departments).", detail = ex.Message });
            }
        }
    }
}
