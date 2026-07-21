using RagDemo.Models;
using RagDemo.Services;

namespace RagDemo.Endpoints;

public static class AskEndpoints
{
    public static void MapAskEndpoints(this WebApplication app)
    {
        app.MapPost("/ask", async (AskRequest request, RagService ragService) =>
        {
            var (answer, usedProducts) = await ragService.AskAsync(request.Question);
            return Results.Ok(new { answer, usedProducts });
        });
    }
}