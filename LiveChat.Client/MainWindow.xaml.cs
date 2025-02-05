using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveChat.Server;
using System.ComponentModel;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Media;
using System.Diagnostics;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using LiveChat.Utilities;
using LiveChat.Utilities.Excepts;

namespace LiveChat.Client
{
    public partial class MainWindow : Window
    {
        private TcpServer LiveChatTcpServer { get; set; }
        private DiscordServer LiveChatDiscordServer { get; set; }
        private FileSystemWatcher Watcher { get; set; }
        private List<User> Users { get; set; }
        private string LiveChatFolderPath { get; set; }
        private string LiveChatSwapFolderPath { get; set; }
        private DispatcherTimer ConnectionCheckTimer { get; set; }
        private TaskbarIcon NotifyIcon { get; set; }
        private bool IsClosing { get; set; }
        private bool IsDarkTheme { get; set; }
        private bool UseMouseScreen { get; set; } = false;
        private Screen SelectedScreen { get; set; } = Screen.PrimaryScreen;
        private Media MediaManager { get; set; }

        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("LiveChat is already running.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsClosing = true;
                Close();
                return;
            }

            InitializeComponent();
            InitializeFileSystemWatcher();
            InitializeListBoxUsers();
            CleanupLiveChatSwapFolder();
            InitRandomLiveChatWallpaper();
            InitializeTcpIpServer().ConfigureAwait(false);
            InitializeDiscordServer().ConfigureAwait(false);
            MediaManager = new Media();
            InitializeConnectionChecker();
            InitializeTrayIcon();
            InitializeTheme();
        }

        private void InitializeListBoxUsers()
        {
            string ipConfigString = ConfigurationManager.AppSettings["LiveChatIpSender"];
            List<string> ipList = ipConfigString.Split(',').ToList();

            Users = new List<User>();
            foreach (string ip in ipList)
            {
                string[] ipSplit = ip.Split('-');
                if (ipSplit.Length == 2)
                {
                    Users.Add(new User
                    {
                        Username = ipSplit[0],
                        IpAddress = ipSplit[1],
                        IsConnected = false
                    });
                }
            }
            listBoxUsers.ItemsSource = Users;
        }

        private async Task StartTcpIpServer()
        {
            string port = ConfigurationManager.AppSettings["LiveChatPort"];
            LiveChatTcpServer = new TcpServer()
            {
                Port = Utils.SafeParseInt(port)
            };

            await LiveChatTcpServer.StartServer();
        }

        private async Task StartDiscordServer()
        {
            string token = ConfigurationManager.AppSettings["DiscordBotToken"];
            string channelId = ConfigurationManager.AppSettings["ChannelId"];

            LiveChatDiscordServer = new DiscordServer(token, Convert.ToUInt64(channelId));

            await LiveChatDiscordServer.StartServer();
        }

        private async Task InitializeTcpIpServer()
        {
            try 
            {
                await StartTcpIpServer();
                Logger.Info("Servers started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start servers: {ex.Message}");
                MessageBox.Show($"Error starting servers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task InitializeDiscordServer()
        {
            try 
            {
                await StartDiscordServer();
                Logger.Info("Servers started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start servers: {ex.Message}");
                MessageBox.Show($"Error starting servers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void ButtonSendFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
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
            await LiveChatTcpServer.SendFileToMultipleIPs(filePath, ipList, Utils.SafeParseInt(port), filePath,
                !string.IsNullOrEmpty(caption) ? caption : null);
            CleanupLiveChatSwapFolder();

            textBoxCaption.Text = string.Empty;
            Logger.Leave();
        }

        private async void ButtonSelectGif_Click(object sender, RoutedEventArgs e)
        {
            var gifSelector = new GifSelectorWindow
            {
                Owner = this
            };

            if (gifSelector.ShowDialog() == true)
            {
                string gifUrl = gifSelector.SelectedGifUrl;
                string tempGifPath = Path.Combine(Path.GetTempPath(), $"LiveChat_temp_{Guid.NewGuid()}.gif");

                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        await client.DownloadFileTaskAsync(gifUrl, tempGifPath);
                    }

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
                    await LiveChatTcpServer.SendFileToMultipleIPs(tempGifPath, ipList, Utils.SafeParseInt(port), tempGifPath,
                        !string.IsNullOrEmpty(caption) ? caption : null);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempGifPath))
                        {
                            File.Delete(tempGifPath);
                        }
                    }
                    catch { }
                }
            }
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

                if (filePath.EndsWith(".metadata") || filePath.EndsWith(".unknown"))
                    return;

                string caption = null;
                string metadataPath = filePath + ".metadata";
                if (File.Exists(metadataPath))
                {
                    caption = File.ReadAllText(metadataPath);
                    try { File.Delete(metadataPath); } catch { }
                }

                MediaManager.EnqueueMedia(filePath, caption);
            }));
        }

        private void InitRandomLiveChatWallpaper()
        {
            Logger.Enter();

            string liveChatWallpaper = Directory.GetCurrentDirectory() + @"\LiveChatWallpaper";

            if (!Directory.Exists(liveChatWallpaper))
            {
                Logger.Info("Create live chat wallpaper directory");

                Directory.CreateDirectory(liveChatWallpaper);
                Logger.Leave();
                return;
            }

            string[] files = Directory.GetFiles(liveChatWallpaper);

            if (files.Length == 0)
            {
                Logger.Info("No wallpaper found");
                return;
            }

            Random random = new Random();

            int randomIndex = random.Next(0, files.Length);

            string randomSelectedWallpaper = files[randomIndex];

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
            Logger.Enter();

            ConnectionCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };

            ConnectionCheckTimer.Tick += async (s, e) => await CheckConnections();
            LiveChatTcpServer.ConnectionStateChanged += OnConnectionStateChanged;
            ConnectionCheckTimer.Start();

            Logger.Leave();
        }

        private async Task CheckConnections()
        {
            Logger.Enter();

            List<string> ipAddresses = Users.Select(u => u.IpAddress).ToList();
            int port = Utils.SafeParseInt(ConfigurationManager.AppSettings["LiveChatPort"]);
            await LiveChatTcpServer.CheckConnectionsAsync(ipAddresses, port);

            Logger.Leave();
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
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
            if (LiveChatTcpServer != null)
            {
                LiveChatTcpServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        private void InitializeTrayIcon()
        {
            Logger.Enter();

            NotifyIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
            {
                Icon = new System.Drawing.Icon("TheBaldZoomer.ico"),
                ToolTipText = "LiveChat",
                MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick
            };

            ContextMenu contextMenu = new ContextMenu();

            MenuItem openMenuItem = new MenuItem { Header = "Open" };

            openMenuItem.Click += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) =>
            {
                IsClosing = true;
                Close();
            };

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenuItem);

            NotifyIcon.ContextMenu = contextMenu;

            NotifyIcon.DoubleClickCommand = new RelayCommand(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });

            Logger.Leave();
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
            if (!IsClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                NotifyIcon?.Dispose();
                base.OnClosing(e);
            }
        }

        private void InitializeTheme()
        {
            IsDarkTheme = IsSystemInDarkMode();
            ApplyTheme(IsDarkTheme);
        }

        private bool IsSystemInDarkMode()
        {
            try
            {
                string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                string registryValueName = "AppsUseLightTheme";

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKeyPath))
                {
                    object registryValue = key?.GetValue(registryValueName);
                    return registryValue != null && (int)registryValue == 0;
                }
            }
            catch
            {
                return false;
            }

        }

        private void ApplyTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            ResourceDictionary themeDictionary = Resources.MergedDictionaries[0];
            object newTheme = isDark ? themeDictionary["DarkTheme"] : themeDictionary["LightTheme"];

            foreach (var key in ((ResourceDictionary)newTheme).Keys)
            {
                Resources[key] = ((ResourceDictionary)newTheme)[key];
            }

            themeIcon.Data = Geometry.Parse(isDark
                ? "M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313 -12.454z" // icône lune
                : "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z"); // icône soleil
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(!IsDarkTheme);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(UseMouseScreen, SelectedScreen)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                UseMouseScreen = settingsWindow.UseMouseScreen;
                SelectedScreen = settingsWindow.SelectedScreen;
                MediaManager.UpdateScreenSettings(UseMouseScreen, SelectedScreen);
            }
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
