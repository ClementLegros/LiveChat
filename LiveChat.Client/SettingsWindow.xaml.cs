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
        public System.Windows.Forms.Screen SelectedScreen { get; private set; }

        private List<Border> screenBorders = new List<Border>();

        public SettingsWindow(bool currentUseMouseScreen, Screen currentScreen)
        {
            InitializeComponent();
            
            // Appliquer le même thème que la fenêtre principale
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                // Copier les ressources de thème
                foreach (var dictionary in mainWindow.Resources.MergedDictionaries)
                {
                    Resources.MergedDictionaries.Add(dictionary);
                }
            }

            // Initialiser les valeurs
            UseMouseScreen = currentUseMouseScreen;
            SelectedScreen = currentScreen;

            // Configurer les contrôles
            useMouseScreenCheckbox.IsChecked = UseMouseScreen;
            
            // Remplir la combo box avec les écrans disponibles
            var screens = Screen.AllScreens.ToList();
            screenComboBox.ItemsSource = screens;
            screenComboBox.DisplayMemberPath = "DeviceName";
            
            // Sélectionner l'écran actuel
            if (currentScreen != null)
            {
                screenComboBox.SelectedItem = screens.FirstOrDefault(s => s.DeviceName == currentScreen.DeviceName);
            }
            
            // Mettre à jour l'état de la combo box
            UpdateScreenComboBoxState();

            // Dessiner les écrans initialement
            DrawScreens();

            // Redessiner quand la taille du canvas change
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

        private void ScreenComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SelectedScreen = screenComboBox.SelectedItem as System.Windows.Forms.Screen;
            DrawScreens(); // Redessiner pour mettre à jour la sélection
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DrawScreens()
        {
            screensCanvas.Children.Clear();
            screenBorders.Clear();

            var screens = Screen.AllScreens.ToList();
            
            // Trouver les dimensions totales
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);
            int maxX = screens.Max(s => s.Bounds.X + s.Bounds.Width);
            int maxY = screens.Max(s => s.Bounds.Y + s.Bounds.Height);
            
            // Calculer l'échelle pour s'adapter au canvas
            double scaleX = screensCanvas.ActualWidth / (maxX - minX);
            double scaleY = screensCanvas.ActualHeight / (maxY - minY);
            double scale = Math.Min(scaleX, scaleY) * 0.8; // 80% pour avoir une marge
            
            // Centrer le canvas
            double offsetX = (screensCanvas.ActualWidth - (maxX - minX) * scale) / 2;
            double offsetY = (screensCanvas.ActualHeight - (maxY - minY) * scale) / 2;
            
            foreach (var screen in screens)
            {
                // Créer une bordure pour représenter l'écran
                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    Background = screen == SelectedScreen ? 
                        new SolidColorBrush(Color.FromRgb(51, 153, 255)) : // #3399FF
                        new SolidColorBrush(Color.FromRgb(224, 224, 224)), // #E0E0E0
                    BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)) // #CCCCCC
                };

                // Ajouter le numéro de l'écran
                var textBlock = new TextBlock
                {
                    Text = (screens.IndexOf(screen) + 1).ToString(),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                border.Child = textBlock;

                // Positionner et dimensionner la bordure
                double x = (screen.Bounds.X - minX) * scale + offsetX;
                double y = (screen.Bounds.Y - minY) * scale + offsetY;
                double width = screen.Bounds.Width * scale;
                double height = screen.Bounds.Height * scale;

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);
                border.Width = width;
                border.Height = height;

                // Ajouter des événements de clic
                border.MouseDown += (s, e) =>
                {
                    if (!UseMouseScreen)
                    {
                        screenComboBox.SelectedItem = screen;
                        SelectedScreen = screen;
                    }
                };

                screensCanvas.Children.Add(border);
                screenBorders.Add(border);
            }
        }
    }
} 