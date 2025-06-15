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

            if (history.Count >= 160)
            {
                var toSummarize = history.GetRange(1, 40);
                var summary = await KangSummary.SummarizeAsync(toSummarize);

                history.RemoveRange(1, 40);
                history.Insert(1, new
                {
                    role = "user",
                    parts = new[] { new { text = summary } }
                });

                Console.WriteLine($"[RigelChatService] Summarized 40 oldest messages for user {userId}.");
            }

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

        public async Task<string> GetGroupResponseAsync(long groupId, long userId, string userMessage, string senderName)
        {
            var groupHistory = GetOrCreateGroupHistory(groupId);
            var userHistory = GetOrCreateUserHistory(userId);

            if (groupHistory.Count >= 160)
            {
                var toSummarize = groupHistory.GetRange(1, 40);
                var summary = await KangSummary.SummarizeAsync(toSummarize);

                groupHistory.RemoveRange(1, 40);
                groupHistory.Insert(1, new
                {
                    role = "user",
                    parts = new[] { new { text = summary } }
                });

                Console.WriteLine($"[RigelChatService] Summarized 40 oldest group messages for group {groupId}.");
            }

            groupHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"[{senderName}]: {userMessage}" } }
            });

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
            var history = new List<object>();
            if (!string.IsNullOrWhiteSpace(personaText))
            {
                history.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = personaText } }
                });
            }
            UserConversations[userId] = history;

            Console.WriteLine($"[RigelChatService] Reset user history for {userId} (with persona re-injected).");
        }

        public void ResetGroupHistory(long groupId)
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
            GroupConversations[groupId] = history;

            Console.WriteLine($"[RigelChatService] Reset group history for {groupId} (with persona re-injected).");
        }
    }
}
