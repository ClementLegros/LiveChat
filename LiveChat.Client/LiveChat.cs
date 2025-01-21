using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Excepts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CalSup.Utilities;
using Vlc.DotNet.Forms;

namespace LiveChat.Client
{
    public partial class LiveChat : Form
    {
        private Server.Server LiveChatServer { get; set; }
        private FileSystemWatcher Watcher { get; set; }
        
        public LiveChat()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            StartServer();
        }

        private async void StartServer()
        {
            LiveChatServer = new Server.Server();

            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            
            await LiveChatServer.StartServer(Utils.SafeParseInt(port));
        }

        private void InitializeFileSystemWatcher()
        {
            string liveChatFolderPath = Path.GetTempPath() + @"LiveChat\";

            if (!Directory.Exists(liveChatFolderPath))
            {
                Directory.CreateDirectory(liveChatFolderPath);
            }

            Watcher = new FileSystemWatcher
            {
                Path = liveChatFolderPath,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*",
                IncludeSubdirectories = true,
            };

            Watcher.Created += OnLiveChatFolderChanged;

            Watcher.Error += OnError;

            Watcher.EnableRaisingEvents = true;
        }

        private void OnLiveChatFolderChanged(object source, FileSystemEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnLiveChatFolderChanged(source, e)));
                return;
            }

            string filePath = e.FullPath;

            if (filePath.EndsWith(".mp4"))
            {
               DisplayVideo(filePath); 
            }
            else
            {
                DisplayImage(filePath);
            }
        }

        private async void buttonSendFile_Click(object sender, EventArgs e)
        {
            Logger.Enter();

            openFileDialogLiveChat.FileName = "";
            
            if (openFileDialogLiveChat.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("No file selected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            string filePath = openFileDialogLiveChat.FileName;
            string settingValue = ConfigurationManager.AppSettings["LiveChatIpSender"];
  
            List<string> ipList = settingValue.Split(',').ToList();
            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            
            await LiveChatServer.SendFileToMultipleIPs(filePath, ipList, Utils.SafeParseInt(port)); 
            
            Logger.Leave();
        }

        private void DisplayImage(string filePath)
        {
            Image image = Image.FromFile(filePath);
            
            Size imageSize = image.Size;
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            int windowsHeight = screenHeight > imageSize.Height ? imageSize.Height : imageSize.Height / 2;
            int windowsWidth = screenWidth > imageSize.Width ? imageSize.Width : imageSize.Width / 2;
            
            // Create a new form dynamically
            Form mediaForm = new Form();
            mediaForm.Size = new System.Drawing.Size(windowsWidth, windowsHeight);
            mediaForm.TopMost = true;
            mediaForm.FormBorderStyle = FormBorderStyle.None;
            mediaForm.StartPosition = FormStartPosition.CenterScreen;
            

            // Create a PictureBox to display the image or GIF
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.ImageLocation = filePath;
            mediaForm.Controls.Add(pictureBox);

            // Create and start a timer to close the form after a few seconds
            Timer timer = new Timer();
            timer.Interval = 8000; // Display for 3 seconds
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                mediaForm.Close();
            };
            timer.Start();

            // Show the form
            mediaForm.Show(); 
        }

        private void DisplayVideo(string filePath)
        {
            // Create a new form dynamically
            Form mediaForm = new Form();
            mediaForm.Size = new System.Drawing.Size(800, 600);
            mediaForm.TopMost = true; // Make the form stay on top
            mediaForm.FormBorderStyle = FormBorderStyle.None;
            mediaForm.StartPosition = FormStartPosition.CenterScreen; // Center the form on the screen

            // Create a VLC control to display the video
            VlcControl vlcControl = new VlcControl();
            vlcControl.Dock = DockStyle.Fill;
            vlcControl.VlcLibDirectory = new System.IO.DirectoryInfo(ConfigurationManager.AppSettings["VlcPath"]); // Adjust the path to your VLC installation
            vlcControl.EndInit();
            vlcControl.SetMedia(new Uri(filePath));
            vlcControl.Play();
            mediaForm.Controls.Add(vlcControl);

            // Create and start a timer to close the form after the video ends
            // Handle the EndReached event to close the form when the video ends
            vlcControl.EndReached += (s, args) =>
            {
                mediaForm.Invoke((Action)(() =>
                {
                    mediaForm.Close();
                }));
            };;

            // Show the form
            mediaForm.Show();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            MessageBox.Show($"An error occurred: {e.GetException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"An error occurred: {e.GetException().Message}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Watcher.Dispose();
            base.OnFormClosing(e);
        }
    }
}