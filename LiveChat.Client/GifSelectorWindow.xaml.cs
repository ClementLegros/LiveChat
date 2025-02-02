using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using WpfAnimatedGif;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace LiveChat.Client
{
    public partial class GifSelectorWindow : Window
    {
        private const string GIPHY_API_URL = "https://api.giphy.com/v1/gifs/search";
        private readonly HttpClient httpClient = new HttpClient();
        public string SelectedGifUrl { get; private set; }
        private readonly List<string> tempFiles = new List<string>();
        private readonly Dictionary<Image, BitmapImage> imageCache = new Dictionary<Image, BitmapImage>();

        public GifSelectorWindow()
        {
            InitializeComponent();
            Closing += GifSelectorWindow_Closing;
        }

        private void GifSelectorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var file in tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch { }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchGifs();
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SearchGifs();
            }
        }

        private async Task SearchGifs()
        {
            try
            {
                // Nettoyage
                foreach (var file in tempFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch { }
                }
                tempFiles.Clear();
                imageCache.Clear();
                gifContainer.Children.Clear();

                string apiKey = ConfigurationManager.AppSettings["GiphyApiKey"];
                string searchQuery = searchBox.Text;
                string url = $"{GIPHY_API_URL}?api_key={apiKey}&q={Uri.EscapeDataString(searchQuery)}&limit=20&rating=g";

                string response = await httpClient.GetStringAsync(url);
                JObject jsonResponse = JObject.Parse(response);
                JArray results = (JArray)jsonResponse["data"];

                foreach (JToken result in results)
                {
                    string animatedUrl = result["images"]["fixed_height"]["url"].ToString();
                    string originalGifUrl = result["images"]["original"]["url"].ToString();

                    // Téléchargement du GIF
                    string tempFile = Path.Combine(Path.GetTempPath(), $"giphy_{Guid.NewGuid()}.gif");
                    tempFiles.Add(tempFile);

                    using (var webClient = new System.Net.WebClient())
                    {
                        await webClient.DownloadFileTaskAsync(new Uri(animatedUrl), tempFile);
                    }

                    var image = new Image
                    {
                        Width = 200,
                        Height = 200,
                        Margin = new Thickness(5),
                        Cursor = Cursors.Hand,
                        Tag = originalGifUrl,
                        Stretch = Stretch.Uniform
                    };
                    
                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(tempFile);
                    bitmap.EndInit();
                    imageCache[image] = bitmap;

                    image.Source = bitmap;
                    ImageBehavior.SetAnimatedSource(image, bitmap);

                    image.MouseEnter += (s, e) =>
                    {
                        var img = s as Image;
                        if (img != null && imageCache.ContainsKey(img))
                        {
                            ImageBehavior.SetAnimatedSource(img, imageCache[img]);
                        }
                    };

                    image.MouseLeave += (s, e) =>
                    {
                        var img = s as Image;
                        if (img != null)
                        {
                            ImageBehavior.SetAnimatedSource(img, null);
                            img.Source = imageCache[img];
                        }
                    };

                    image.MouseDown += (s, e) =>
                    {
                        SelectedGifUrl = (s as Image)?.Tag as string;
                        DialogResult = true;
                        Close();
                    };

                    gifContainer.Children.Add(image);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching GIFs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 