using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class ImageChatService
    {
        private readonly RigelChatService _chatService;

        public ImageChatService(RigelChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> HandleImageAsync(long groupId, long userId, byte[] imageData, string prompt)
        {
            var groupHistory = _chatService.GetOrCreateGroupHistory(groupId);
            var userHistory = _chatService.GetOrCreateUserHistory(userId);

            string base64Image = Convert.ToBase64String(imageData);

            // Gemini expects inlineData for images
            var multimodalPart = new
            {
                role = "user",
                parts = new object[]
                {
                    new {
                        inlineData = new {
                            mimeType = "image/jpeg",
                            data = base64Image
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
