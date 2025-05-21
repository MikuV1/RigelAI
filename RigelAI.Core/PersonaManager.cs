using System;
using System.IO;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public static class PersonaManager
    {
        private static string? personaText;

        // Load the persona text from file asynchronously
        public static async Task<bool> LoadPersonaAsync(string filePath = "persona.txt")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Persona file not found at: {filePath}");
                    return false;
                }

                personaText = await File.ReadAllTextAsync(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error loading persona: " + ex.Message);
                return false;
            }
        }

        // Get the loaded persona text
        public static string GetPersonaText()
        {
            return personaText ?? string.Empty;
        }
    }
}
