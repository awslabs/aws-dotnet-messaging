using System.Net.Http;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using static System.Runtime.InteropServices.JavaScript.JSType;

var  HttpClient _httpClient = new HttpClient();
var  _apiEndpoint = "https://[your-api-url]/Publisher/chatmessage";


// The function handler that will be called for each Lambda event
var handler = (string input, ILambdaContext context) => {
    // Parse input
    var message = new ChatMessage
    {
        MessageDescription = request.Body
    };

    // Send to PublisherAPI
    var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, message);

    // Return result
    return new APIGatewayProxyResponse
    {
        StatusCode = (int)response.StatusCode,
        Body = await response.Content.ReadAsStringAsync()
    };


};

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();
