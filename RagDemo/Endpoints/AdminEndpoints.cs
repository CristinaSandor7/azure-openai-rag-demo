using RagDemo.Models;
using RagDemo.Services;

namespace RagDemo.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/admin")
            .AddEndpointFilter(async (context, next) =>
            {
                var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var expectedApiKey = config["AdminApiKey"];
                var providedApiKey = context.HttpContext.Request.Headers["X-Admin-Api-Key"].ToString();

                if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != expectedApiKey)
                {
                    return Results.Unauthorized();
                }

                return await next(context);
            });

        adminGroup.MapPost("/products", async (AdminProductRequest request, RagService ragService) =>
        {
            var id = await ragService.AddOrUpdateProductAsync(request);
            return Results.Ok(new { message = "Product saved successfully", id });
        });

        adminGroup.MapDelete("/products/{id}", async (string id, RagService ragService) =>
        {
            await ragService.DeleteProductAsync(id);
            return Results.Ok(new { message = "Product deleted successfully" });
        });
    }
}