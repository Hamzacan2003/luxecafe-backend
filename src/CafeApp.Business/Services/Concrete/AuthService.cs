using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeApp.Business.DTOs.Auth;
using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CafeApp.Business.Services.Concrete;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<AppRole> roleManager,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _config = config;
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user == null)
            return (false, "Kullanıcı adı veya şifre hatalı.", null);

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return (false, "Kullanıcı adı veya şifre hatalı.", null);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Cashier";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);
        var expires = DateTime.UtcNow.AddDays(double.Parse(_config["JwtSettings:DurationInDays"] ?? "7"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Role, primaryRole),
            new("FullName", user.FullName ?? user.UserName!)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (true, "Giriş başarılı.", new AuthResponseDto
        {
            Token = tokenHandler.WriteToken(token),
            FullName = user.FullName ?? user.UserName!,
            UserName = user.UserName!,
            Role = primaryRole,
            ExpiresAt = expires
        });
    }

    public async Task<(bool Success, string Message)> RegisterEmployeeAsync(RegisterEmployeeDto dto)
    {
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return (false, "Geçersiz rol belirtildi.");

        var existing = await _userManager.FindByNameAsync(dto.UserName);
        if (existing != null)
            return (false, "Bu kullanıcı adı zaten kullanımda.");

        var user = new AppUser
        {
            UserName = dto.UserName,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, dto.Role);
        return (true, "Personel hesabı başarıyla oluşturuldu.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, "Şifre başarıyla güncellendi.");
    }
}