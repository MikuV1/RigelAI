using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class RigelChatService
    {
        private static readonly ConcurrentDictionary<long, List<object>> UserConversations = new();
        private static readonly ConcurrentDictionary<long, List<object>> GroupConversations = new();
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

        public async Task<string> GetPrivateResponseAsync(long userId, string userMessage)
        {
            var history = GetOrCreateUserHistory(userId);

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

        public async Task<string> GetGroupResponseAsync(long groupId, long userId, string userMessage)
        {
            var groupHistory = GetOrCreateGroupHistory(groupId);
            var userHistory = GetOrCreateUserHistory(userId);

            // Add to group history
            groupHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            // Also save to personal user history
            userHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var reply = await GeminiClient.ChatAsync(userMessage, groupHistory);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                groupHistory.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
                userHistory.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
            }

            return reply;
        }

        public List<object> GetOrCreateGroupHistory(long groupId)
        {
            // Only inject persona once, when group history is first created
            return GroupConversations.GetOrAdd(groupId, id =>
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

        public List<object> GetOrCreateUserHistory(long userId)
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
