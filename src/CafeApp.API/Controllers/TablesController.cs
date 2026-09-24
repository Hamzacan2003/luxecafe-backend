using CafeApp.Business.Services.Abstract;
using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

namespace CafeApp.API.Controllers;

public class CreateTableDto
{
    public string TableNumber { get; set; } = string.Empty;
}

public class UpdateTableDto
{
    public string TableNumber { get; set; } = string.Empty;
}

public class TableResponseDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public TableStatus Status { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly CafeDbContext _context;
    private readonly IQrCodeService _qrCodeService;
    private readonly IConfiguration _configuration;

    public TablesController(CafeDbContext context, IQrCodeService qrCodeService, IConfiguration configuration)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _configuration = configuration;
    }

    // Telefonun bağlanabileceği gerçek frontend URL'sini üreten merkezi metot
    private string GetClientBaseUrl()
    {
        // 1. appsettings.json dosyasında "FrontendSettings:ClientUrl" tanımlıysa öncelikli kullan
        var configuredUrl = _configuration["FrontendSettings:ClientUrl"];
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl.TrimEnd('/');
        }

        // 2. İstek atan adresi kontrol et
        string host = Request.Host.Host;

        // 3. Eğer istek localhost veya 127.0.0.1 ise, bilgisayarın Wi-Fi / Yerel Ağ IP'sini (192.168.x.x) otomatik bul
        if (host == "localhost" || host == "127.0.0.1")
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                var localIp = hostEntry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                       && !IPAddress.IsLoopback(ip));

                if (localIp != null)
                {
                    host = localIp.ToString();
                }
            }
            catch
            {
                // Fallback olarak host kalır
            }
        }

        var frontendPort = "5173";
        return $"http://{host}:{frontendPort}";
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tables = await _context.Tables
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        bool hasChanges = false;
        foreach (var t in tables)
        {
            if (string.IsNullOrWhiteSpace(t.QrToken))
            {
                t.QrToken = Guid.NewGuid().ToString("N");
                hasChanges = true;
            }
        }
        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        var clientBaseUrl = GetClientBaseUrl();

        var response = tables.Select(t =>
        {
            var menuUrl = $"{clientBaseUrl}/menu/{t.QrToken}";
            var qrBytes = _qrCodeService.GenerateQrCode(menuUrl);
            var base64 = Convert.ToBase64String(qrBytes);

            return new TableResponseDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Status = t.Status,
                QrToken = t.QrToken,
                QrCodeBase64 = base64
            };
        }).ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TableNumber))
            return BadRequest(new { message = "Masa numarası boş olamaz." });

        var table = new RestaurantTable
        {
            TableNumber = dto.TableNumber.Trim(),
            Status = TableStatus.Empty,
            QrToken = Guid.NewGuid().ToString("N")
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        var clientBaseUrl = GetClientBaseUrl();
        var menuUrl = $"{clientBaseUrl}/menu/{table.QrToken}";
        var qrBytes = _qrCodeService.GenerateQrCode(menuUrl);

        var response = new TableResponseDto
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            Status = table.Status,
            QrToken = table.QrToken,
            QrCodeBase64 = Convert.ToBase64String(qrBytes)
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTableDto dto)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return NotFound(new { message = "Masa bulunamadı." });

        table.TableNumber = dto.TableNumber.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { message = "Masa adı güncellendi." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return NotFound(new { message = "Masa bulunamadı." });

        if (table.Status == TableStatus.Occupied)
            return BadRequest(new { message = "Dolu ve açık adisyonu olan masa silinemez." });

        _context.Tables.Remove(table);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Masa başarıyla kaldırıldı." });
    }

    [HttpGet("{id}/qr-code")]
    public async Task<IActionResult> GetTableQrCode(Guid id)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return NotFound("Masa bulunamadı.");

        if (string.IsNullOrWhiteSpace(table.QrToken))
        {
            table.QrToken = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();
        }

        var clientBaseUrl = GetClientBaseUrl();
        var menuUrl = $"{clientBaseUrl}/menu/{table.QrToken}";
        var qrImageBytes = _qrCodeService.GenerateQrCode(menuUrl);

        return File(qrImageBytes, "image/png", $"{table.TableNumber}-qr.png");
    }
}