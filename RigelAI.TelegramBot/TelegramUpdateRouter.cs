using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
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
            string response = null;

            // Read message text or caption
            var text = message.Text ?? message.Caption;

            // 1. Auto-handle voice blobs
            if (message.Voice != null)
            {
                var file = await _botClient.SendRequest(
                    new GetFileRequest { FileId = message.Voice.FileId },
                    cancellationToken);
                var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                var audioBytes = await _httpClient.GetByteArrayAsync(fileUrl);

                response = await _voiceService.HandleVoiceAsync(userId, audioBytes);
            }

            // 2. Handle prefixed commands (in text or caption)
            else if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();

                if (text.StartsWith("/image", StringComparison.OrdinalIgnoreCase) && message.Photo != null)
                {
                    var prompt = text.Substring(6).Trim();
                    var photo = message.Photo[^1];
                    var file = await _botClient.SendRequest(
                        new GetFileRequest { FileId = photo.FileId },
                        cancellationToken);
                    var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                    var imageBytes = await _httpClient.GetByteArrayAsync(fileUrl);

                    response = await _imageService.HandleImageAsync(userId, imageBytes, prompt);
                }
                else if (text.StartsWith("/voice", StringComparison.OrdinalIgnoreCase) && message.Audio != null)
                {
                    var prompt = text.Substring(6).Trim();
                    var file = await _botClient.SendRequest(
                        new GetFileRequest { FileId = message.Audio.FileId },
                        cancellationToken);
                    var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                    var audioBytes = await _httpClient.GetByteArrayAsync(fileUrl);

                    response = await _voiceService.HandleVoiceAsync(userId, audioBytes);
                }
                else if (text.StartsWith("/file", StringComparison.OrdinalIgnoreCase) && message.Document != null)
                {
                    var prompt = text.Substring(5).Trim();
                    var file = await _botClient.SendRequest(
                        new GetFileRequest { FileId = message.Document.FileId },
                        cancellationToken);
                    var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";
                    var fileStream = await _httpClient.GetStreamAsync(fileUrl);

                    response = await _docService.HandleDocumentAsync(userId, fileStream, message.Document.FileName, prompt);
                }
                else
                {
                    response = await _chatService.GetResponseAsync(userId, text);
                }
            }

            // 3. Send the response (if available)
            if (!string.IsNullOrWhiteSpace(response))
            {
                await _botClient.SendRequest(
                    new SendMessageRequest
                    {
                        ChatId = chatId,
                        Text = response
                    },
                    cancellationToken
                );
            }
        }
    }
}
