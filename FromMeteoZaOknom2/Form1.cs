using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Web.WebView2.WinForms;

namespace FromMeteoZaOknom2
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Label titleLabel;
        private readonly CultureInfo russianCulture = CultureInfo.CreateSpecificCulture("ru-RU");
        private readonly HttpClient httpClient = new HttpClient();
        private readonly Dictionary<int, Label[]> weatherPanels = new Dictionary<int, Label[]>();
        private readonly Dictionary<string, (string ip, int correction, Label label, int counter)> sensorConfig = new()
        {
            { "East", ("http://192.168.137.97:8097", 0, null /* label7 */, 0) },
            { "West", ("http://192.168.137.98:8098", 0, null /* label6 */, 0) },
            { "Inner", ("http://192.168.137.99:8099", 0, null /* label15 */, 0) }
        };
        private bool changeColorMessageEnable;
        private const string OpenWeatherApiUrl = "http://api.openweathermap.org/data/2.5/forecast/daily?q=Moscow,ru&mode=json&units=metric&lang=ru&cnt=10&APPID=274135263c7242c24cb8c6e893bb4706";
        private const string MeteoInfoUrl = "https://meteoinfo.ru/zaoknom";

        public Form1()
        {
            InitializeComponent();
            CacheWeatherPanels();
            SetBrowserFeatureControl();
            //this.Load += new System.EventHandler(this.Form1_Load); //масштабирование
            sensorConfig["East"] = (sensorConfig["East"].ip, sensorConfig["East"].correction, label7, sensorConfig["East"].counter);
            sensorConfig["West"] = (sensorConfig["West"].ip, sensorConfig["West"].correction, label6, sensorConfig["West"].counter);
            sensorConfig["Inner"] = (sensorConfig["Inner"].ip, sensorConfig["Inner"].correction, label15, sensorConfig["Inner"].counter);
        }

        private void CacheWeatherPanels()
        {
            var panels = Controls.OfType<Panel>().OrderBy(p => p.Name).ToList();
            for (int i = 0; i < panels.Count; i++)
            {
                weatherPanels[i] = panels[i].Controls.OfType<Label>().OrderBy(l => l.Name).ToArray();
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // Установить фиксированный размер формы 1920x1080 и стиль для разработки
            timerForSensors.Enabled = true;
            LogError("Таймер timerForSensors включён");
            timerForSensors.Interval = 10 * 1000; // 10 сек.
            timerForWeb.Enabled = true;
            LogError("Таймер timerForWeb включён");
            timerForWeb.Interval = 10 * 60000; // 10 мин.
            Cursor.Position = new Point(0, 0);
            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                await LoadSensorsDataAsync("East");
                await LoadSensorsDataAsync("West");
                await LoadSensorsDataAsync("Inner");
                //await Task.Delay(1000);
                await LoadFromMeteoAndWeatherSiteAsync();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка инициализации: {ex.Message}");
            }
            finally
            {
                timerFirstInit.Enabled = false;
            }
        }

        private async Task LoadFromMeteoAndWeatherSiteAsync()
        {
            if (BackColor == Color.Black) return;

            label8.Text = DateTime.Now.ToString("HH:mm", russianCulture);

            try
            {
                UpdateSunriseSunset();
                await UpdateWeatherForecastAsync();
                await UpdateMeteoInfoAsync();
            }
            catch (Exception ex)
            {
                LogError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void UpdateSunriseSunset()
        {
            DateTime date = DateTime.Today;
            DateTime sunrise = DateTime.Now, sunset = DateTime.Now;
            bool isSunrise = false, isSunset = false;

            SunTimes.Instance.CalculateSunRiseSetTimes(
                new SunTimes.LatitudeCoords(55, 45, 7, SunTimes.LatitudeCoords.Direction.North),
                new SunTimes.LongitudeCoords(37, 36, 56, SunTimes.LongitudeCoords.Direction.East),
                date, ref sunrise, ref sunset, ref isSunrise, ref isSunset);

            label10.Text = $"Сегодня: восход {sunrise:HH:mm}, закат {sunset:HH:mm}";
        }

        private async Task UpdateWeatherForecastAsync()
        {
            if (!await CheckUrlAsync(OpenWeatherApiUrl)) return;

            try
            {
                string json = await httpClient.GetStringAsync(OpenWeatherApiUrl);
                var data = JsonConvert.DeserializeObject<RootObject>(json);

                if (data?.list == null) throw new Exception("Неверный формат данных OpenWeather");

                int panelCount = weatherPanels.Count;
                for (int i = 0; i < panelCount && i < data.list.Count; i++)
                {
                    var forecast = data.list[i]; // Используем прямой порядок
                    var labels = weatherPanels[i];

                    labels[6].Text = UnixDateTimeToDateTime(forecast.dt.ToString()).ToString("d MMMM, dddd", russianCulture);
                    labels[5].BackgroundImage = weatherPicture.WeatherIconToPicture(forecast.weather[0].icon);

                    int nightTemp = Convert.ToInt32(Math.Round(forecast.temp.night));
                    labels[4].Text = nightTemp >= 0 ? $"+{nightTemp}°" : $"{nightTemp}°";

                    labels[3].Text = $"{Convert.ToInt32(forecast.pressure * 0.7501)} мм";

                    int dayTemp = Convert.ToInt32(Math.Round(forecast.temp.day));
                    labels[2].Text = dayTemp >= 0 ? $"+{dayTemp}°" : $"{dayTemp}°";

                    labels[1].Text = WindDirection.getWindDirection(forecast.deg);
                    labels[0].Text = forecast.weather[0].description;
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка OpenWeather: {ex.Message}");
            }
        }

        private async Task UpdateMeteoInfoAsync()
        {
            if (!await CheckUrlAsync(MeteoInfoUrl))
            {
                webView21.Visible = false;
                webView22.Visible = false;
                return;
            }

            webView21.Visible = true;
            webView22.Visible = true;
          

            try
            {
                await NavigateWebView2Async(webView21, MeteoInfoUrl, 480);
                await NavigateWebView2Async(webView22, MeteoInfoUrl, 1025);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка MeteoInfo: {ex.Message}");
                webView21.Visible = false;
                webView22.Visible = false;
            }
        }

        private async Task NavigateWebView2Async(WebView2 browser, string url, int scrollY)
          {
          try
          {
            LogError($"Начало навигации WebView2: {url}, scrollY={scrollY}");
            await browser.EnsureCoreWebView2Async();
            browser.CoreWebView2.Navigate(url);

            // Ожидание полной загрузки
            var tcs = new TaskCompletionSource<bool>();
            browser.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (e.IsSuccess)
                    tcs.TrySetResult(true);
                else
                    tcs.TrySetException(new Exception($"Navigation failed: {e.WebErrorStatus}"));
            };
            await tcs.Task;

            // Задержка для динамической загрузки
            await Task.Delay(1000);

            // Прокрутка
            await browser.CoreWebView2.ExecuteScriptAsync($"window.scrollTo(10, {scrollY});");
            LogError($"Прокрутка к (10, {scrollY})");

            // Скрытие элемента jm-back-top
            for (int i = 0; i < 3; i++)
            {
                var elementExists = await browser.CoreWebView2.ExecuteScriptAsync("document.getElementById('jm-back-top') !== null");
                if (elementExists == "true")
                {
                    await browser.CoreWebView2.ExecuteScriptAsync(@"
                    var element = document.getElementById('jm-back-top');
                    element.style.setProperty('display', 'none', 'important');
                    element.style.visibility = 'hidden';
                    element.style.opacity = '0';
                    element.parentNode.removeChild(element);
                ");
                    LogError($"Элемент jm-back-top скрыт и удалён на попытке {i + 1}");
                }
                else
                {
                    LogError($"Элемент jm-back-top не найден на странице на попытке {i + 1}");
                    break;
                }
                await Task.Delay(500);
            }

            // Установка прозрачного фона для всех ключевых элементов
            await browser.CoreWebView2.ExecuteScriptAsync(@"
            // Список селекторов
            var selectors = [
                '.sticky-bar',
                '.dj-offcanvas-wrapper',
                '.dj-offcanvas-pusher',
                '.jm-top-bar',
                '.mod-search-searchword91',
                '.jm-bar',
                '.jm-footer-menu-bg',
                '.jm-footer-in',
                '[class*=""jm-""]',
                '[class*=""dj-""]',
                'div[style*=""background-color""]',
                'section[style*=""background-color""]'
            ].join(', ');

            // Установка прозрачного фона
            var elements = document.querySelectorAll(selectors);
            elements.forEach(el => {
                el.style.setProperty('background-color', 'transparent', 'important');
                el.style.setProperty('background-image', 'none', 'important');
            });

            // Прозрачность для html и body
            document.documentElement.style.backgroundColor = 'transparent';
            document.body.style.backgroundColor = 'transparent';

            // Глобальный CSS для переопределения
            var style = document.createElement('style');
            style.innerHTML = `
                html, body, .sticky-bar, .dj-offcanvas-wrapper, .dj-offcanvas-pusher,
                .jm-top-bar, .mod-search-searchword91, .jm-bar, .jm-footer-menu-bg, .jm-footer-in,
                [class*=""jm-""], [class*=""dj-""], div[style*=""background-color""], section[style*=""background-color""] {
                    background-color: transparent !important;
                    background-image: none !important;
                }
                html, body {
                    overflow: hidden !important;
                 }
            `;
            document.head.appendChild(style);

            // Логирование найденных элементов
            var foundElements = Array.from(elements).map(el => ({
                tag: el.tagName,
                id: el.id,
                class: el.className,
                background: window.getComputedStyle(el).backgroundColor
            }));
            JSON.stringify(foundElements);
        ");
            var foundElements = await browser.CoreWebView2.ExecuteScriptAsync("JSON.stringify(foundElements)");
            LogError($"Найденные элементы с фонами: {foundElements}");

            // Проверка оставшихся непрозрачных элементов
            var remainingOpaque = await browser.CoreWebView2.ExecuteScriptAsync(@"
            Array.from(document.querySelectorAll('*'))
                .filter(el => window.getComputedStyle(el).backgroundColor !== 'rgba(0, 0, 0, 0)' && window.getComputedStyle(el).backgroundColor !== 'transparent')
                .map(el => ({ tag: el.tagName, id: el.id, class: el.className, background: window.getComputedStyle(el).backgroundColor }))
                .join(', ')
        ");
            LogError($"Оставшиеся непрозрачные элементы: {remainingOpaque}");

            // Скрытие полос прокрутки
            await browser.CoreWebView2.ExecuteScriptAsync(@"
            document.documentElement.style.scrollbarWidth = 'none';
            document.body.style.scrollbarWidth = 'none';
            document.documentElement.style.msOverflowStyle = 'none';
            document.body.style.msOverflowStyle = 'none';
            var style = document.createElement('style');
            style.innerHTML = `::-webkit-scrollbar { display: none; }`;
            document.head.appendChild(style);
        ");
            LogError("Полосы прокрутки скрыты");

            // Проверка текущей позиции прокрутки
            var scrollYActual = await browser.CoreWebView2.ExecuteScriptAsync("window.scrollY");
            LogError($"Текущая позиция прокрутки: {scrollYActual}");

            var pageHeight = await browser.CoreWebView2.ExecuteScriptAsync("document.body.scrollHeight");
            LogError($"Высота страницы: {pageHeight}");
        }
        catch (Exception ex)
        {
            LogError($"Ошибка в NavigateWebView2Async: {ex.Message}");
        }
    }
    private async Task<bool> CheckUrlAsync(string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

             private async Task LoadSensorsDataAsync(string sensor)
        {
            const int maxAttempts = 4;
            const string errorDisplay = "CoEr";
            const string sensorError = "SEr";

            var config = sensorConfig[sensor];
            try
            {
                if (sensor == "Inner")
                {
                    string data = await FetchInnerDataAsync(config.ip, config.correction);
                    string[] dataParts = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Исправлено

                    if (dataParts[0] == "-145")
                    {
                        config.label.Text = sensorError;
                        label76.Text = "---";
                        label2.Text = "---";
                        label4.Text = "---";
                    }
                    else
                    {
                        config.label.Text = $"{dataParts[0]}˚";
                        label76.Text = $"вл. {dataParts[1]}%";
                        label2.Text = dataParts[2];
                        if (int.TryParse(dataParts[2], out int pressure))
                        {
                            label4.Text = (pressure / 1.33333333).ToString("0");
                            label2.Text += " гПа";
                        }
                        else
                        {
                            label4.Text = "---";
                        }
                    }
                }
                else
                {
                    string temp = await FetchTemperatureAsync(config.ip, config.correction);
                    if (temp == "------")
                    {
                        config = (config.ip, config.correction, config.label, config.counter + 1);
                        if (config.counter >= maxAttempts)
                        {
                            config.label.Text = errorDisplay;
                            config = (config.ip, config.correction, config.label, 0);
                        }
                    }
                    else
                    {
                        config.label.Text = $"{temp}˚";
                        config = (config.ip, config.correction, config.label, 0);
                    }
                    sensorConfig[sensor] = config;
                }
            }
            catch (Exception ex)
            {
                LogError($"Ошибка загрузки {sensor}: {ex.Message}");
                if (sensor == "Inner")
                {
                    config.label.Text = errorDisplay;
                    label2.Text = "---";
                    label76.Text = "---";
                    label4.Text = "---";
                }
            }
        }

        private async Task<string> FetchTemperatureAsync(string ip, int correction)
        {
            if (!await PingServerAsync(ip.Substring(7, ip.IndexOf(':', 7) - 7)))
                return "------";

            try
            {
                string response = await httpClient.GetStringAsync(ip);
                if (response == "SEr") return "------";
                if (int.TryParse(response, out int temp))
                    return (temp + correction).ToString();
                return "------";
            }
            catch
            {
                return "------";
            }
        }

        private async Task<string> FetchInnerDataAsync(string ip, int correction)
        {
            if (!await PingServerAsync(ip.Substring(7, ip.IndexOf(':', 7) - 7)))
                return "--- --- ----";

            try
            {
                string response = await httpClient.GetStringAsync(ip);
                string[] parts = response.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Исправлено
                if (parts.Length < 3 || !int.TryParse(parts[0], out int temp))
                    return "--- --- ----";
                parts[0] = (temp + correction).ToString();
                return string.Join(" ", parts);
            }
            catch
            {
                return "--- --- ----";
            }
        }

        private async Task<bool> PingServerAsync(string address)
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = await ping.SendPingAsync(address, 200);
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void timerOneTickPerSecond_Tick(object sender, EventArgs e)
        {
            //ServiceLabel.Text = "вход в timerOneTickPerSecond";
            if (label18.Tag == null || label18.Tag.ToString() != timerForWeb.Interval.ToString())
            {
                label18.Text = $"инт. {timerForWeb.Interval / 60000} мин.";
                label18.Tag = timerForWeb.Interval.ToString();
                //ServiceLabel.Text = timerOneTickPerMinute.Interval.ToString();
            }

            int currentHour = DateTime.Now.Hour;
            bool isNight = currentHour > 22 || currentHour < 6;
            if (isNight && BackColor != Color.Black)
                NightPattern();
            else if (!isNight && BackColor == Color.Black)
                DayPattern();

            DateTime now = DateTime.Now;
            label3.Text = now.ToString("d MMMM yyyy, dddd", russianCulture);
            label14.Text = now.ToString("HH:mm:ss", russianCulture);

            changeMessageColor();
        }

        private async void timerForWeb_Tick(object sender, EventArgs e)
        {
            //ServiceLabel.Text = "вход в timerForWeb";
            try
            {
                await LoadFromMeteoAndWeatherSiteAsync();

            }
            catch (Exception ex)
            {
                LogError($"Ошибка таймера погоды: {ex.Message}");
            }
        }

        private async void timerForSensors_Tick(object sender, EventArgs e)
        {
            //ServiceLabel.Text = "вход в timerForSensors";
            string[] regions = { "East", "West", "Inner" };
            foreach (var region in regions)
            {
                try
                {
                    await LoadSensorsDataAsync(region);
                    //await Task.Delay(10000);
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка датчика {region}: {ex.Message}");
                }
            }
        }
        private void changeMessageColor()
        {
            label9.BackColor = changeColorMessageEnable
                ? label9.BackColor == Color.Coral ? Color.Transparent : Color.Coral
                : Color.Transparent;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            changeColorMessageEnable = checkBox1.Checked;
            if (!checkBox1.Checked)
                label9.BackColor = Color.Transparent;
        }

        private void NightPattern()
        {
            try
            {
                Cursor.Hide();
                BackColor = Color.Black;
                ForeColor = Color.FromArgb(64, 64, 64);
                BackgroundImage = Properties.Resources.NightForMeteo;
                label9.ForeColor = Color.FromArgb(64, 64, 64);
                var controls = new List<Control> { webView21, webView22, label10, label16, label1, label8, label9, label18, checkBox1 };
                controls.ForEach(control => control.Visible = false);
                Controls.OfType<Panel>().ToList().ForEach(panel => panel.Visible = false);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка ночного режима: {ex.Message}");
                label13.Text = "Ошибка переключения в ночной режим";
            }
        }

        private void DayPattern()
        {
            try
            {
                Cursor.Show();
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                BackgroundImage = Properties.Resources.backForMeteo2;
                label9.ForeColor = SystemColors.ControlText;
                var controls = new List<Control> { webView21, webView22, label10, label16, label1, label8, label9, label18, checkBox1 };
                controls.ForEach(control => control.Visible = true);
                Controls.OfType<Panel>().ToList().ForEach(panel => panel.Visible = true);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка дневного режима: {ex.Message}");
                label13.Text = "Ошибка переключения в дневной режим";
            }
        }

        private void label16_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void label18_Click(object sender, EventArgs e)
        {
            const int OneMinuteMs = 60000;
            const int TenMinutesMs = 600000;
            timerForWeb.Interval = timerForWeb.Interval == OneMinuteMs ? TenMinutesMs : OneMinuteMs;
            label18.Text = $"инт. {timerForWeb.Interval / OneMinuteMs} мин.";
        }

        private void label14_Click(object sender, EventArgs e)
        {
            timerOneTickPerSecond.Enabled = !timerOneTickPerSecond.Enabled;
        }

        /*private void WriteLog()
        {
            try
            {
                File.AppendAllText(@"D:\MeteoLog.txt", $"{DateTime.Now}\n");
            }
            catch (Exception ex)
            {
                LogError($"Ошибка логирования: {ex.Message}"); 
            }
        }
        */
        private void LogError(string message)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss}: {message}");
            //ServiceLabel.Text += $"{DateTime.Now:HH:mm:ss}: {message}\r\n";//  \r\n после {message} для перевода строки
        }

        private static DateTime UnixDateTimeToDateTime(string unixDate)
        {
            try
            {
                long unixMs = Convert.ToInt64(unixDate) * 1000;
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixMs).ToLocalTime();
            }
            catch
            {
                return DateTime.Now;
            }
        }

        // Заглушки для отсутствующих классов
        private class RootObject
        {
            public List<Forecast> list { get; set; }
        }

        private class Forecast
        {
            public long dt { get; set; }
            public Temperature temp { get; set; }
            public double pressure { get; set; }
            public int deg { get; set; }
            public List<Weather> weather { get; set; }
        }

        private class Temperature
        {
            public double day { get; set; }
            public double night { get; set; }
        }

        private class Weather
        {
            public string icon { get; set; }
            public string description { get; set; }
        }

        private void SetBrowserFeatureControl()
        {
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            if (string.Equals(fileName, "devenv.exe", StringComparison.OrdinalIgnoreCase) || string.Equals(fileName, "XDesProc.exe", StringComparison.OrdinalIgnoreCase))
                return;

            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode());
        }

        private void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key?.SetValue(appName, value, RegistryValueKind.DWord);
            }
        }

        private uint GetBrowserEmulationMode()
        {
            int browserVersion = 7;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer", RegistryKeyPermissionCheck.ReadSubTree))
            {
                var version = ieKey?.GetValue("svcVersion") ?? ieKey?.GetValue("Version");
                if (version != null)
                    int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }

            return browserVersion switch
            {
                7 => 7000,
                8 => 8000,
                9 => 9000,
                10 => 10000,
                _ => 11000
            };
        }

  
    }
}