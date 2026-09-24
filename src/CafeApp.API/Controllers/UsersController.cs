using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.API.Controllers;

public class CreateEmployeeDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier"; // "Manager" veya "Cashier"
}

public class UserListDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UsersController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserListDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserListDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Tanımsız"
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Kullanıcı adı ve şifre zorunludur." });

        var existing = await _userManager.FindByNameAsync(dto.UserName);
        if (existing != null)
            return BadRequest(new { message = "Bu kullanıcı adı zaten kullanılıyor." });

        var user = new AppUser
        {
            UserName = dto.UserName.Trim(),
            FullName = dto.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? $"{dto.UserName.ToLower()}@cafe.local" : dto.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Kullanıcı oluşturulamadı.";
            return BadRequest(new { message = firstError });
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            await _roleManager.CreateAsync(new AppRole { Name = dto.Role });
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new { message = $"{dto.FullName} personeli başarıyla kaydedildi." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (user.UserName?.ToLower() == "admin")
            return BadRequest(new { message = "Varsayılan ana yönetici silinemez." });

        await _userManager.DeleteAsync(user);
        return Ok(new { message = "Personel sistemden kaldırıldı." });
    }
}