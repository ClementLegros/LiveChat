using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveChat.Utilities;
using LiveChat.Utilities.Excepts;
using WpfAnimatedGif;
using Color = System.Windows.Media.Color;
using Control = System.Windows.Forms.Control;
using Except = System.Excepts.Except;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;

namespace LiveChat.Client
{
    public class Media
    {
        private Queue<MediaItem> MediaQueue { get; set; }
        private bool IsDisplayingMedia { get; set; }
        private Window CurrentMediaWindow { get; set; }
        private bool UseMouseScreen { get; set; }
        private Screen SelectedScreen { get; set; }

        public Media(bool useMouseScreen = false, Screen selectedScreen = null)
        {
            Logger.Enter();

            MediaQueue = new Queue<MediaItem>();
            IsDisplayingMedia = false;
            UseMouseScreen = useMouseScreen;
            SelectedScreen = selectedScreen ?? Screen.PrimaryScreen;

            Logger.Leave();
        }

        public void EnqueueMedia(string filePath, string caption)
        {
            Logger.Enter();

            MediaQueue.Enqueue(new MediaItem { FilePath = filePath, Caption = caption });

            if (!IsDisplayingMedia)
            {
                ProcessNextMedia();
            }

            Logger.Leave();
        }

        public void UpdateScreenSettings(bool useMouseScreen, Screen selectedScreen)
        {
            UseMouseScreen = useMouseScreen;
            SelectedScreen = selectedScreen ?? Screen.PrimaryScreen;
        }

        private async void ProcessNextMedia()
        {
            Logger.Enter();

            if (MediaQueue.Count == 0 || IsDisplayingMedia)
            {
                Logger.Info("No more media items to process");
                Logger.Leave();
                return;
            }

            IsDisplayingMedia = true;
            MediaItem mediaItem = MediaQueue.Dequeue();

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

        private Screen GetTargetScreen()
        {
            if (!UseMouseScreen) return SelectedScreen;
            Point mousePosition = Control.MousePosition;
            return Screen.FromPoint(mousePosition);
        }

        private void PositionWindowOnTargetScreen(Window window)
        {
            Logger.Enter();

            Screen targetScreen = GetTargetScreen();
            Rectangle screenBounds = targetScreen.Bounds;
            window.WindowState = WindowState.Normal;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = screenBounds.Left;
            window.Top = screenBounds.Top;
            window.Width = screenBounds.Width;
            window.Height = screenBounds.Height;
            window.Loaded += (s, e) =>
            {
                window.WindowState = WindowState.Maximized;
                window.Activate();
                window.Focus();
                window.Topmost = true;
            };

            window.Show();

            Logger.Leave();
        }

        private Task DisplayImage(string filePath, string caption)
        {
            Logger.Enter();

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Except.Try(() =>
            {
                if (CurrentMediaWindow != null)
                {
                    CurrentMediaWindow.Close();
                }

                Window imageWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = null,
                    Topmost = true,
                    WindowState = WindowState.Maximized
                };

                Grid grid = new Grid();

                Viewbox viewbox = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                Grid innerGrid = new Grid();

                Image image = new Image
                {
                    Source = new BitmapImage(new Uri(filePath)),
                    Stretch = Stretch.Uniform
                };

                innerGrid.Children.Add(image);

                if (caption != null)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = caption,
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
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
                PositionWindowOnTargetScreen(imageWindow);
                imageWindow.Show();

                DispatcherTimer timer = new DispatcherTimer
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
            }).Catch((Exception ex) =>
            {
                Logger.Error($"Error displaying image: {ex.Message}");
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                tcs.SetException(ex);
            });

            Logger.Leave();
            return tcs.Task;
        }

        private Task DisplayVideo(string filePath, string caption)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Except.Try(() =>
            {
                if (CurrentMediaWindow != null)
                {
                    CurrentMediaWindow.Close();
                }

                Window videoWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = null,
                    Topmost = true,
                    WindowState = WindowState.Maximized
                };

                Grid grid = new Grid();

                Viewbox viewbox = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                Grid innerGrid = new Grid();

                MediaElement mediaElement = new MediaElement
                {
                    Source = new Uri(filePath),
                    LoadedBehavior = MediaState.Play,
                    UnloadedBehavior = MediaState.Close,
                    Stretch = Stretch.Uniform,
                    Volume = 1,
                    IsMuted = false
                };

                innerGrid.Children.Add(mediaElement);

                if (caption != null)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = caption,
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
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
                PositionWindowOnTargetScreen(videoWindow);
                videoWindow.Show();
            })
            .Catch((Exception ex) =>
            {
                Logger.Error($"Error displaying video: {ex.Message}");
                MessageBox.Show($"Error displaying video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            });

            return tcs.Task;
        }

        private Task DisplayGif(string filePath, string caption)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Except.Try(() =>
            {
                if (CurrentMediaWindow != null)
                {
                    CurrentMediaWindow.Close();
                }

                Window imageWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = null,
                    Topmost = true,
                    WindowState = WindowState.Maximized
                };

                Grid grid = new Grid();
                Viewbox viewbox = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    MaxWidth = SystemParameters.PrimaryScreenWidth * 0.75,
                    MaxHeight = SystemParameters.PrimaryScreenHeight * 0.75,
                    StretchDirection = StretchDirection.Both
                };

                Grid innerGrid = new Grid();

                Image image = new Image
                {
                    Stretch = Stretch.Uniform
                };

                BitmapImage gifImage = new BitmapImage();
                gifImage.BeginInit();
                gifImage.UriSource = new Uri(filePath);
                gifImage.EndInit();

                ImageBehavior.SetAnimatedSource(image, gifImage);

                innerGrid.Children.Add(image);

                if (caption != null)
                {
                    TextBlock textBlock = new TextBlock
                    {
                        Text = caption,
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
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
                PositionWindowOnTargetScreen(imageWindow);
                imageWindow.Show();

                DispatcherTimer timer = new DispatcherTimer
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
            })
            .Catch((Exception ex) =>
            {
                Logger.Error($"Error displaying GIF: {ex.Message}");
                MessageBox.Show($"Error displaying GIF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetException(ex);
            });

            return tcs.Task;
        }
    }

    public class MediaItem
    {
        public string FilePath { get; set; }
        public string Caption { get; set; }
    }
}
