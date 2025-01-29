using System;
using System.Collections.Generic;
using System.Excepts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CalSup.Utilities;
using System.Security.Cryptography;

namespace LiveChat.Server
{
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public string IpAddress { get; set; }
        public bool IsConnected { get; set; }
    }

    public class Server
    {
        private List<TcpListener> Listeners { get; set; }
        private Dictionary<string, bool> ConnectionStates { get; set; }
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        private static readonly byte[] AesKey = new byte[] 
        { 
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x17, 0x18, 0x19, 0x20, 0x21, 0x22, 0x23, 0x24,
            0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32
        };
        
        private static readonly byte[] AesIV = new byte[] 
        { 
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
        };

        public Server()
        {
            Listeners = new List<TcpListener>();
            ConnectionStates = new Dictionary<string, bool>();
        }

        public async Task StartServer(int port)
        {
            Logger.Enter();
            
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Listeners.Add(listener);

            Task listenerTask = AcceptClientsAsync(listener);  
           
            await Task.WhenAll(listenerTask);
            
            Logger.Leave();
        }

        private async Task AcceptClientsAsync(TcpListener listener)
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            Logger.Enter();
            
            IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (remoteEndPoint != null)
            {
                Logger.Info($"Client connected to {remoteEndPoint.Address}:{remoteEndPoint.Port}");    
            }

            using (NetworkStream stream = client.GetStream())
            {
                // Lire la longueur du nom du fichier
                byte[] fileNameLengthBytes = new byte[4];
                await stream.ReadAsync(fileNameLengthBytes, 0, 4);
                int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
        
                // Lire le nom du fichier
                byte[] fileNameBytes = new byte[fileNameLength];
                await stream.ReadAsync(fileNameBytes, 0, fileNameLength);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                if (fileName == null) return;
                
                // Lire la longueur de la caption
                byte[] captionLengthBytes = new byte[4];
                await stream.ReadAsync(captionLengthBytes, 0, 4);
                int captionLength = BitConverter.ToInt32(captionLengthBytes, 0);

                string caption = null;
                if (captionLength > 0)
                {
                    // Lire la caption
                    byte[] captionBytes = new byte[captionLength];
                    await stream.ReadAsync(captionBytes, 0, captionLength);
                    caption = Encoding.UTF8.GetString(captionBytes);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    byte[] encryptedBytes = ms.ToArray();
                    byte[] fileBytes = DecryptData(encryptedBytes);
                    string fileType = GetFileType(fileBytes);
                    if (!IsValidFileType(fileType))
                    {
                        Logger.Warning($"Rejected file of unknown type");
                        return;
                    }
                    
                    // Créer un nom de fichier unique sans la caption
                    string randomFileName = $"{Guid.NewGuid()}.{fileType}";
                    string liveChatFolderPath = Path.GetTempPath() + $@"LiveChat\{randomFileName}";
                    
                    // Sauvegarder le fichier
                    File.WriteAllBytes(liveChatFolderPath, fileBytes);

                    // Si une caption existe, créer un fichier metadata associé
                    if (!string.IsNullOrEmpty(caption))
                    {
                        string metadataPath = liveChatFolderPath + ".metadata";
                        File.WriteAllText(metadataPath, caption);
                    }
                    
                    Logger.Info($"{fileType.ToUpper()} file received and saved with caption."); 
                }
            }

            await CleanupOldFiles(Path.GetTempPath() + @"LiveChat\", TimeSpan.FromHours(24));
        }
        
        public async Task SendFileToMultipleIPs(string filePath, List<string> ipAddresses, int port, string fileName, string caption = null)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            List<Task> sendTasks = new List<Task>();

            foreach (var ipAddress in ipAddresses)
            {
                sendTasks.Add(SendFile(fileBytes, ipAddress, port, fileName, caption));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task SendFile(byte[] fileBytes, string ipAddress, int port, string fileName, string caption = null)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        throw new TimeoutException($"La connexion à {ipAddress}:{port} a expiré");
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Envoyer le nom du fichier
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                        byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                        await stream.WriteAsync(fileNameLength, 0, 4);
                        await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                        // Envoyer la caption
                        byte[] captionBytes = caption != null ? Encoding.UTF8.GetBytes(caption) : new byte[0];
                        byte[] captionLength = BitConverter.GetBytes(captionBytes.Length);
                        await stream.WriteAsync(captionLength, 0, 4);
                        if (captionBytes.Length > 0)
                        {
                            await stream.WriteAsync(captionBytes, 0, captionBytes.Length);
                        }

                        // Envoyer le fichier
                        byte[] encryptedFileBytes = EncryptData(fileBytes);
                        await stream.WriteAsync(encryptedFileBytes, 0, encryptedFileBytes.Length);
                        
                        Logger.Info($"Fichier envoyé avec succès à {ipAddress}:{port}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Erreur lors de l'envoi du fichier à {ipAddress}:{port}: {ex.Message}");
            }
        }
        
        private string GetFileType(byte[] fileBytes)
        {
            // Check for common file headers
            if (fileBytes.Length > 4)
            {
                // JPEG
                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                    return "jpg";
                // PNG
                if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
                    return "png";
                // GIF
                if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46)
                    return "gif";
                // MP4
                if (fileBytes[4] == 0x66 && fileBytes[5] == 0x74 && fileBytes[6] == 0x79 && fileBytes[7] == 0x70)
                    return "mp4";
            }
            return "unknown";
        }

        private bool IsValidFileType(string fileType)
        {
            var allowedTypes = new[] { "jpg", "png", "gif", "mp4" };
            return allowedTypes.Contains(fileType.ToLower());
        }

        private byte[] EncryptData(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(encryptedData, 0, encryptedData.Length);
                        csDecrypt.FlushFinalBlock();
                    }
                    return msDecrypt.ToArray();
                }
            }
        }

        public async Task CheckConnectionsAsync(List<string> ipAddresses, int port)
        {
            foreach (var ipAddress in ipAddresses)
            {
                bool isConnected = await IsHostAvailable(ipAddress, port);
                
                // Si l'état a changé ou n'existait pas
                if (!ConnectionStates.ContainsKey(ipAddress) || ConnectionStates[ipAddress] != isConnected)
                {
                    ConnectionStates[ipAddress] = isConnected;
                    OnConnectionStateChanged(ipAddress, isConnected);
                }
            }
        }

        private async Task<bool> IsHostAvailable(string ipAddress, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void OnConnectionStateChanged(string ipAddress, bool isConnected)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs 
            { 
                IpAddress = ipAddress, 
                IsConnected = isConnected 
            });
        }

        private async Task CleanupOldFiles(string folderPath, TimeSpan maxAge)
        {
            try
            {
                var directory = new DirectoryInfo(folderPath);
                var files = directory.GetFiles();
                var cutoffTime = DateTime.Now - maxAge;

                foreach (var file in files)
                {
                    if (file.CreationTime < cutoffTime)
                    {
                        file.Delete();
                        Logger.Info($"Cleaned up old file: {file.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during cleanup: {ex.Message}");
            }
        }
    }
}