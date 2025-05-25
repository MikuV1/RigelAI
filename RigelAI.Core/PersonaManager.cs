using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public static class PersonaManager
    {
        private static string? personaText;

        public static async Task<bool> LoadPersonaAsync(string fileName = "persona.txt")
        {
            try
            {
                var corePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var candidatePath = Path.Combine(corePath ?? ".", fileName);

                if (!File.Exists(candidatePath))
                {
                    candidatePath = Path.Combine(AppContext.BaseDirectory, fileName);
                }

                if (!File.Exists(candidatePath))
                {
                    Console.WriteLine($"❌ Persona file not found at: {candidatePath}");
                    return false;
                }

                personaText = await File.ReadAllTextAsync(candidatePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error loading persona: " + ex.Message);
                return false;
            }
        }

        public static string GetPersonaText()
        {
            return personaText ?? string.Empty;
        }
    }
}
