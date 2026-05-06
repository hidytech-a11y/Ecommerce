using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    private User() { } // EF

    public User(
        string firstname,
        string lastname,
        string email, 
        string passwordHash, 
        UserRole role)
    {
        Id = Guid.NewGuid();
        FirstName = firstname;
        LastName = lastname;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }
}