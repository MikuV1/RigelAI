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

            string base64Audio = Convert.ToBase64String(voiceData);

            var multimodalPart = new
            {
                role = "user",
                parts = new object[]
                {
                    new {
                        inlineData = new {
                            mimeType = "audio/ogg",
                            data = base64Audio
                        }
                    },
                    new {
                        text = "Please transcribe or respond to this voice message."
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
