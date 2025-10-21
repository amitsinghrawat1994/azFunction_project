using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace MyHttpFunction;

public class ItemDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
}


public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;

    public HttpExample(ILogger<HttpExample> logger)
    {
        _logger = logger;
    }

    // [Function("HttpExample")]
    // public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    // {
    //     _logger.LogInformation("C# HTTP trigger function processed a request.");
    //     return new OkObjectResult("Welcome to Azure Functions!");
    // }

    // 1) GET with optional query string: /api/items?filter=x&page=2
    [Function("GetItems")]
    public async Task<HttpResponseData> GetItems(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items")] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("GetItems");
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query); // or use manual parsing
        var filter = query["filter"];
        var page = int.TryParse(query["page"], out var p) ? p : 1;

        logger.LogInformation($"GetItems called filter={filter} page={page}");

        // Example response
        var items = new[] {
                new ItemDto { Id = "1", Name = "Apple", Quantity = 10 },
                new ItemDto { Id = "2", Name = "Banana", Quantity = 5 }
            };

        var res = req.CreateResponse(HttpStatusCode.OK);
        res.Headers.Add("Content-Type", "application/json");
        await res.WriteStringAsync(JsonSerializer.Serialize(items));
        return res;
    }

    // 2) GET with route parameter: /api/items/{id} 
    [Function("GetItemById")]
    public async Task<HttpResponseData> GetItemById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        var logger = context.GetLogger("GetItemById");
        logger.LogInformation($"GetItemById called id={id}");

        // fake lookup
        if (id != "1")
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Item {id} not found");
            return notFound;
        }

        var item = new ItemDto { Id = "1", Name = "Apple", Quantity = 10 };
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.Headers.Add("Content-Type", "application/json");
        await res.WriteStringAsync(JsonSerializer.Serialize(item));
        return res;
    }

    // 3) POST with JSON body -> bind to POCO
    [Function("CreateItem")]
    public async Task<HttpResponseData> CreateItem(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "items")] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("CreateItem");
        using var sr = new StreamReader(req.Body);
        var body = await sr.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Empty request body");
            return bad;
        }

        ItemDto? item;
        try
        {
            item = JsonSerializer.Deserialize<ItemDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (item is null)
                throw new JsonException("deserialized to null");
        }
        catch (JsonException ex)
        {
            logger.LogError($"JSON parse error: {ex.Message}");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON body");
            return bad;
        }

        // Simple validation
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Name is required");
            return bad;
        }

        // Simulate create (in real app, write to DB)
        item.Id = Guid.NewGuid().ToString();

        var created = req.CreateResponse(HttpStatusCode.Created);
        created.Headers.Add("Content-Type", "application/json");
        await created.WriteStringAsync(JsonSerializer.Serialize(item));
        // optionally set Location header:
        created.Headers.Add("Location", $"/api/items/{item.Id}");
        return created;
    }
}
