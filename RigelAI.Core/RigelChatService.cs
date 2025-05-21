using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class RigelChatService
    {
        public async Task<bool> InitializeAsync()
        {
            // Load persona, returns true if successful
            return await GeminiClient.LoadPersonaAsync();
        }

        public async Task<string> GetResponseAsync(string userMessage)
        {
            // Pass the message to GeminiClient and get response
            return await GeminiClient.ChatAsync(userMessage);
        }

        public void Reset()
        {
            GeminiClient.ResetChat();
        }
    }
}
