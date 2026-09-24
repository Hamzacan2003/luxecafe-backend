using CafeApp.Business.DTOs.Menu;
using CafeApp.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.Business.Services.Concrete;

public class MenuService
{
    private readonly CafeDbContext _context;

    public MenuService(CafeDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerMenuDto?> GetMenuByQrTokenAsync(string qrToken)
    {
        // 1. Masayı bul
        var table = await _context.Tables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.QrToken == qrToken);

        if (table == null) return null;

        // 2. Kategorileri ve içindeki aktif ürünleri çek
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.Products.Where(p => p.IsAvailable))
            .ToListAsync();

        return new CustomerMenuDto
        {
            TableNumber = table.TableNumber,
            Categories = categories.Select(c => new CategoryMenuDto
            {
                CategoryName = c.Name,
                Items = c.Products.Select(p => new MenuItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.UnitPrice,
                    ImageUrl = p.ImageUrl,
                    InStock = p.StockQuantity > 0
                }).ToList()
            }).ToList()
        };
    }
}