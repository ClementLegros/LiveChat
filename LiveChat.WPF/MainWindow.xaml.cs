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
        private Queue<string> MediaQueue { get; set; }
        private bool IsDisplayingMedia { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileSystemWatcher();
            StartServer();
            InitializeListBoxUsers();
            CleanupLiveChatSwapFolder();
            MediaQueue = new Queue<string>();
            IsDisplayingMedia = false;
            
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string filePath = e.FullPath;
                MediaQueue.Enqueue(filePath);
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
            string filePath = MediaQueue.Dequeue();

            try
            {
                if (filePath.EndsWith(".mp4"))
                {
                    await DisplayVideo(filePath);
                }
                else if (filePath.EndsWith(".gif"))
                {
                    await DisplayGif(filePath);
                }
                else
                {
                    await DisplayImage(filePath);
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

        private Task DisplayImage(string filePath)
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

        private Task DisplayVideo(string filePath)
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
                    LoadedBehavior = MediaState.Manual,
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
                    tcs.SetResult(true);
                };

                CurrentMediaWindow = videoWindow;
                videoWindow.Show();

                // Attendre que le MediaElement soit chargé avant de lancer la lecture
                mediaElement.Loaded += (s, e) =>
                {
                    mediaElement.Play();
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error displaying video: {ex.Message}");
                MessageBox.Show($"Error displaying video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        private Task DisplayGif(string filePath)
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

                var viewbox = new System.Windows.Controls.Viewbox
                {
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                var innerGrid = new Grid();

                // Configurer le BitmapImage spécifiquement pour les GIFs
                var gifImage = new System.Windows.Media.Imaging.BitmapImage();
                gifImage.BeginInit();
                gifImage.UriSource = new Uri(filePath);
                gifImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                // Activer l'animation du GIF
                gifImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                gifImage.EndInit();

                var image = new System.Windows.Controls.Image
                {
                    Source = gifImage,
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
