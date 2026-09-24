using CafeApp.Business.DTOs.Product;
using CafeApp.Business.Services.Abstract;
using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.API.Controllers;

// Güncelleme için DTO (Ayrı dosyaya taşımak istemezseniz doğrudan burada tanımlı)
public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public IFormFile? Image { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CafeDbContext _context;
    private readonly IStorageService _storageService;

    public ProductsController(CafeDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
    {
        string imageUrl = string.Empty;
        if (dto.Image != null)
        {
            imageUrl = await _storageService.UploadFileAsync(dto.Image);
        }

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            UnitPrice = dto.UnitPrice,
            CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId,
            ImageUrl = imageUrl,
            IsAvailable = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // Ürün Güncelleme (PUT: api/Products/{id})
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromForm] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.UnitPrice = dto.UnitPrice;
        product.CostPrice = dto.CostPrice;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;

        // Yeni fotoğraf yüklendiyse MinIO'ya aktar ve ImageUrl'i güncelle
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var imageUrl = await _storageService.UploadFileAsync(dto.Image);
            product.ImageUrl = imageUrl;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Ürün başarıyla güncellendi." });
    }

    // Ürün Silme (DELETE: api/Products/{id})
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ürün silindi." });
    }
}