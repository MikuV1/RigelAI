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

        public async Task<string> HandleDocumentAsync(long groupId, long userId, Stream fileStream, string fileName, string prompt)
        {
            var groupHistory = _chatService.GetOrCreateGroupHistory(groupId);
            var userHistory = _chatService.GetOrCreateUserHistory(userId);

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            string base64File = Convert.ToBase64String(memoryStream.ToArray());

            var multimodalPart = new
            {
                role = "user",
                parts = new object[]
                {
                    new {
                        inlineData = new {
                            mimeType = "application/pdf",
                            data = base64File
                        }
                    },
                    new {
                        text = prompt
                    }
                }
            };

            groupHistory.Add(multimodalPart);
            userHistory.Add(multimodalPart);

            var response = await GeminiClient.ChatWithPartsAsync(groupHistory);

            var modelPart = new
            {
                role = "model",
                parts = new[] { new { text = response } }
            };
            groupHistory.Add(modelPart);
            userHistory.Add(modelPart);

            return response;
        }
    }
}
