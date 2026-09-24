using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CafeApp.API.Controllers;

public record UpdateProfileDto(string FullName, string Email, string? CurrentPassword, string? NewPassword);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AccountController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Oturum bilgisi geçersiz." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.FullName,
            user.Email,
            Role = roles.FirstOrDefault() ?? "Cashier"
        });
    }

    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Oturum bilgisi geçersiz." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(new { message = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });

        // Şifre değişikliği talebi varsa
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                return BadRequest(new { message = "Şifrenizi değiştirmek için mevcut şifrenizi girmelisiniz." });

            var passResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!passResult.Succeeded)
                return BadRequest(new { message = string.Join(", ", passResult.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = "Profil bilgileri başarıyla güncellendi." });
    }
}