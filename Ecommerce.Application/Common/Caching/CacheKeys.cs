namespace Ecommerce.Application.Common.Caching;

public static class CacheKeys
{
    public static string ProductDetails(Guid id) => $"product:{id}";

    // NEW
    public static string ProductBySlug(string slug) => $"product:slug:{slug}";

    public static string ProductsPage(int page, int pageSize, string? search) =>
        $"products:page:{page}:size:{pageSize}:search:{search ?? "all"}";
}