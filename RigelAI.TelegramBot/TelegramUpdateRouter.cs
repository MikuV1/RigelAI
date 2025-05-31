using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RigelAI.Core;

namespace RigelAI.TelegramBot
{
    public class TelegramUpdateRouter
    {
        private readonly ITelegramBotClient _botClient;
        private readonly string _botToken;
        private readonly RigelChatService _chatService;
        private readonly ImageChatService _imageService;
        private readonly VoiceChatService _voiceService;
        private readonly DocumentChatService _docService;
        private readonly HttpClient _httpClient;

        public TelegramUpdateRouter(
            ITelegramBotClient botClient,
            string botToken,
            RigelChatService chatService,
            ImageChatService imageService,
            VoiceChatService voiceService,
            DocumentChatService docService,
            HttpClient httpClient)
        {
            _botClient = botClient;
            _botToken = botToken;
            _chatService = chatService;
            _imageService = imageService;
            _voiceService = voiceService;
            _docService = docService;
            _httpClient = httpClient;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            var userId = message.From?.Id ?? 0;
            var chatId = message.Chat.Id;
            var chatType = message.Chat.Type;

            string response = null;
            bool shouldRespond = chatType == ChatType.Private;

            // Group detection logic
            if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
            {
                var text = message.Text ?? message.Caption ?? "";

                if (text.StartsWith("!ar ", StringComparison.OrdinalIgnoreCase))
                {
                    shouldRespond = true;
                    text = text.Substring(4).Trim();
                }
                else if (message.Entities != null)
                {
                    foreach (var entity in message.Entities)
                    {
                        if (entity.Type == MessageEntityType.Mention &&
                            message.Text.Substring(entity.Offset, entity.Length)
                                   .Equals($"@{(await _botClient.SendRequest(new GetMeRequest(), cancellationToken)).Username}", StringComparison.OrdinalIgnoreCase))
                        {
                            shouldRespond = true;
                            break;
                        }
                    }
                }
                else if (message.ReplyToMessage != null)
                {
                    var me = await _botClient.SendRequest(new GetMeRequest(), cancellationToken);
                    if (message.ReplyToMessage.From?.Id == me.Id)
                    {
                        shouldRespond = true;
                    }
                }

                message.Text = text;
            }

            if (!shouldRespond)
                return;

            var messageText = message.Text ?? message.Caption;

            // 1️⃣ Voice
            if (message.Voice != null)
            {
                var file = await _botClient.SendRequest(
                    new GetFileRequest { FileId = message.Voice.FileId },
                    cancellationToken);
                var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                var audioBytes = await _httpClient.GetByteArrayAsync(fileUrl);

                response = await _voiceService.HandleVoiceAsync(chatId, userId, audioBytes);
            }
            // 2️⃣ Image
            else if (!string.IsNullOrWhiteSpace(messageText) &&
                     messageText.StartsWith("/image", StringComparison.OrdinalIgnoreCase) &&
                     message.Photo != null)
            {
                var prompt = messageText.Substring(6).Trim();
                var photo = message.Photo[^1];
                var file = await _botClient.SendRequest(
                    new GetFileRequest { FileId = photo.FileId },
                    cancellationToken);
                var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                var imageBytes = await _httpClient.GetByteArrayAsync(fileUrl);

                response = await _imageService.HandleImageAsync(chatId, userId, imageBytes, prompt);
            }
            // 3️⃣ Document
            else if (!string.IsNullOrWhiteSpace(messageText) &&
                     messageText.StartsWith("/file", StringComparison.OrdinalIgnoreCase) &&
                     message.Document != null)
            {
                var prompt = messageText.Substring(5).Trim();
                var file = await _botClient.SendRequest(
                    new GetFileRequest { FileId = message.Document.FileId },
                    cancellationToken);
                var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                var fileStream = await _httpClient.GetStreamAsync(fileUrl);

                response = await _docService.HandleDocumentAsync(chatId, userId, fileStream, message.Document.FileName, prompt);
            }
            // 4️⃣ Fallback for plain text (like !ar hello!)
            else if (!string.IsNullOrWhiteSpace(messageText))
            {
                if (chatType == ChatType.Private)
                {
                    response = await _chatService.GetPrivateResponseAsync(userId, messageText);
                }
                else
                {
                    response = await _chatService.GetGroupResponseAsync(chatId, userId, messageText);
                }
            }

            // 5️⃣ Send the response (in the correct topic, if any)
            if (!string.IsNullOrWhiteSpace(response))
            {
                await _botClient.SendRequest(
                    new SendMessageRequest
                    {
                        ChatId = chatId,
                        MessageThreadId = message.MessageThreadId, // 🟩 ensure reply in same topic
                        Text = response
                    },
                    cancellationToken
                );
                Console.WriteLine($"[Bot] Responded to {userId} in chat {chatId} (thread {message.MessageThreadId ?? 0}): {response}");
            }
        }
    }
}
