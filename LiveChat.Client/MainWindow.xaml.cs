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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using System.Windows.Threading;
using LiveChat.Server;
using System.ComponentModel;  // Add this for CancelEventArgs
using System.Drawing;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Input;
using Image = System.Windows.Controls.Image;

namespace LiveChat.Client
{
    public partial class MainWindow : Window
    {
        private Server.Server LiveChatServer { get; set; }
        private FileSystemWatcher Watcher { get; set; }
        private List<User> Users { get; set; }
        private string LiveChatFolderPath { get; set; }
        private string LiveChatSwapFolderPath { get; set; }
        private Window CurrentMediaWindow { get; set; }
        private Queue<MediaItem> MediaQueue { get; set; }
        private bool IsDisplayingMedia { get; set; }
        private DispatcherTimer _connectionCheckTimer;
        private TaskbarIcon _notifyIcon;
        private bool _isClosing;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            InitializeListBoxUsers();
            CleanupLiveChatSwapFolder();
            InitRandomLiveChatWallpaper();
            _ = StartServer();
            MediaQueue = new Queue<MediaItem>();
            IsDisplayingMedia = false;
            InitializeConnectionChecker();
            InitializeTrayIcon();
        }

        private void InitializeListBoxUsers()
        {
            string ipConfigString = ConfigurationManager.AppSettings["LiveChatIpSender"];
            List<string> ipList = ipConfigString.Split(',').ToList();

            Users = new List<User>();
            foreach (string ip in ipList)
            {
                string[] ipSplit = ip.Split('-');
                if (ipSplit.Length == 2) // Vérification que le split a bien donné 2 parties
                {
                    Users.Add(new User
                    {
                        Username = ipSplit[0],
                        IpAddress = ipSplit[1],
                        IsConnected = false // État initial
                    });
                }
            }
            listBoxUsers.ItemsSource = Users;
        }

        private async Task StartServer()
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
            string caption = textBoxCaption.Text;

            List<string> ipList = new List<string>();

            if (listBoxUsers.SelectedItems.Count == 0)
            {
                string ipConfigString = ConfigurationManager.AppSettings["LiveChatIpSender"];
                List<string> ipListConfig = ipConfigString.Split(',').ToList();

                foreach (string ipConfig in ipListConfig)
                {
                    string[] ipSplit = ipConfig.Split('-');
                    if (ipSplit.Length == 0) continue;
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
            await LiveChatServer.SendFileToMultipleIPs(filePath, ipList, Utils.SafeParseInt(port), filePath, 
                !string.IsNullOrEmpty(caption) ? caption : null);
            CleanupLiveChatSwapFolder();
            Logger.Leave();
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

            if (!Directory.Exists(LiveChatSwapFolderPath))
            {
                Directory.CreateDirectory(LiveChatSwapFolderPath);
            }
        }

        private void OnLiveChatFolderChanged(object source, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string filePath = e.FullPath;
                
                // Ignorer les fichiers .metadata
                if (filePath.EndsWith(".metadata"))
                    return;

                // Lire la caption si elle existe
                string caption = null;
                string metadataPath = filePath + ".metadata";
                if (File.Exists(metadataPath))
                {
                    caption = File.ReadAllText(metadataPath);
                    // Supprimer le fichier metadata après lecture
                    try { File.Delete(metadataPath); } catch { }
                }

                // Ajouter le fichier et sa caption à la queue
                MediaQueue.Enqueue(new MediaItem { FilePath = filePath, Caption = caption });
                
                if (!IsDisplayingMedia)
                {
                    ProcessNextMedia();
                }
            }));
        }

        private async void ProcessNextMedia()
        {
            if (MediaQueue.Count == 0 || IsDisplayingMedia)
            {
                return;
            }

            IsDisplayingMedia = true;
            var mediaItem = MediaQueue.Dequeue();

            try
            {
                if (mediaItem.FilePath.EndsWith(".mp4"))
                {
                    await DisplayVideo(mediaItem.FilePath, mediaItem.Caption);
                }
                else if (mediaItem.FilePath.EndsWith(".gif"))
                {
                    await DisplayGif(mediaItem.FilePath, mediaItem.Caption);
                }
                else
                {
                    await DisplayImage(mediaItem.FilePath, mediaItem.Caption);
                }
            }
            finally
            {
                IsDisplayingMedia = false;
                if (MediaQueue.Count > 0)
                {
                    ProcessNextMedia();
                }
            }
        }

        private Task DisplayImage(string filePath, string caption)
        {
            var tcs = new TaskCompletionSource<bool>();

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
                    tcs.SetResult(true);
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying image: {ex.Message}");
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        private Task DisplayVideo(string filePath, string caption)
        {
            var tcs = new TaskCompletionSource<bool>();

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

                var grid = new Grid();

                var viewbox = new Viewbox
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
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Volume = 1,
                    IsMuted = false
                };

                innerGrid.Children.Add(mediaElement);

                if (caption != null)
                {
                    var textBlock = new TextBlock
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

                mediaElement.MediaEnded += (s, e) =>
                {
                    videoWindow.Close();
                    CurrentMediaWindow = null;
                    tcs.SetResult(true);
                };

                mediaElement.MediaFailed += (s, e) =>
                {
                    MessageBox.Show($"Media Failed: {e.ErrorException.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    videoWindow.Close();
                    CurrentMediaWindow = null;
                    tcs.SetException(e.ErrorException);
                };

                CurrentMediaWindow = videoWindow;
                videoWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying video: {ex.Message}");
                MessageBox.Show($"Error displaying video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        private Task DisplayGif(string filePath, string caption)
        {
            var tcs = new TaskCompletionSource<bool>();

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

                var grid = new Grid();
                var viewbox = new Viewbox
                {
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                var innerGrid = new Grid();

                var image = new Image
                {
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                // Création du BitmapImage pour le GIF
                var gifImage = new BitmapImage();
                gifImage.BeginInit();
                gifImage.UriSource = new Uri(filePath);
                gifImage.EndInit();

                // Configuration de l'animation
                ImageBehavior.SetAnimatedSource(image, gifImage);

                innerGrid.Children.Add(image);

                if (caption != null)
                {
                    var textBlock = new TextBlock
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
                    tcs.SetResult(true);
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying GIF: {ex.Message}");
                MessageBox.Show($"Error displaying GIF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        private void InitRandomLiveChatWallpaper()
        {
            string liveChatWallpaper = Directory.GetCurrentDirectory() + @"\LiveChatWallpaper";

            if (!Directory.Exists(liveChatWallpaper))
            {
                Directory.CreateDirectory(liveChatWallpaper);
                return;
            }

            string[] files = Directory.GetFiles(liveChatWallpaper);
            
            if(files.Length == 0) return;

            Random random = new Random();

            int randomIndex = random.Next(0, files.Length);

            string randomSelectedWallpaper =  files[randomIndex];
            
            RandomPictureWallpaper.Source = new BitmapImage(new Uri(randomSelectedWallpaper));
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

        private void InitializeConnectionChecker()
        {
            _connectionCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _connectionCheckTimer.Tick += async (s, e) => await CheckConnections();
            LiveChatServer.ConnectionStateChanged += OnConnectionStateChanged;
            _connectionCheckTimer.Start();
        }

        private async Task CheckConnections()
        {
            var ipAddresses = Users.Select(u => u.IpAddress).ToList();
            int port = Utils.SafeParseInt(ConfigurationManager.AppSettings["LiveChatPort"]);
            await LiveChatServer.CheckConnectionsAsync(ipAddresses, port);
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            // Comme cet événement peut venir d'un autre thread, on utilise le Dispatcher
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var user = Users.FirstOrDefault(u => u.IpAddress == e.IpAddress);
                if (user != null)
                {
                    user.IsConnected = e.IsConnected;
                }
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (LiveChatServer != null)
            {
                LiveChatServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
            {
                Icon = new System.Drawing.Icon("TheBaldZoomer.ico"),
                ToolTipText = "LiveChat",
                MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick
            };

            // Create context menu
            var contextMenu = new ContextMenu();
            
            var openMenuItem = new MenuItem { Header = "Open" };
            openMenuItem.Click += (s, e) => 
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };
            
            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => 
            {
                _isClosing = true;
                Close();
            };

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;
            
            _notifyIcon.DoubleClickCommand = new RelayCommand(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _notifyIcon.Dispose();
                base.OnClosing(e);
            }
        }
    }

    // Nouvelle classe pour stocker les informations du média
    public class MediaItem
    {
        public string FilePath { get; set; }
        public string Caption { get; set; }
    }
}
