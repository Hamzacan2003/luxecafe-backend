using Microsoft.AspNetCore.Identity;

namespace CafeApp.DataAccess.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

