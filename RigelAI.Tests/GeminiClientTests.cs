using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using RigelAI.Core;
using Xunit;

public class GeminiClientTests
{
    [Fact]
    public async Task ChatAsync_ReturnsBotReply_AndUpdatesHistory()
    {
        // Arrange
        var userMessage = "Hello, bot!";
        var conversationHistory = new List<object>();

        // Fake Gemini API JSON response
        var fakeResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = "Hi, human!" }
                        }
                    }
                }
            }
        };
        var fakeJson = JsonConvert.SerializeObject(fakeResponse);

        // Mock HttpMessageHandler to fake HTTP response
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJson)
            });

        var httpClient = new HttpClient(handlerMock.Object);

        // Inject mocked HttpClient into GeminiClient
        GeminiClient.SetHttpClient(httpClient);

        // Set a dummy API key environment variable for test
        System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", "dummy-key");

        // Act
        var botReply = await GeminiClient.ChatAsync(userMessage, conversationHistory);

        // Assert returned reply is as expected
        Assert.Equal("Hi, human!", botReply);

        // Assert user message was added to conversation history
        Assert.Contains(conversationHistory, item =>
        {
            dynamic msg = item;
            try
            {
                return msg.role == "user" && msg.parts[0].text == userMessage;
            }
            catch
            {
                return false;
            }
        });

        // Assert bot reply was added to conversation history
        Assert.Contains(conversationHistory, item =>
        {
            dynamic msg = item;
            try
            {
                return msg.role == "model" && msg.parts[0].text == "Hi, human!";
            }
            catch
            {
                return false;
            }
        });
    }

    [Fact]
    public async Task ChatAsync_ReturnsErrorMessage_WhenApiKeyMissing()
    {
        // Arrange
        var userMessage = "Test message";
        var conversationHistory = new List<object>();

        // Remove API key environment variable
        System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);

        // Act
        var result = await GeminiClient.ChatAsync(userMessage, conversationHistory);

        // Assert
        Assert.Equal("❌ API key missing.", result);
    }

    [Fact]
    public async Task ChatAsync_ReturnsErrorMessage_OnHttpFailure()
    {
        // Arrange
        var userMessage = "Hi";
        var conversationHistory = new List<object>();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        GeminiClient.SetHttpClient(httpClient);

        System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", "dummy-key");

        // Act
        var result = await GeminiClient.ChatAsync(userMessage, conversationHistory);

        // Assert the error message includes status code
        Assert.Contains("❌ Gemini API error:", result);
    }
}
