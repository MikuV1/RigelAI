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
            var history = UserConversations.GetOrAdd(userId, _ => new List<object>());

            // Inject persona only on first interaction
            if (history.Count == 0 && !string.IsNullOrWhiteSpace(personaText))
            {
                history.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = personaText } }
                });
            }

            return await GeminiClient.ChatAsync(userMessage, history);
        }

        public void ResetUserHistory(long userId)
        {
            UserConversations[userId] = new List<object>();
        }
    }
}
