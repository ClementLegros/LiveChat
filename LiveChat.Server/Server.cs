using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CalSup.Utilities;

namespace LiveChat.Server
{
    public class Server
    {
        private List<TcpListener> Listeners { get; set; }

        public Server()
        {
            Listeners = new List<TcpListener>();
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
                string caption = null;
                if (fileName.Contains("text="))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    string[] fileNameParts = fileNameWithoutExtension.Split('=');
                    caption = fileNameParts[1];
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    byte[] fileBytes = ms.ToArray();
                    string fileType = GetFileType(fileBytes);
                    string randomFileName = caption == null ? $"{Guid.NewGuid()}.{fileType}" : $"{Guid.NewGuid()}-text={caption}.{fileName}";

                    string liveChatFolderPath = Path.GetTempPath() + $@"LiveChat\{randomFileName}";
                    
                    File.WriteAllBytes(liveChatFolderPath, fileBytes);
                    Logger.Info($"{fileType.ToUpper()} file received and saved."); 
                }
            }
        }
        
        public async Task SendFileToMultipleIPs(string filePath, List<string> ipAddresses, int port, string fileName)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            List<Task> sendTasks = new List<Task>();

            foreach (var ipAddress in ipAddresses)
            {
                sendTasks.Add(SendFile(fileBytes, ipAddress, port, fileName));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task SendFile(byte[] fileBytes, string ipAddress, int port, string fileName)
        {
            using (TcpClient client = new TcpClient(ipAddress, port))
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                    
                    await stream.WriteAsync(fileNameLength, 0, 4);
                    await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                    
                    Logger.Info($"File sent to {ipAddress}:{port}");
                }
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
    }
}