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

            // Add image (as base64) and prompt to both histories
            groupHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt }, new { text = Convert.ToBase64String(imageData) } }
            });
            userHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt }, new { text = Convert.ToBase64String(imageData) } }
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
