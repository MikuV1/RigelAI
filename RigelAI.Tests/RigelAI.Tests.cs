using System.Threading.Tasks;
using Xunit;
using RigelAI.Core;

public class GeminiClientTests
{
    [Fact]
    public async Task ChatAsync_NoApiKey_ReturnsError()
    {
        var originalKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);

        var response = await GeminiClient.ChatAsync("Hello");

        System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", originalKey);

        Assert.Contains("API key missing", response);
    }

    [Fact]
    public async Task ResetChat_ClearsHistory()
    {
        await GeminiClient.ChatAsync("Test message");
        Assert.True(GeminiClient.ChatHistoryCount > 0);

        GeminiClient.ResetChat();

        Assert.Equal(0, GeminiClient.ChatHistoryCount);
    }
}
