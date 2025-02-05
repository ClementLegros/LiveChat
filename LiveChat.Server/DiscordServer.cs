using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using LiveChat.Utilities;

namespace LiveChat.Server
{
    public class DiscordServer
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token;
        private readonly ulong _channelId;
        private readonly HttpClient _httpClient;

        public DiscordServer(string token, ulong channelId)
        {
            _token = token;
            _channelId = channelId;
            
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.MessageContent | 
                                 GatewayIntents.GuildMessages |   
                                 GatewayIntents.Guilds,
                LogLevel = LogSeverity.Debug
            };
            
            _client = new DiscordSocketClient(config);
            _httpClient = new HttpClient();

            _client.Log += LogAsync;
            _client.MessageReceived += HandleMessageAsync;
            _client.Ready += () =>
            {
                Logger.Info($"Bot is ready! Connected as {_client.CurrentUser.Username}");
                Logger.Info($"Watching channel ID: {_channelId}");
                return Task.CompletedTask;
            };
        }

        public async Task StartServer()
        {
            Logger.Enter();
            try
            {
                await _client.LoginAsync(TokenType.Bot, _token).ConfigureAwait(false);
                await _client.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start Discord server: {ex.Message}");
                throw;
            }
            Logger.Leave();
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            // Ignorer les messages qui ne sont pas du bon canal
            if (message.Channel.Id != _channelId) return;

            try
            {
                string liveChatPath = Path.Combine(Path.GetTempPath(), "LiveChat");
                if (!Directory.Exists(liveChatPath))
                {
                    Directory.CreateDirectory(liveChatPath);
                }

                // Traiter le texte du message
                if (!string.IsNullOrEmpty(message.Content))
                {
                    string fileName = string.Format("{0}.txt", Guid.NewGuid());
                    string filePath = Path.Combine(Path.GetTempPath(), "LiveChat", fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fileStream))
                    {
                        await writer.WriteAsync(message.Content).ConfigureAwait(false);
                    }
                    Logger.Info($"Saved message text to {fileName}");
                }

                // Traiter les pièces jointes
                foreach (var attachment in message.Attachments)
                {
                    string fileType = Path.GetExtension(attachment.Filename);
                    if (!Utils.IsValidFileType(fileType))
                    {
                        Logger.Warning($"Rejected file of unknown type: {fileType}");
                        continue;
                    }

                    string fileName = string.Format("{0}{1}", Guid.NewGuid(), fileType);
                    string filePath = Path.Combine(Path.GetTempPath(), "LiveChat", fileName);

                    // Télécharger et sauvegarder le fichier
                    byte[] fileData = await _httpClient.GetByteArrayAsync(attachment.Url).ConfigureAwait(false);
                    await WriteAllBytesAsync(filePath, fileData).ConfigureAwait(false);

                    // Sauvegarder les métadonnées si nécessaire
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        string metadataPath = filePath + ".metadata";
                        using (var fileStream = new FileStream(metadataPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var writer = new StreamWriter(fileStream))
                        {
                            await writer.WriteAsync(message.Content).ConfigureAwait(false);
                        }
                    }

                    Logger.Info($"Saved attachment to {fileName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing Discord message: {ex.Message}");
            }
        }

        public async Task StopServer()
        {
            if (_client != null)
            {
                await _client.StopAsync().ConfigureAwait(false);
                await _client.LogoutAsync().ConfigureAwait(false);
            }
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
                   bufferSize: 4096, useAsync: true))
            {
                await fileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Logger.Info($"Discord: {log.Message}");
            if (log.Exception != null)
                Logger.Error($"Discord Exception: {log.Exception}");
            return Task.CompletedTask;
        }
    }
}
