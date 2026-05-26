#!/usr/bin/env dotnet run
#:package Microsoft.Extensions.AI.OpenAI@*
#:package OpenAI@*
#:package Microsoft.Extensions.Logging.Abstractions@*
#:property PublishAot=false

#pragma warning disable OPENAI001

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;

// ============================================================
// 1. Define the mock API response payload (Responses API format)
// ============================================================
string mockResponsePayload = """
{
  "id": "resp_mock123",
  "object": "response",
  "created_at": 1741891428,
  "status": "completed",
  "error": null,
  "incomplete_details": null,
  "instructions": null,
  "max_output_tokens": 20,
  "model": "gpt-4o-mini-2024-07-18",
  "output": [
    {
      "type": "message",
      "id": "msg_mock456",
      "status": "completed",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Hello! I'm a mocked OpenAI Responses API reply via MEAI.",
          "annotations": []
        }
      ]
    }
  ],
  "parallel_tool_calls": true,
  "previous_response_id": null,
  "reasoning": {
    "effort": null,
    "generate_summary": null
  },
  "store": true,
  "temperature": 0.5,
  "text": {
    "format": {
      "type": "text"
    }
  },
  "tool_choice": "auto",
  "tools": [],
  "top_p": 1.0,
  "usage": {
    "input_tokens": 26,
    "input_tokens_details": {
      "cached_tokens": 0
    },
    "output_tokens": 14,
    "output_tokens_details": {
      "reasoning_tokens": 0
    },
    "total_tokens": 40
  },
  "user": null,
  "metadata": {}
}
""";

// ============================================================
// 2. Create the VerbatimHttpHandler with logging
// ============================================================
var handler = new VerbatimHttpHandler(mockResponsePayload);
using var httpClient = new HttpClient(handler);

// ============================================================
// 3. Set up the client using the Responses API endpoint
// ============================================================
var openAIClient = new OpenAIClient(
    new ApiKeyCredential("fake-key"),
    new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) });
IChatClient client = openAIClient.GetResponsesClient().AsIChatClient("gpt-4o-mini");

// ============================================================
// 4. Execute the scenario and print the result
// ============================================================
var response = await client.GetResponseAsync("Hello, how are you?", new()
{
    MaxOutputTokens = 20,
    Temperature = 0.5f,
});
Console.WriteLine($"✅ OpenAI Responses API Mock Success!");
Console.WriteLine($"   Response: {response.Text}");
Console.WriteLine($"   Model: {response.ModelId}");
Console.WriteLine($"   Tokens: {response.Usage?.TotalTokenCount}");
Console.WriteLine($"   ResponseId: {response.ResponseId}");

// ============================================================
// VerbatimHttpHandler — logs all request/response details
// ============================================================
class VerbatimHttpHandler(string responsePayload) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine("=== REQUEST ===");
        Console.WriteLine($"{request.Method} {request.RequestUri}");
        foreach (var header in request.Headers)
            Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        if (request.Content is not null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"  Content-Type: {request.Content.Headers.ContentType}");
            Console.WriteLine("--- Request Body ---");
            Console.WriteLine(requestBody);
        }
        Console.WriteLine();

        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responsePayload, Encoding.UTF8, "application/json")
        };

        Console.WriteLine("=== RESPONSE ===");
        Console.WriteLine($"  Status: {resp.StatusCode}");
        Console.WriteLine("--- Response Body ---");
        Console.WriteLine(responsePayload);
        Console.WriteLine();

        return resp;
    }
}
