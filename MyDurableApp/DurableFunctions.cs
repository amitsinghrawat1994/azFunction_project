using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using System.Net;
using System.Text.Json;

public static class DurableFunctions
{
    // Orchestrator - defines the workflow
    [Function("Orchestrator")]
    public static async Task<string> Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext ctx)
    {
        // call an activity
        var result = await ctx.CallActivityAsync<string>("SayHelloActivity", "Durable");
        return $"Orchestration result: {result}";
    }

    // Activity - unit of work
    [Function("SayHelloActivity")]
    public static string SayHelloActivity([ActivityTrigger] string name)
    {
        return $"Hello, {name}!";
    }

    // Client (HTTP starter)
    [Function("HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] IDurableClient starter)
    {
        // start a new orchestration
        string instanceId = await starter.StartNewAsync("Orchestrator", null);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        // Link to status endpoints (durable client provides status URLs)
        var status = starter.CreateCheckStatusResponse(req, instanceId);
        // Return the URL(s) in the response body (or headers) as JSON:
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            instanceId,
            statusQueryGetUri = status.Headers.Location?.ToString()
        }));
        return response;
    }
}
