using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class DocumentChatService
    {
        private readonly RigelChatService _chatService;

        public DocumentChatService(RigelChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> HandleDocumentAsync(long groupId, long userId, Stream documentStream, string fileName, string prompt)
        {
            using var memoryStream = new MemoryStream();
            await documentStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var groupHistory = _chatService.GetOrCreateGroupHistory(groupId);
            var userHistory = _chatService.GetOrCreateUserHistory(userId);

            groupHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt }, new { text = $"Document: {fileName}" }, new { text = Convert.ToBase64String(fileBytes) } }
            });
            userHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt }, new { text = $"Document: {fileName}" }, new { text = Convert.ToBase64String(fileBytes) } }
            });

            var response = await GeminiClient.ChatWithPartsAsync(groupHistory);

            groupHistory.Add(new
            {
                role = "model",
                parts = new[] { new { text = response } }
            });
            userHistory.Add(new
            {
                role = "model",
                parts = new[] { new { text = response } }
            });

            return response;
        }
    }
}
