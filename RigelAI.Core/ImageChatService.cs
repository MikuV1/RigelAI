using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RigelAI.Core;

namespace RigelAI.Core
{
    public class ImageChatService
    {
        private readonly RigelChatService _rigelChatService;

        public ImageChatService(RigelChatService rigelChatService)
        {
            _rigelChatService = rigelChatService;
        }

        public async Task<string> HandleImageAsync(long userId, byte[] imageBytes, string userPrompt)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "❌ Image data is empty.";

            string base64Image = Convert.ToBase64String(imageBytes);

            var imagePart = new
            {
                inlineData = new
                {
                    mimeType = "image/png", // Change this if the format is different
                    data = base64Image
                }
            };

            var conversationHistory = _rigelChatService.GetUserHistory(userId);

            conversationHistory.Add(new
            {
                role = "user",
                parts = new object[]
                {
                    new { text = userPrompt },
                    imagePart
                }
            });

            return await _rigelChatService.GetResponseFromHistoryAsync(userId, conversationHistory);
        }
    }
}