# 🤖 RigelAI — Multimodal Telegram Bot with Gemini API

RigelAI is a C#-based Telegram bot powered by Google's Gemini LLM, capable of handling:
- 💬 Natural language conversations
- 🖼️ Image understanding (`/image`)
- 🔉 Voice messages
- 📄 Document summarization (`/file`)
- 🧠 Per-user memory with persona
- 🚀 Fully modular and extensible architecture

---

## 🛠️ Features

- ✅ Gemini Integration
- ✅ Telegram.Bot 22.5.1 compatible
- ✅ Modular services: `RigelChatService`, `ImageChatService`, `VoiceChatService`, `DocumentChatService`
- ✅ Automatic persona injection
- ✅ Per-user conversation memory
- ✅ .NET 9.0 multi-project solution
- ✅ Unit tests for GeminiClient

---

## 🧱 Project Structure

```
RigelAI/
├── RigelAI.Core/           # Core services and Gemini integration
├── RigelAI.ConsoleApp/     # Console interface for local testing
├── RigelAI.TelegramBot/    # Telegram bot entry point and router
├── RigelAI.Tests/          # xUnit test coverage
└── persona.txt             # Customizable personality file
```

---

## ⚙️ Environment Variables

Create a `.env` or configure environment variables:

| Variable                    | Purpose                      |
|----------------------------|------------------------------|
| `GEMINI_API_KEY`           | Your Gemini API key          |
| `RIGEL_TELEGRAM_BOT_TOKEN` | Your Telegram Bot token      |

---

## 🧪 How to Run

### Console App

```bash
cd RigelAI.ConsoleApp
dotnet run
```

### Telegram Bot

```bash
cd RigelAI.TelegramBot
dotnet run
```

---

## 🗃️ Supported Commands

| Command      | Input Type        | Description                         |
|--------------|-------------------|-------------------------------------|
| Just text    | Text              | Regular conversation                |
| `/image`     | Photo + Caption   | Gemini image understanding          |
| `/file`      | Doc/PDF + Caption | Summarize uploaded documents        |
| Voice        | Voice message     | Gemini-based audio understanding    |

---

## 📌 Future Plans

- ✂️ Chat history summarization & trimming  
- 🔊 Whisper voice transcription  
- 🧠 Persistent memory layer  
- 🌍 Deployment-ready packaging  

---

## 🧑‍💻 Credits

Built with ❤️ by MikuV1 using:
- [.NET 9.0](https://dotnet.microsoft.com)
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
- [Gemini LLM API](https://ai.google.dev/)
