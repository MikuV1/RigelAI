using System;
using System.Threading.Tasks;
using RigelAI.Core;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔹 RigelAI Console Chatbot 🔹");

        var chatService = new RigelChatService();
        bool personaLoaded = await chatService.InitializeAsync();

        if (!personaLoaded)
        {
            Console.WriteLine("❌ Failed to load persona.txt.");
        }
        else
        {
            Console.WriteLine("✅ Persona loaded successfully.");
        }

        Console.WriteLine("Type your message below. Type 'reset' to clear history. Type 'exit' to quit.");

        long userId = 0; // Default single-user ID for console app

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                chatService.ResetUserHistory(userId);
                Console.WriteLine("🌀 Chat history reset.");
                continue;
            }

            string response = await chatService.GetResponseAsync(userId, input);
            Console.WriteLine($"🤖 {response}");
        }

        Console.WriteLine("👋 Goodbye!");
    }
}
