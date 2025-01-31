using System;
using System.Windows.Forms;
using System.Windows;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using Application = System.Windows.Application;

namespace LiveChat.Client
{
    public partial class SettingsWindow : Window
    {
        public bool UseMouseScreen { get; private set; }
        public Screen SelectedScreen { get; private set; }

        private List<Border> ScreenBorders { get; set; } = new List<Border>();

        public SettingsWindow(bool currentUseMouseScreen, Screen currentScreen)
        {
            InitializeComponent();
            
            UseMouseScreen = currentUseMouseScreen;
            SelectedScreen = currentScreen;

            useMouseScreenCheckbox.IsChecked = UseMouseScreen;
            
            List<Screen> screens = Screen.AllScreens.ToList();
            screenComboBox.ItemsSource = screens;
            screenComboBox.DisplayMemberPath = "DeviceName";
            
            if (currentScreen != null)
            {
                screenComboBox.SelectedItem = screens.FirstOrDefault(s => s.DeviceName == currentScreen.DeviceName);
            }
            
            UpdateScreenComboBoxState();
            DrawScreens();
            screensCanvas.SizeChanged += (s, e) => DrawScreens();
        }

        private void UseMouseScreenCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UseMouseScreen = useMouseScreenCheckbox.IsChecked ?? false;
            UpdateScreenComboBoxState();
        }

        private void UpdateScreenComboBoxState()
        {
            screenComboBox.IsEnabled = !UseMouseScreen;
        }

        private void ScreenComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedScreen = screenComboBox.SelectedItem as Screen;
            DrawScreens(); 
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DrawScreens()
        {
            screensCanvas.Children.Clear();
            ScreenBorders.Clear();

            List<Screen> screens = Screen.AllScreens.ToList();
            
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);
            int maxX = screens.Max(s => s.Bounds.X + s.Bounds.Width);
            int maxY = screens.Max(s => s.Bounds.Y + s.Bounds.Height);
            
            double scaleX = screensCanvas.ActualWidth / (maxX - minX);
            double scaleY = screensCanvas.ActualHeight / (maxY - minY);
            double scale = Math.Min(scaleX, scaleY) * 0.8; // 80% pour avoir une marge
            
            double offsetX = (screensCanvas.ActualWidth - (maxX - minX) * scale) / 2;
            double offsetY = (screensCanvas.ActualHeight - (maxY - minY) * scale) / 2;
            
            foreach (Screen screen in screens)
            {
                Border border = new Border
                {
                    BorderThickness = new Thickness(1),
                    Background = Equals(screen, SelectedScreen) ? 
                        new SolidColorBrush(Color.FromRgb(51, 153, 255)) : // #3399FF
                        new SolidColorBrush(Color.FromRgb(224, 224, 224)), // #E0E0E0
                    BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)) // #CCCCCC
                };

                TextBlock textBlock = new TextBlock
                {
                    Text = (screens.IndexOf(screen) + 1).ToString(),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                border.Child = textBlock;

                double x = (screen.Bounds.X - minX) * scale + offsetX;
                double y = (screen.Bounds.Y - minY) * scale + offsetY;
                double width = screen.Bounds.Width * scale;
                double height = screen.Bounds.Height * scale;

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);
                border.Width = width;
                border.Height = height;

                border.MouseDown += (s, e) =>
                {
                    if (UseMouseScreen) return;
                    screenComboBox.SelectedItem = screen;
                    SelectedScreen = screen;
                };

                screensCanvas.Children.Add(border);
                ScreenBorders.Add(border);
            }
        }
    }
} 