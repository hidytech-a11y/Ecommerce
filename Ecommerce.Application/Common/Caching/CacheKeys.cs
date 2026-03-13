namespace Ecommerce.Application.Common.Caching;

public static class CacheKeys
{
    public static string ProductsPage(int page, int pageSize, string? search)
        => $"catalog:products:page:{page}:size:{pageSize}:search:{search}";

    public static string ProductDetails(Guid productId)
        => $"catalog:product:{productId}";
}