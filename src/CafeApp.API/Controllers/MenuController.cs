using CafeApp.DataAccess.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // <-- Müşteriler için yetkilendirmeyi kaldırıyoruz, herkese açık!
public class MenuController : ControllerBase
{
    private readonly CafeDbContext _context;

    public MenuController(CafeDbContext context)
    {
        _context = context;
    }

    [HttpGet("categories-with-products")]
    public async Task<IActionResult> GetCategoriesWithProducts()
    {
        var data = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                Products = c.Products
                    .Where(p => !p.IsDeleted && p.IsAvailable)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        UnitPrice = p.UnitPrice,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        CategoryId = p.CategoryId
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{qrToken}")]
    public async Task<IActionResult> GetMenuByQrToken(string qrToken)
    {
        var cleanToken = qrToken.Trim();
        var table = await _context.Tables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.QrToken.ToLower() == cleanToken.ToLower());

        if (table == null)
            return NotFound(new { message = "Geçersiz veya süresi dolmuş masa karekodu." });

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                Products = c.Products
                    .Where(p => !p.IsDeleted) // Menüde tüm aktif ürünler görünsün
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        UnitPrice = p.UnitPrice,
                        Price = p.UnitPrice,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        InStock = p.StockQuantity > 0
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            TableNumber = table.TableNumber,
            Categories = categories
        });
    }
}