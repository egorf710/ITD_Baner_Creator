using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITD_Baner_Creator
{
    public partial class Form1 : Form
    {
        // ====== БАННЕР РЕДАКТОР ======
        Bitmap originalImage;
        Bitmap scaledImage;

        Rectangle cropRect = new Rectangle(0, 0, 186, 62);

        bool isDragging = false;
        Point dragStart;

        Bitmap lastSavedBanner;

        // ====== WEBVIEW + AUTH ======
        string bearerToken;
        string username;
        bool auth = false;
        public Form1()
        {
            InitializeComponent();


            this.Load += Form1_Load;
        }

        // ====== WEBVIEW LOGIN ======
        private async void Form1_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();

            webView21.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            auth = false;
            webView21.Source = new Uri("https://xn--d1ah4a.com/login");
        }

        private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {

            try
            {
                string url = e.Request.Uri;

                if (url.Contains("/api") && url.Contains("auth"))
                {
                    Stream stream = await e.Response.GetContentAsync();
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        string token = ExtractToken(json);


                        if (!string.IsNullOrEmpty(token))
                        {
                            bearerToken = token;
                            username = await GetUsernameFromApiAsync(bearerToken);
                            if (string.IsNullOrEmpty(username))
                            {
                                MessageBox.Show("Не удалось получить username.");
                                return;
                            }

                            webView21.CoreWebView2.WebResourceResponseReceived -= CoreWebView2_WebResourceResponseReceived;
                            webView21.Source = new Uri($"https://xn--d1ah4a.com/{username}");
                            if (!auth)
                            {
                                Invoke((Action)(() =>
                                {
                                    Text = "ИТД Баннер - от @sonata  — Авторизация OK";
                                    MessageBox.Show($"Авторизация успешна {username}");
                                }));

                                auth = true;
                            }
                        }
                    }
                }

            }
            catch
            {
                // игнорируем ошибки
            }

        }

        string ExtractToken(string json)
        {
            string key = "\"accessToken\":\"";
            int i = json.IndexOf(key);
            if (i < 0) return null;

            i += key.Length;
            int j = json.IndexOf("\"", i);
            if (j < 0) return null;

            return json.Substring(i, j - i);
        }

        private async Task<string> GetUsernameFromApiAsync(string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("https://xn--d1ah4a.com/api/profile");
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                var profile = ExtractUsername(json);

                return profile;
            }
        }
        string ExtractUsername(string json)
        {
            string key = "\"username\":\"";
            int i = json.IndexOf(key);
            if (i < 0) return null;

            i += key.Length;
            int j = json.IndexOf("\"", i);
            if (j < 0) return null;

            return json.Substring(i, j - i);
        }


        // ====== ЗАГРУЗКА КАРТИНКИ ======

        private void btnEditBanner_Click(object sender, EventArgs e)
        {
            using (BannerEditorForm editor = new BannerEditorForm())
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    lastSavedBanner?.Dispose();
                    lastSavedBanner = new Bitmap(editor.EditedBanner);
                    MessageBox.Show("Баннер готов к загрузке!");
                }
            }
        }


        // ====== ЗАГРУЗКА НА САЙТ ======

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (lastSavedBanner == null)
            {
                MessageBox.Show("Сначала отредактируйте баннер.");
                return;
            }
            if (webView21.Source != new Uri($"https://xn--d1ah4a.com/{username}"))
            {
                webView21.NavigationCompleted += WebView21_NavigationCompleted;
                webView21.Source = new Uri($"https://xn--d1ah4a.com/{username}");
            }
            else
            {
                await Task.Delay(500);
                // Снимаем обработчик, чтобы не срабатывать повторно
                webView21.NavigationCompleted -= WebView21_NavigationCompleted;

                using (MemoryStream ms = new MemoryStream())
                {
                    lastSavedBanner.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    string base64 = Convert.ToBase64String(ms.ToArray());

                    string js = $@"
                    (function insertBanner() {{
                        const btn = document.querySelector('.profile-banner__btn[title=""Нарисовать баннер""]');
                        if (btn) btn.click();

                        function draw() {{
                            const canvas = document.querySelector('.drawing-canvas');
                            if (!canvas) {{
                                setTimeout(draw, 100);
                                return;
                            }}
                            const ctx = canvas.getContext('2d');
                            const img = new Image();
                            img.onload = () => ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                            img.src = 'data:image/png;base64,{base64}';
                        }}
                        draw();
                    }})()
                    ";

                    await webView21.ExecuteScriptAsync(js);
                    MessageBox.Show("Баннер вставлен в canvas. Осталось нажать 'Сохранить' на сайте.");
                }
            }

        }
        private async void WebView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await Task.Delay(1000);
            // Снимаем обработчик, чтобы не срабатывать повторно
            webView21.NavigationCompleted -= WebView21_NavigationCompleted;

            using (MemoryStream ms = new MemoryStream())
            {
                lastSavedBanner.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                string base64 = Convert.ToBase64String(ms.ToArray());

                string js = $@"
        (function insertBanner() {{
            const btn = document.querySelector('.profile-banner__btn[title=""Нарисовать баннер""]');
            if (btn) btn.click();

            function draw() {{
                const canvas = document.querySelector('.drawing-canvas');
                if (!canvas) {{
                    setTimeout(draw, 100);
                    return;
                }}
                const ctx = canvas.getContext('2d');
                const img = new Image();
                img.onload = () => ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                img.src = 'data:image/png;base64,{base64}';
            }}
            draw();
        }})()
        ";

                await webView21.ExecuteScriptAsync(js);
                MessageBox.Show("Баннер вставлен в canvas. Осталось нажать 'Сохранить' на сайте.");
            }
        }

        private void rewardMe_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("https://boosty.to/sonata.realize/donate");
            Process.Start(sInfo);
        }
    }
}
