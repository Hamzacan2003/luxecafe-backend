using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.API.Controllers;

public record CreateCategoryRequest(string Name, int DisplayOrder);
public record UpdateCategoryRequest(string Name, int DisplayOrder);

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CafeDbContext _context;

    public CategoriesController(CafeDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                Products = c.Products
                    .Where(p => !p.IsDeleted)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        UnitPrice = p.UnitPrice,
                        CostPrice = p.CostPrice,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsAvailable = p.IsAvailable,
                        CategoryId = p.CategoryId
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound(new { message = "Kategori bulunamadı." });

        category.Name = request.Name;
        category.DisplayOrder = request.DisplayOrder;

        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound(new { message = "Kategori bulunamadı." });

        if (category.Products.Any())
            return BadRequest(new { message = "Bu kategoriye bağlı ürünler var. Önce ürünleri silin veya taşıyın." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Kategori başarıyla silindi." });
    }
}