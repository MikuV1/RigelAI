using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class RigelChatService
    {
        private static readonly ConcurrentDictionary<long, List<object>> UserConversations = new();
        private string personaText = "";

        public async Task<bool> InitializeAsync(string personaFilePath = "persona.txt")
        {
            bool loaded = await PersonaManager.LoadPersonaAsync(personaFilePath);
            if (loaded)
            {
                personaText = PersonaManager.GetPersonaText();
            }
            return loaded;
        }

        public async Task<string> GetResponseAsync(long userId, string userMessage)
        {
            var history = GetUserHistory(userId);

            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var reply = await GeminiClient.ChatAsync(userMessage, history);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                history.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
            }

            return reply;
        }

        public async Task<string> GetResponseFromHistoryAsync(long userId, List<object> updatedHistory)
        {
            UserConversations[userId] = updatedHistory;

            var reply = await GeminiClient.ChatWithPartsAsync(updatedHistory);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                updatedHistory.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
            }

            return reply;
        }

        public List<object> GetUserHistory(long userId)
        {
            return UserConversations.GetOrAdd(userId, id =>
            {
                var history = new List<object>();
                if (!string.IsNullOrWhiteSpace(personaText))
                {
                    history.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = personaText } }
                    });
                }
                return history;
            });
        }

        public void ResetUserHistory(long userId)
        {
            UserConversations[userId] = new List<object>();
        }
    }
}
