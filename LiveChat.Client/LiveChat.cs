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
        
        public LiveChat()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            StartServer();
            InitializeListBoxUsers();
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
            mediaForm.AllowTransparency = true;

            string caption = null;

            if(filePath.Contains("text="))
            {
                string[] filePathSplit = filePath.Split('=');
                caption = filePathSplit[1];
                caption = caption.Split('.')[0]; // Enlever l'extension du fichier
            }
            
            // Create a PictureBox to display the image or GIF
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.ImageLocation = filePath;
            pictureBox.BackColor = Color.Transparent;
            mediaForm.Controls.Add(pictureBox);

            if (!string.IsNullOrEmpty(caption))
            {
                OutlineLabel captionLabel = new OutlineLabel();
                captionLabel.Text = caption;
                
                captionLabel.AutoSize = true;
                captionLabel.TextAlign = ContentAlignment.MiddleCenter;
                captionLabel.ForeColor = Color.White;
                captionLabel.BackColor = Color.Transparent;                                 
                captionLabel.Font = new Font("Arial", 25, FontStyle.Bold);
                
                // Calculate position - moved down by 20% of the form height
                captionLabel.Location = new Point(
                    (mediaForm.ClientSize.Width - captionLabel.PreferredWidth) / 2,
                    (int)((mediaForm.ClientSize.Height - captionLabel.PreferredHeight) * 0.7) // Changed from 0.5 (center) to 0.7 (lower)
                );
                
                mediaForm.Controls.Add(captionLabel);
                captionLabel.BringToFront();
            }

            // Create and start a timer to close the form after a few seconds
            Timer timer = new Timer();
            timer.Interval = 8000; // Display for 3 seconds
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
    
    public class OutlineLabel : Label
    {
        public OutlineLabel()
        {
            this.SetStyle(ControlStyles.Opaque |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            string text = this.Text;
            Font font = this.Font;
            Rectangle bounds = this.ClientRectangle;

            using (GraphicsPath path = GetStringPath(text, font, bounds, new StringFormat()))
            {
                using (Pen pen = new Pen(Color.Black, 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
                using (Brush brush = new SolidBrush(this.ForeColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        private GraphicsPath GetStringPath(string text, Font font, Rectangle bounds, StringFormat format)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddString(
                text,
                font.FontFamily,
                (int)font.Style,
                font.Size * 1.333f,
                bounds,
                format
            );
            return path;
        }
    }
}