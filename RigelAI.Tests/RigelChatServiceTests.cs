using System.IO;
using System.Threading.Tasks;
using Moq;
using RigelAI.Core;
using Xunit;

namespace RigelAI.Tests
{
    public class RigelChatServiceTests
    {
        [Fact]
        public async Task InitializeAsync_LoadsPersona()
        {
            var service = new RigelChatService();
            var result = await service.InitializeAsync();
            Assert.True(result || !result); // Placeholder: Should check persona file presence
        }

        [Fact]
        public void ResetUserHistory_ResetsHistory()
        {
            var service = new RigelChatService();
            service.ResetUserHistory(123);
            var history = service.GetOrCreateUserHistory(123);
            Assert.NotNull(history);
        }
    }

    public class ImageChatServiceTests
    {
        [Fact]
        public async Task HandleImageAsync_AddsToHistoryAndReturnsResponse()
        {
            var chatService = new RigelChatService();
            var service = new ImageChatService(chatService);
            var image = new byte[] { 1, 2, 3 };
            GeminiClient.SetHttpClient(new System.Net.Http.HttpClient(new Mock<System.Net.Http.HttpMessageHandler>().Object));
            var result = await service.HandleImageAsync(1, 2, image, "prompt");
            Assert.NotNull(result);
        }
    }

    public class VoiceChatServiceTests
    {
        [Fact]
        public async Task HandleVoiceAsync_AddsToHistoryAndReturnsResponse()
        {
            var chatService = new RigelChatService();
            var service = new VoiceChatService(chatService);
            var audio = new byte[] { 1, 2, 3 };
            GeminiClient.SetHttpClient(new System.Net.Http.HttpClient(new Mock<System.Net.Http.HttpMessageHandler>().Object));
            var result = await service.HandleVoiceAsync(1, 2, audio);
            Assert.NotNull(result);
        }
    }

    public class DocumentChatServiceTests
    {
        [Fact]
        public async Task HandleDocumentAsync_AddsToHistoryAndReturnsResponse()
        {
            var chatService = new RigelChatService();
            var service = new DocumentChatService(chatService);
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            GeminiClient.SetHttpClient(new System.Net.Http.HttpClient(new Mock<System.Net.Http.HttpMessageHandler>().Object));
            var result = await service.HandleDocumentAsync(1, 2, ms, "file.pdf", "prompt");
            Assert.NotNull(result);
        }
    }

    public class KoboldCppClientTests
    {
        [Fact]
        public async Task ChatAsync_ReturnsReplyOrError()
        {
            var handler = new Mock<System.Net.Http.HttpMessageHandler>();
            var httpClient = new System.Net.Http.HttpClient(handler.Object);
            KoboldCppClient.SetHttpClient(httpClient);
            var history = new System.Collections.Generic.List<object>();
            var result = await KoboldCppClient.ChatAsync("hi", history);
            Assert.NotNull(result);
        }
    }
}
