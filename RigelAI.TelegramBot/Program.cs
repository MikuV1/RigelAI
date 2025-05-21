using System;
using System.Threading;
using System.Threading.Tasks;
using RigelAI.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    static async Task Main()
    {
        var token = Environment.GetEnvironmentVariable("RIGEL_TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Bot token not set in environment variables.");
            return;
        }

        var botClient = new TelegramBotClient(token);
        using var cts = new CancellationTokenSource();

        var chatService = new RigelChatService();
        bool personaLoaded = await chatService.InitializeAsync();
        if (!personaLoaded)
        {
            Console.WriteLine("❌ Failed to load persona.txt.");
        }

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token);

        var me = await botClient.SendRequest<Telegram.Bot.Types.User>(new Telegram.Bot.Requests.GetMeRequest(), cts.Token);
        Console.WriteLine($"Bot started: @{me.Username}");

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();

        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { Text: { } messageText })
                return;

            var chatId = update.Message.Chat.Id;
            Console.WriteLine($"Received from {chatId}: {messageText}");

            string response = await chatService.GetResponseAsync(chatId, messageText); ;

            await client.SendRequest(
                new Telegram.Bot.Requests.SendMessageRequest
                {
                    ChatId = chatId,
                    Text = response
                },
                cancellationToken);
        }

        Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Telegram Bot Error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
