using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Models;
using BlogApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public CategoriesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Category>>> Get() =>
            await _mongoDbService.GetCategoriesAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> Get(string id)
        {
            var category = await _mongoDbService.GetCategoryAsync(id);
            if (category == null) return NotFound();
            return category;
        }

        [HttpPost]
        [Authorize] // For now, only authenticated users can create categories
        public async Task<IActionResult> Post(Category category)
        {
            category.Id = null; // Ensure MongoDB generates a new ID
            await _mongoDbService.CreateCategoryAsync(category);
            return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Put(string id, Category category)
        {
            var existingCategory = await _mongoDbService.GetCategoryAsync(id);
            if (existingCategory == null) return NotFound();
            await _mongoDbService.UpdateCategoryAsync(id, category);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var category = await _mongoDbService.GetCategoryAsync(id);
            if (category == null) return NotFound();
            await _mongoDbService.DeleteCategoryAsync(id);
            return NoContent();
        }
    }
}