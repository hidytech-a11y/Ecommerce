using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items;

    private Cart() { }

    public Cart(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
    }

    public void AddItem(Guid productId, int quantity)
    {
        var existing = _items.FirstOrDefault(x => x.ProductId == productId);

        if (existing != null)
        {
            existing.IncreaseQuantity(quantity);
            return;
        }

        _items.Add(new CartItem(productId, quantity));
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == productId);

        if (item != null)
            _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
    }
}