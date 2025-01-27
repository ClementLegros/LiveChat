using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CalSup.Utilities;
using CalSup.Utilities.Excepts;
using Microsoft.Win32;

namespace LiveChat.WPF
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Server.Server LiveChatServer { get; set; }
        private FileSystemWatcher Watcher { get; set; }
        private List<User> Users { get; set; }
        private string LiveChatFolderPath { get; set; }
        private string LiveChatSwapFolderPath { get; set; }
        private Window CurrentMediaWindow { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            StartServer();
            InitializeListBoxUsers();
            CleanupLiveChatSwapFolder();
            
            // Load Amelie image if needed
            // pictureBoxAmelie.Source = new BitmapImage(new Uri("path_to_amelie_image.png", UriKind.Relative));
        }

        private void InitializeListBoxUsers()
        {
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
            
            listBoxUsers.ItemsSource = Users;
        }

        private async void StartServer()
        {
            LiveChatServer = new Server.Server();
            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            await LiveChatServer.StartServer(Utils.SafeParseInt(port));
        }

        // ... Rest of the methods would be similar to the WinForms version but adapted for WPF ...
        // You'll need to convert the Form displays to WPF Windows
        // Replace OpenFileDialog with Microsoft.Win32.OpenFileDialog
        // Replace Windows.Forms controls with WPF equivalents

        private async void ButtonSendFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() != true)
            {
                MessageBox.Show("No file selected", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string filePath = openFileDialog.FileName;

            // Rest of the send file logic remains similar
            // Just adapt the UI elements to WPF equivalents
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
            
            //To see how to deal with it
            //Watcher.Error += OnError;

            Watcher.EnableRaisingEvents = true;

            LiveChatSwapFolderPath = Path.GetTempPath() + @"LiveChatSwap\";

            if(!Directory.Exists(LiveChatSwapFolderPath))
            {
                Directory.CreateDirectory(LiveChatSwapFolderPath);
            }    
        }

        private void OnLiveChatFolderChanged(object source, FileSystemEventArgs e)
        {
            // Update UI on the main thread using Dispatcher
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string filePath = e.FullPath;

                if (filePath.EndsWith(".mp4"))
                {
                    DisplayVideo(filePath); 
                }
                else
                {
                    DisplayImage(filePath);
                }
            }));
        }

        private void DisplayImage(string filePath)
        {
            try
            {
                if (CurrentMediaWindow != null)
                {
                    CurrentMediaWindow.Close();
                }

                var imageWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = null,
                    Topmost = true,
                    WindowState = WindowState.Maximized
                };

                var grid = new System.Windows.Controls.Grid();

                // Un seul conteneur pour l'image et le texte superposé
                var viewbox = new System.Windows.Controls.Viewbox
                {
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                // Grid interne pour superposer le texte sur l'image
                var innerGrid = new Grid();

                var image = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(filePath)),
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                innerGrid.Children.Add(image);

                string caption = null;
                if (filePath.Contains("text="))
                {
                    caption = filePath.Split('=')[1].Split('.')[0];
                }

                if (caption != null)
                {
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = caption,
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                        Padding = new Thickness(20, 10, 20, 10),
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    innerGrid.Children.Add(textBlock);
                }

                viewbox.Child = innerGrid;
                grid.Children.Add(viewbox);

                imageWindow.Content = grid;

                int displayDuration = Utils.SafeParseInt(ConfigurationManager.AppSettings["ImageDisplayDuration"]) * 1000;

                CurrentMediaWindow = imageWindow;
                imageWindow.Show();

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(displayDuration)
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    imageWindow.Close();
                    CurrentMediaWindow = null;
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying image: {ex.Message}");
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayVideo(string filePath)
        {
            try
            {
                if (CurrentMediaWindow != null)
                {
                    CurrentMediaWindow.Close();
                }

                var videoWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = null,
                    Topmost = true,
                    WindowState = WindowState.Maximized
                };

                var grid = new System.Windows.Controls.Grid();

                var viewbox = new System.Windows.Controls.Viewbox
                {
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                var innerGrid = new Grid();

                var mediaElement = new MediaElement
                {
                    Source = new Uri(filePath),
                    LoadedBehavior = MediaState.Play,
                    UnloadedBehavior = MediaState.Close,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                innerGrid.Children.Add(mediaElement);

                string caption = null;
                if (filePath.Contains("text="))
                {
                    caption = filePath.Split('=')[1].Split('.')[0];
                }

                if (caption != null)
                {
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = caption,
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                        Padding = new Thickness(20, 10, 20, 10),
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    innerGrid.Children.Add(textBlock);
                }

                viewbox.Child = innerGrid;
                grid.Children.Add(viewbox);

                videoWindow.Content = grid;

                // Gérer la fin de la vidéo
                mediaElement.MediaEnded += (s, e) =>
                {
                    videoWindow.Close();
                    CurrentMediaWindow = null;
                };

                CurrentMediaWindow = videoWindow;
                videoWindow.Show();

                // Démarrer la lecture
                mediaElement.Play();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying video: {ex.Message}");
                MessageBox.Show($"Error displaying video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}
