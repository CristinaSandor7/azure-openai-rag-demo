using Azure;
using Azure.AI.OpenAI;
using RagDemo.Endpoints;
using RagDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];

builder.Services.AddSingleton(new AzureOpenAIClient(
    new Uri(endpoint!),
    new AzureKeyCredential(apiKey!)
));

builder.Services.AddSingleton<SearchIndexService>();
builder.Services.AddSingleton<RagService>();

var app = builder.Build();

var ragService = app.Services.GetRequiredService<RagService>();
await ragService.SeedProductsIfEmptyAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapAskEndpoints();
app.MapAdminEndpoints();

app.Run();