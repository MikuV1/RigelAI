using System.Collections.Generic;
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
    public void ResetChat_ClearsHistory()
    {
        // Clear chat first
        GeminiClient.ResetChat();

        // Use reflection to get private conversationHistory list
        var conversationHistoryField = typeof(GeminiClient).GetField("conversationHistory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var conversationHistory = conversationHistoryField?.GetValue(null) as List<object>;
        Assert.NotNull(conversationHistory);

        // Add dummy message to simulate chat history
        conversationHistory.Add(new
        {
            role = "user",
            parts = new[] { new { text = "Test message" } }
        });

        // Verify chat history has at least one entry
        Assert.True(GeminiClient.ChatHistoryCount > 0);

        // Reset and verify cleared
        GeminiClient.ResetChat();
        Assert.Equal(0, GeminiClient.ChatHistoryCount);
    }
}
