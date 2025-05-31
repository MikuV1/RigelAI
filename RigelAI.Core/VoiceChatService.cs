using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class VoiceChatService
    {
        private readonly RigelChatService _chatService;

        public VoiceChatService(RigelChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> HandleVoiceAsync(long groupId, long userId, byte[] voiceData)
        {
            var groupHistory = _chatService.GetOrCreateGroupHistory(groupId);
            var userHistory = _chatService.GetOrCreateUserHistory(userId);

            groupHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = Convert.ToBase64String(voiceData) } }
            });
            userHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = Convert.ToBase64String(voiceData) } }
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
