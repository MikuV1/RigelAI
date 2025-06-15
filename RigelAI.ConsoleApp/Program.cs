using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RigelAI.Core;

namespace RigelAI.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("🧪 Testing KoboldCpp OpenAI API chat");
            Console.WriteLine("Type 'exit' to quit.\n");

            var conversationHistory = new List<object>();

            // Optional: inject a system prompt to shape behavior
            conversationHistory.Add(new
            {
                role = "system",
                content = "you are an friend that always so friendly"
            });

            while (true)
            {
                Console.Write("You: ");
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string reply = await KoboldCppClient.ChatAsync(input, conversationHistory);
                Console.WriteLine($"\n {reply}\n");
            }
        }
    }
}
