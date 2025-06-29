using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RigelAI.Core;
using RigelAI.TelegramBot;
using DotNetEnv;

class Program
{
    public static TelegramBotClient BotClient { get; private set; }

    static async Task Main()
    {
        // Centralized .env loading
        try
        {
            DotNetEnv.Env.Load();
            Console.WriteLine("✅ .env loaded successfully in TelegramBot Program.Main.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to load .env in TelegramBot Program.Main: {ex.Message}");
        }

        var token = Environment.GetEnvironmentVariable("RIGEL_TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("❌ Bot token not set in environment variables.");
            return;
        }

        BotClient = new TelegramBotClient(token);
        using var cts = new CancellationTokenSource();
        var httpClient = new HttpClient();

        // Initialize core services
        var chatService = new RigelChatService();
        await chatService.InitializeAsync();

        var imageService = new ImageChatService(chatService);
        var voiceService = new VoiceChatService(chatService);
        var docService = new DocumentChatService(chatService);

        // Initialize update router
        var router = new TelegramUpdateRouter(BotClient, token, chatService, imageService, voiceService, docService, httpClient);

        // Start receiving updates
        BotClient.StartReceiving(
            updateHandler: async (bot, update, token) =>
            {
                if (update.Message != null)
                {
                    Console.WriteLine("🔔 Update received:");
                    Console.WriteLine($"  - ChatId: {update.Message.Chat.Id}");
                    Console.WriteLine($"  - ChatType: {update.Message.Chat.Type}");
                    Console.WriteLine($"  - From: {update.Message.From?.Username ?? "unknown"} ({update.Message.From?.Id})");
                    Console.WriteLine($"  - Text/Caption: {update.Message.Text ?? update.Message.Caption}");
                }

                await router.HandleUpdateAsync(update, token);
            },
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Receive all
            },
            cancellationToken: cts.Token
        );

        var me = await BotClient.SendRequest(new Telegram.Bot.Requests.GetMeRequest(), cts.Token);
        Console.WriteLine($"🤖 Bot started: @{me.Username}");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        cts.Cancel();
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"❌ Telegram Bot Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
