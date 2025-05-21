using System;
using System.Text;
using System.Threading.Tasks;
using RigelAI.Core;

namespace RigelAI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Welcome to Rigel AI (Gemini Mode)");
            Console.WriteLine("Type 'exit' to quit.\n");

            bool loaded = await GeminiClient.LoadPersonaAsync();
            if (!loaded) return;

            GeminiClient.ResetChat();

            while (true)
            {
                var userInput = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string response = await GeminiClient.ChatAsync(userInput);
                Console.WriteLine(response);
            }
        }
    }
}
