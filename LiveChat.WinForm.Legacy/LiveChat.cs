using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private List<User> Users { get; set; }
        private string LiveChatFolderPath { get; set; }
        private string LiveChatSwapFolderPath { get; set; }
        private Form CurrentMediaForm { get; set; }
        
        public LiveChat()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            StartServer();
            InitializeListBoxUsers();
            CleanupLiveChatSwapFolder();
        }

        private void InitializeListBoxUsers()
        {
            Logger.Enter();
            
            string ipConfigString = ConfigurationManager.AppSettings["LiveChatIpSender"];
            
            List<string> ipList = ipConfigString.Split(',').ToList();

            Users = new List<User>();
            
            foreach (string ip in ipList)
            {
                string[] ipSplit = ip.Split('-');
                
                if(ipSplit.Length == 0) continue;
                
                Users.Add(new User
                {
                    Username = ipSplit[0],
                    IpAddress = ipSplit[1]
                });
            }
            
            listBoxUsers.DataSource = Users;
            listBoxUsers.SelectedItem = null;
            
            Logger.Leave();
        }

        private async void StartServer()
        {
            LiveChatServer = new Server.Server();

            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            
            await LiveChatServer.StartServer(Utils.SafeParseInt(port));
        }

        private void InitializeFileSystemWatcher()
        {
            LiveChatFolderPath = Path.GetTempPath() + @"LiveChat\";

            if (!Directory.Exists(LiveChatFolderPath))
            {
                Directory.CreateDirectory(LiveChatFolderPath);
            }

            Watcher = new FileSystemWatcher
            {
                Path = LiveChatFolderPath,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*",
                IncludeSubdirectories = true,
            };

            Watcher.Created += OnLiveChatFolderChanged;

            Watcher.Error += OnError;

            Watcher.EnableRaisingEvents = true;

            LiveChatSwapFolderPath = Path.GetTempPath() + @"LiveChatSwap\";

            if(!Directory.Exists(LiveChatSwapFolderPath))
            {
                Directory.CreateDirectory(LiveChatSwapFolderPath);
            }    
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
                return;
            }

            string filePath = openFileDialogLiveChat.FileName;

            if(!string.IsNullOrEmpty(textBoxCaption.Text))
            {
                string directory = LiveChatSwapFolderPath;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                
                string newFileName = fileNameWithoutExtension+ $"-text={textBoxCaption.Text}" + extension;
                string newFilePath = Path.Combine(directory, newFileName);
                
                File.Copy(filePath, newFilePath);
                
                filePath = newFilePath;
            }

            List<string> ipList = new List<string>();

            if (listBoxUsers.SelectedItems.Count == 0)
            {
                string ipConfigString = ConfigurationManager.AppSettings["LiveChatIpSender"];
                List<string> ipListConfig = ipConfigString.Split(',').ToList();

                foreach (string ipConfig in ipListConfig)
                {
                    string[] ipSplit = ipConfig.Split('-');
                    
                    if(ipSplit.Length == 0) continue;
                    
                    ipList.Add(ipSplit[1]);
                }
            }
            else
            {
                List<User> users = listBoxUsers.SelectedItems.Cast<User>().ToList();
                foreach (User user in users)
                {
                    ipList.Add(user.IpAddress);
                }
            }
  
            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            
            await LiveChatServer.SendFileToMultipleIPs(filePath, ipList, Utils.SafeParseInt(port), filePath); 
            
            CleanupLiveChatSwapFolder();
            
            Logger.Leave();
        }

        private void DisplayImage(string filePath)
        {
            if (CurrentMediaForm != null && !CurrentMediaForm.IsDisposed)
            {
                CurrentMediaForm.Close();
                CurrentMediaForm.Dispose();
            }

            Image image = Image.FromFile(filePath);
            
            Size imageSize = image.Size;
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            int windowsHeight = screenHeight > imageSize.Height ? imageSize.Height : imageSize.Height / 2;
            int windowsWidth = screenWidth > imageSize.Width ? imageSize.Width : imageSize.Width / 2;
            
            // Create a new form dynamically
            Form mediaForm = new Form();
            CurrentMediaForm = mediaForm;
            mediaForm.Size = new System.Drawing.Size(windowsWidth, windowsHeight);
            mediaForm.TopMost = true;
            mediaForm.FormBorderStyle = FormBorderStyle.None;
            mediaForm.StartPosition = FormStartPosition.CenterScreen;
            mediaForm.AllowTransparency = true;

            // Create a panel to organize controls vertically
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.AutoSize = true;
            mediaForm.Controls.Add(mainPanel);

            // Create a PictureBox to display the image or GIF
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.ImageLocation = filePath;
            pictureBox.BackColor = Color.Transparent;
            mainPanel.Controls.Add(pictureBox);

            string caption = null;
            if(filePath.Contains("text="))
            {
                string[] filePathSplit = filePath.Split('=');
                caption = filePathSplit[1];
                caption = caption.Split('.')[0];
            }

            if (!string.IsNullOrEmpty(caption))
            {
                Panel captionPanel = new Panel();
                captionPanel.Dock = DockStyle.Bottom;
                captionPanel.Height = 40;
                captionPanel.BackColor = Color.FromArgb(64, 0, 0, 0);
                mainPanel.Controls.Add(captionPanel);

                Label captionLabel = new Label();
                captionLabel.Text = caption;
                captionLabel.Font = new Font("Arial", 14, FontStyle.Bold);
                captionLabel.ForeColor = Color.White;
                captionLabel.AutoSize = true;
                captionLabel.TextAlign = ContentAlignment.MiddleCenter;
                
                captionLabel.Location = new Point(
                    (captionPanel.Width - TextRenderer.MeasureText(caption, captionLabel.Font).Width) / 2,
                    (captionPanel.Height - captionLabel.Height) / 2
                );
                
                captionPanel.Controls.Add(captionLabel);
            }

            int displayDuration = Utils.SafeParseInt(ConfigurationManager.AppSettings["LiveChatPort"]) * 1000;
            
            if (displayDuration == 0) displayDuration = 8000;
            
            Timer timer = new Timer();
            timer.Interval = displayDuration;
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                mediaForm.Close();
            };
            timer.Start();

            mediaForm.Show(); 
        }

        private void DisplayVideo(string filePath)
        {
            if (CurrentMediaForm != null && !CurrentMediaForm.IsDisposed)
            {
                CurrentMediaForm.Close();
                CurrentMediaForm.Dispose();
            }

            // Create a new form dynamically
            Form mediaForm = new Form();
            CurrentMediaForm = mediaForm;
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

        private void CleanupLiveChatSwapFolder()
        {
            Logger.Enter();

            Except.Try(() =>
            {
                string[] files = Directory.GetFiles(LiveChatSwapFolderPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }).Catch((Exception e) =>
            {
                Logger.Error(e.Message);
            });
            
            Logger.Leave();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Logger.Error($"An error occurred: {e.GetException().Message}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (CurrentMediaForm != null && !CurrentMediaForm.IsDisposed)
            {
                CurrentMediaForm.Close();
                CurrentMediaForm.Dispose();
            }
            
            Watcher.Dispose();
            base.OnFormClosing(e);
        }
    }
}