using System;
using System.Collections.Concurrent;
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

        private static readonly ConcurrentDictionary<long, string> UserNames = new();

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
            var text = message.Text ?? message.Caption ?? "";

            UserNames[userId] = GetBestDisplayName(message.From);

            if (text.Trim().Equals("/reset", StringComparison.OrdinalIgnoreCase) && chatType == ChatType.Private)
            {
                _chatService.ResetUserHistory(userId);
                await _botClient.SendRequest(new SendMessageRequest { ChatId = chatId, Text = "🔄 Your personal history has been reset!" }, cancellationToken);
                return;
            }
            else if (text.Trim().Equals("/resetgroup", StringComparison.OrdinalIgnoreCase) && chatType is ChatType.Group or ChatType.Supergroup)
            {
                _chatService.ResetGroupHistory(chatId);
                await _botClient.SendRequest(new SendMessageRequest { ChatId = chatId, Text = "🔄 Group history has been reset!" }, cancellationToken);
                return;
            }

            string response = null;
            bool shouldRespond = chatType == ChatType.Private;

            bool isFileCommand = text.StartsWith("/image", StringComparison.OrdinalIgnoreCase)
                              || text.StartsWith("/file", StringComparison.OrdinalIgnoreCase)
                              || text.StartsWith("/voice", StringComparison.OrdinalIgnoreCase);

            if (chatType is ChatType.Group or ChatType.Supergroup)
            {
                var me = await _botClient.SendRequest(new GetMeRequest(), cancellationToken);

                if (!isFileCommand)
                {
                    if (text.StartsWith("!ar ", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldRespond = true;
                        text = text.Substring(4).Trim();
                    }

                    // ✅ Respond if message mentions the bot (even in replies)
                    if (!shouldRespond && message.Entities != null)
                    {
                        foreach (var entity in message.Entities)
                        {
                            if (entity.Type == MessageEntityType.Mention &&
                                message.Text.Substring(entity.Offset, entity.Length)
                                    .Equals($"@{me.Username}", StringComparison.OrdinalIgnoreCase))
                            {
                                shouldRespond = true;
                                break;
                            }
                        }
                    }

                    // ✅ Respond if reply to bot
                    if (!shouldRespond && message.ReplyToMessage?.From?.Id == me.Id)
                    {
                        shouldRespond = true;
                    }
                }
                else
                {
                    shouldRespond = true;
                }

                message.Text = text;
            }

            if (!shouldRespond)
                return;

            var messageText = message.Text ?? message.Caption;

            if (message.Voice != null)
            {
                var file = await _botClient.SendRequest(new GetFileRequest { FileId = message.Voice.FileId }, cancellationToken);
                var audioBytes = await _httpClient.GetByteArrayAsync($"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}");
                response = await _voiceService.HandleVoiceAsync(chatId, userId, audioBytes);
            }
            else if (message.Audio != null)
            {
                var file = await _botClient.SendRequest(new GetFileRequest { FileId = message.Audio.FileId }, cancellationToken);
                var audioBytes = await _httpClient.GetByteArrayAsync($"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}");
                response = await _voiceService.HandleVoiceAsync(chatId, userId, audioBytes);
            }
            else if (!string.IsNullOrWhiteSpace(messageText) && messageText.StartsWith("/image", StringComparison.OrdinalIgnoreCase) && message.Photo != null)
            {
                var prompt = messageText.Substring(6).Trim();
                var file = await _botClient.SendRequest(new GetFileRequest { FileId = message.Photo[^1].FileId }, cancellationToken);
                var imageBytes = await _httpClient.GetByteArrayAsync($"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}");
                response = await _imageService.HandleImageAsync(chatId, userId, imageBytes, prompt);
            }
            else if (!string.IsNullOrWhiteSpace(messageText) && messageText.StartsWith("/file", StringComparison.OrdinalIgnoreCase) && message.Document != null)
            {
                var prompt = messageText.Substring(5).Trim();
                var file = await _botClient.SendRequest(new GetFileRequest { FileId = message.Document.FileId }, cancellationToken);
                var fileStream = await _httpClient.GetStreamAsync($"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}");
                response = await _docService.HandleDocumentAsync(chatId, userId, fileStream, message.Document.FileName, prompt);
            }
            else if (!string.IsNullOrWhiteSpace(messageText))
            {
                if (chatType == ChatType.Private)
                {
                    response = await _chatService.GetPrivateResponseAsync(userId, messageText);
                }
                else
                {
                    var senderName = UserNames.GetValueOrDefault(userId, message.From?.FirstName ?? "User");
                    response = await _chatService.GetGroupResponseAsync(chatId, userId, messageText, senderName);
                }
            }

            if (!string.IsNullOrWhiteSpace(response))
            {
                await _botClient.SendRequest(
                    new SendMessageRequest
                    {
                        ChatId = chatId,
                        MessageThreadId = message.MessageThreadId,
                        Text = response
                    }, cancellationToken
                );

                Console.WriteLine($"[Bot] Responded to {userId} in chat {chatId} (thread {message.MessageThreadId ?? 0}): {response}");
            }
        }

        private static string GetBestDisplayName(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Username))
                return $"@{user.Username}";
            else
                return $"{user.FirstName} {user.LastName}".Trim();
        }
    }
}
