using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = default!;

    private Category() { }

    public Category(string name)
    {
        Name = name;
    }
}