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

        public async Task<string> HandleVoiceAsync(long userId, byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                return "❌ Voice data is empty.";

            string base64Audio = Convert.ToBase64String(audioBytes);

            var conversation = _chatService.GetUserHistory(userId);

            conversation.Add(new
            {
                role = "user",
                parts = new object[]
                {
                    new
                    {
                        inline_data = new
                        {
                            mime_type = "audio/ogg",
                            data = base64Audio
                        }
                    }
                }
            });

            return await _chatService.GetResponseFromHistoryAsync(userId, conversation);
        }
    }
}
