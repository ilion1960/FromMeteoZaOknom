using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Net.NetworkInformation;



namespace FromMeteoZaOknom2
{


    public partial class Form1 : Form
    {

        [DllImport("KERNEL32.DLL", EntryPoint = "SetProcessWorkingSetSize", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool SetProcessWorkingSetSize(IntPtr pProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("KERNEL32.DLL", EntryPoint = "GetCurrentProcess", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GetCurrentProcess();

        public static int i = 0; // сервисный счетчик  
        public static Boolean changeColorMessageEnable;
      //public static int countSerialPortError = 0;
        public static Bitmap w_image;
        
        // Загрузка файла D:\thermometer.ini    

        public static void WriteLog()
        {

            StreamWriter writer = new StreamWriter(@"D:\MeteoLog.txt", true);
            writer.WriteLine(DateTime.Now.ToString());
            writer.Close();
                       
        }

        // преобразование UNIX времеми в обычные дату и время
        static DateTime UnixDateTimeToDateTime(string UnixDate)
        {
            long unix_dt = Convert.ToInt64(UnixDate) * 1000;
            DateTime dt = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddMilliseconds(unix_dt);
            return dt;
        }
        
        public static string LoadIniFile(String str)
        {
            string comPort = "";
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(str);
                comPort = file.ReadLine();
                file.Close();
            }
            catch
            {
                comPort = "Не найден файл " + str;
            }
            return comPort;
        }
        
        public Form1()
        {
           
            InitializeComponent();
            SetBrowserFeatureControl();
                        
        }

        protected override bool ProcessCmdKey(ref Message m, Keys keyData)
        {
            bool blnProcess = false;
            if (keyData == Keys.Escape) // по Escape закрываем эту программу
            {
                blnProcess = true;
                this.Close();           // закрыть
            }
            if (keyData == Keys.A)      // по кл. А вызываем внешнюю программу
            {
                blnProcess = true;
                Process proc = new Process();
                proc.StartInfo.FileName = @"C:\Program Files\Vimicro Corporation\VMUVC\amcap.exe";
                proc.StartInfo.Arguments = @"";
                proc.Start();
            }
            if (blnProcess == true)
                return true;
            else
                return base.ProcessCmdKey(ref m, keyData);
        }

        /*private void Key(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                Form Form2 = new Form();
                Process proc = new Process();
                proc.StartInfo.FileName = @"C:\Program Files\Vimicro Corporation\VMUVC\amcap.exe";
                proc.StartInfo.Arguments = "";
                proc.Start();
                Form2.Show();
             }
        }*/
        
        
        private void Form1_Load(object sender, EventArgs e)
        {
            WriteLog();
            //this.KeyDown += new KeyEventHandler(Key); // в свойствах формы Form1 необходимо установить KeyPreview в true
            System.Windows.Forms.Cursor.Position = new Point(0, 0);
            timer1.Enabled = true;
            //webBrowser1.Visible = false;
            //label10.Visible = true;
         
        }

                
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            timer1.Enabled = false;  // срабатывает 1 раз при загрузке программы
            loadSensorsData();
            Wait(1000);
            loadFromMeteoAndWeatherSite();
        }

        private void loadFromMeteoAndWeatherSite()
        {
            //IntPtr pHandle = GetCurrentProcess(); // обнуляем кэш браузера
            //SetProcessWorkingSetSize(pHandle, -1, -1); // обнуляем кэш браузера
            label8.Text = DateTime.Now.ToShortTimeString();
            //webBrowser1.Visible = true;
            //label10.Visible = false;
            webBrowser2.Navigate("about:blank"); // обнуляем кэш браузера
            webBrowser3.Navigate("about:blank"); // обнуляем кэш браузера

            DateTime date = DateTime.Today;
            bool isSunrise = false; bool isSunset = false;
            DateTime sunrise = DateTime.Now; DateTime sunset = DateTime.Now;
            // Coordinates of Moscow
            SunTimes.Instance.CalculateSunRiseSetTimes(new SunTimes.LatitudeCoords(55, 45, 7, SunTimes.LatitudeCoords.Direction.North),
                                                       new SunTimes.LongitudeCoords(37, 36, 56, SunTimes.LongitudeCoords.Direction.East),
                                                       date, ref sunrise, ref sunset, ref isSunrise, ref isSunset);

            label10.Text = "Сегодня: восход " + sunrise.ToString("HH:mm") + ", закат " + sunset.ToString("HH:mm");

            if (BackColor != System.Drawing.Color.Black)
            {

                string openweather_api_string = @"http://api.openweathermap.org/data/2.5/forecast/daily?q=Moscow,ru&mode=json&units=metric&lang=ru&cnt=10&APPID=274135263c7242c24cb8c6e893bb4706";
                if (CheckURL(openweather_api_string)) // ==========
                {
                    //webBrowser1.Visible = true;
                   
                        //MessageBox.Show("openweather доступен");
                        string json = GetJSON.getJSONstring(openweather_api_string);
                        RootObject data = JsonConvert.DeserializeObject<RootObject>(json);
                        //label19.Text = UnixDateTimeToDateTime(data.list[1].dt.ToString()).ToString("dd MMMM, dddd", CultureInfo.CreateSpecificCulture("ru-RU"));
                   
                    // Считаем panel
                    List<Control> panels = new List<Control>();
                    foreach (Control control in this.Controls)
                    {
                        if (control.GetType() == typeof(Panel))
                            panels.Add(control);
                    }

                    int pcount = panels.Count(); // число panels прогноза
                    for (int i = pcount-1; i >= 0; i--)
                    {
                        // считаем label on panel
                        List<Control> panelLabels = new List<Control>();
                        foreach (Control control in panels[i].Controls)
                        {
                            if (control.GetType() == typeof(Label))
                                panelLabels.Add(control);
                        }

                        
                        panelLabels[6].Text = UnixDateTimeToDateTime(data.list[pcount-i-1].dt.ToString()).ToString("d MMMM, dddd", CultureInfo.CreateSpecificCulture("ru-RU")); // дата   
                        panelLabels[5].BackgroundImage = weatherPicture.WeatherIconToPicture(data.list[pcount - i-1].weather[0].icon); // картинка погоды 
                        
                        string plus = " ";
                        int tnight = Convert.ToInt32(Math.Round(data.list[pcount - i-1].temp.night));
                        if (tnight == 0) plus = "  ";
                        if (tnight >= 1) plus = "+";
                        panelLabels[4].Text = plus + tnight.ToString("0") + "°"; // температура ночью
                        
                        panelLabels[3].Text = Convert.ToInt32(data.list[pcount - i-1].pressure * 0.7501).ToString("0") + " мм"; // давление
                        
                        plus = " ";
                        int tday = Convert.ToInt32(Math.Round(data.list[pcount - i-1].temp.day));
                        if (tday == 0) plus = "  ";
                        if (tday >= 1) plus = "+";
                        panelLabels[2].Text = plus + tday.ToString("0") + "°"; // температура днем
                        
                        panelLabels[1].Text = WindDirection.getWindDirection(data.list[pcount - i-1].deg); // ветер
                        panelLabels[0].Text = data.list[pcount - i-1].weather[0].description; // описание погоды

                     }

                    
                }
                else
                {
                    //webBrowser1.Visible = false;
                    //MessageBox.Show("openweather недоступен");
                }

                if (CheckURL(@"https://meteoinfo.ru/zaoknom"))
                {
                    webBrowser2.Visible = true;
                    webBrowser3.Visible = true;
                    webBrowser2.ScrollBarsEnabled = false;
                    webBrowser3.ScrollBarsEnabled = false;
                    try
                    {

                        while (webBrowser2.ReadyState != WebBrowserReadyState.Complete) Application.DoEvents();
                        //if (!webBrowser2.IsBusy) webBrowser2.Refresh();
                        webBrowser2.Navigate(@"https://meteoinfo.ru/zaoknom");
                        webBrowser2.Document.Window.ScrollTo(10, 480);               //вднх 280; киевская, краснопресненская 480
                        Wait(2000);
                        webBrowser2.Document.GetElementById("jm-back-top").Style = "display: none;";
                        //webBrowser2.Refresh();
                        
                        
                                                
                        while (webBrowser3.ReadyState != WebBrowserReadyState.Complete) Application.DoEvents();
                        //if (!webBrowser3.IsBusy) webBrowser3.Refresh();
                        webBrowser3.Navigate(@"https://meteoinfo.ru/zaoknom");
                        webBrowser3.Document.Window.ScrollTo(0, 1025);              //вднх 805; киевская, краснопресненская 1025
                        Wait(2000);
                        webBrowser3.Document.GetElementById("jm-back-top").Style = "display: none;";
                        //webBrowser3.Refresh();
                        
                    }
                    catch { }
                }
                else
                {
                    webBrowser2.Visible = false;
                    webBrowser3.Visible = false;
                }
            }
            
        } //End of loadFromMeteoAndWeatherSite()
        
        public static void Wait(int WaitMilliSeconds)
        {
            DateTime timeout = DateTime.Now.AddMilliseconds(WaitMilliSeconds);
            while (DateTime.Now < timeout)
            {
                Application.DoEvents();
            }
        }
        
        
        // время последней загрузки с сайта meteo и openweather, срабатывает раз в 1 мин. / раз в 10 мин.
        private void timerOneTickPerMinute_Tick(object sender, EventArgs e)
        {

            try
            {
                loadFromMeteoAndWeatherSite();
            }

            catch { }
        }

        //Таймер для опроса датчиков каждые 20 сек.
        private void timerForSensorsOneTickPer20sec_Tick(object sender, EventArgs e)
        {
            try
            {
                loadSensorsData();
            }

            catch { }

        }

        private void loadSensorsData()
        {
            string str = "---";
            string outsideEastIP = "http://192.168.137.97:8097";
            //MessageBox.Show(outsideEastIP.Substring(7,14));
            string outsideWestIP = "http://192.168.137.98:8098";
            string innerIP = "http://192.168.137.99:8099";
            int correctionOutsideEast = 0; /////////corr//////////////////////////////
            int correctionOutsideWest = 0; /////////////corr//////////////////////////
            int correctionInner = 0; ///////////////////////corr//////////////////////

            // температура с востока, вывод на форму

            try
            {
                str = tempFrom18B20(outsideEastIP, correctionOutsideEast);
            }
            catch { label7.Text = "CoEr"; }
            //MessageBox.Show(str); 
            label7.Text = str + "˚"; // East (3 символа +  "˚"
            Wait(200);

            // температура с запада, вывод на форму

            try
            {
                str = tempFrom18B20(outsideWestIP, correctionOutsideWest);
            }
            catch { label6.Text = "CoEr"; }
            label6.Text = str + "˚"; // West (3 символа +  "˚")
            Wait(200);

            // температура, давление в гПа, влажность внутри, вывод на форму

            try
            {
                str = dataFromBME280(innerIP, correctionInner);
            }
            catch 
                {
                    label15.Text = "CoEr";
                    label2.Text = "---"; // pressure(4 символа)
                    label76.Text = "---"; // Humidity (3 символа +  "%")
                    label4.Text = "---";
                }
            
            string[] dataStr = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (dataStr[0] == "-145")
            {
                label15.Text = "SEr"; // Inner temperature(3 символа +  "˚")
                label76.Text = "---"; // Humidity (3 символа +  "%")
                label2.Text = "---"; // pressure(4 символа)
                label4.Text = "---";
            }
            else
            {
                label15.Text = dataStr[0] + "˚"; // Inner temperature(3 символа +  "˚")
                label76.Text = "вл. " + dataStr[1] + "%"; // Humidity (3 символа +  "%")
                label2.Text = dataStr[2]; // pressure(4 символа)

            }
             //label4.Text = "760";    // Pressure (3 символа)
            try
            {
                int press = Convert.ToInt32(label2.Text);
                label4.Text = (press / 1.33333333).ToString("0");
                label2.Text += " гПа";
            }
            catch { }
        }

        private string tempFrom18B20(string sensorIP, int correction)
        {
            //string temperature = "-2";
            int temperatureInt = 0;
            string temperature = "------";
            var webClient = new WebClient();
            try
            {
                temperature = webClient.DownloadString(sensorIP);
            }
            catch { }
            //MessageBox.Show(temperature);
            try
            {
                if (temperature != "SEr   ")
                {
                    temperatureInt = Convert.ToInt32(temperature) + correction;
                    temperature = Convert.ToString(temperatureInt);
                    //if (temperatureInt > 0) temperature = "+" + temperature;

                }
                else temperature = temperature.Trim();
            }
            catch { }
            return temperature;
        }

        private string dataFromBME280(string sensorIP, int correctionTempBME280)
        {
            //string data = "-22 100 1234";
            int temperatureInt = 0;
            var webClient = new WebClient();
            string data = webClient.DownloadString(sensorIP);
            string[] tempStr = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {

                temperatureInt = Convert.ToInt32(tempStr[0]) + correctionTempBME280;
                tempStr[0] = Convert.ToString(temperatureInt);
                data = tempStr[0] + ' ' + tempStr[1] + ' ' + tempStr[2];
            }
            catch { }
            //MessageBox.Show(data);
            return data;
        }


        // текущее время, срабатывает раз в секунду
        private void timerOneTickPerSecond_Tick(object sender, EventArgs e)
        {
            //i++;

            /*try
            {
                serialPort1.Write("a");
            }
            catch { }*/
            label18.Text = "инт. " + (timerOneTickPerMinute.Interval/60000).ToString() + " мин.";
            if (((DateTime.Now.Hour > 22) || (DateTime.Now.Hour < 6)) && (BackColor != System.Drawing.Color.Black)) NightPattern();
            if (((DateTime.Now.Hour > 5) && (DateTime.Now.Hour < 23)) && (BackColor == System.Drawing.Color.Black)) DayPattern();
            
            label3.Text = DateTime.Now.ToString("  d MMMM yyyy, dddd",CultureInfo.CreateSpecificCulture("ru-RU"));
            //label3.Text = DateTime.Now.ToString(" 29 сентября 2099, воскресенье", CultureInfo.CreateSpecificCulture("ru-RU"));
            label14.Text = DateTime.Now.ToString("   HH:mm:ss", CultureInfo.CreateSpecificCulture("ru-RU"));
            //label14.Text = "   00:00:00";
            // d MMMM yyyy - 1 сентября 2014 (сентября - 8 симв.)
            // dddd - полное название дня (воскресенье - 11 симв.)
            // HH:mm:ss - 01:11:58
            //label7.Text = "+14˚";
            //label6.Text = "+16˚";
            //label15.Text = "+26˚";
            changeMessageColor();
               
            }
 
        private void changeMessageColor()
        {
            if (changeColorMessageEnable)
            {
                if (label9.BackColor.Name == "Coral")
                    label9.BackColor = System.Drawing.Color.Transparent;
                else
                    label9.BackColor = System.Drawing.Color.Coral;
            }
            else label9.BackColor = System.Drawing.Color.Transparent;
        }

        
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                changeColorMessageEnable = true;
            }
            else
            {
                changeColorMessageEnable = false;
                label9.BackColor = System.Drawing.Color.Transparent;
            }
        }

        // Функция возвращает true в случае если адрес url доступен и false если адрес не существует.
        static bool CheckURL(String url)
        {
            if (String.IsNullOrEmpty(url))
                return false;
            WebRequest request = WebRequest.Create(url);
            try
            {
                HttpWebResponse res = request.GetResponse() as HttpWebResponse;
                if (res.StatusDescription == "OK")
                {
                    return true;
                }
            }
            catch { }
            return false;
        }
        
      //Проверка доступности сервера (ping)
        public bool ping_server(string server_address)
        {
            bool result = false;
            Ping Pinger = new Ping();
            PingReply Reply = Pinger.Send(server_address, 200) ; //TTL = 200 ms
            if (Reply.Status.ToString() == "TimedOut") result = false;
            if (Reply.Status.ToString() == "Success") result = true;
            //MessageBox.Show(result.ToString());
            return result;
        }
        
        private void NightPattern()
        {
            try
            {
                System.Windows.Forms.Cursor.Hide();
                webBrowser2.Visible = false;
                webBrowser3.Visible = false;
                label10.Visible = false; // "Ошибка загрузки прогноза" 
                label17.Visible = false; // "Данные с метеостанции не поступают" 
                label16.Visible = false; // домик
                label1.Visible = false;
                label8.Visible = false;
                label9.Visible = false;
                label18.Visible = false;
                BackgroundImage = FromMeteoZaOknom2.Properties.Resources.NightForMeteo;
                BackColor = System.Drawing.Color.Black;
                ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
                checkBox1.Visible = false;
                label9.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
                /*List<Control> panels = new List<Control>();*/
                foreach (Control control in this.Controls)
                {
                    if (control.GetType() == typeof(Panel))
                        control.Visible = false;
                }
                
                
            }
            catch { label13.Text = "Ошибка переключения в ночной режим"; }
            
        }

        private void DayPattern()
        {
            try
            {
                System.Windows.Forms.Cursor.Show();
                BackgroundImage = FromMeteoZaOknom2.Properties.Resources.backForMeteo2;
                BackColor = System.Drawing.SystemColors.Control;
                ForeColor = System.Drawing.SystemColors.ControlText;
                webBrowser2.Visible = true;
                webBrowser3.Visible = true;
                label10.Visible = true; // "Ошибка загрузки прогноза" 
                label17.Visible = true; // "Данные с метеостанции не поступают" 
                label16.Visible = true; // домик
                label1.Visible = true;
                label8.Visible = true;
                label9.Visible = true;
                label18.Visible = true;
                checkBox1.Visible = true;
                label9.ForeColor = System.Drawing.SystemColors.ControlText;
                foreach (Control control in this.Controls)
                {
                    if (control.GetType() == typeof(Panel))
                        control.Visible = true;
                }
                
             }
            catch { label13.Text = "Ошибка переключения в дневной режим"; }
        }

        // закрыть программу - щелчок на домике
        private void label16_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // переключение интервала загрузки погоды с сайтов: 1 мин. или 10 мин.
        private void label18_Click(object sender, EventArgs e)
        {
            if (timerOneTickPerMinute.Interval == 60000)
            {
                timerOneTickPerMinute.Interval = 600000;
                label18.Text = "инт. " + (timerOneTickPerMinute.Interval / 60000).ToString() + " мин.";
                return;
            }
            if (timerOneTickPerMinute.Interval == 600000)
            {
                timerOneTickPerMinute.Interval = 60000;
                label18.Text = "инт. " + (timerOneTickPerMinute.Interval / 60000).ToString() + " мин.";
                return;
            }
            

        }

        // остановка всех таймеров при нажатии на время
        private void label14_Click(object sender, EventArgs e)
        {
            if (timerOneTickPerSecond.Enabled == true)
            {
                timerOneTickPerSecond.Enabled = false;
                return;
            }
            if (timerOneTickPerSecond.Enabled == false)
            {
                timerOneTickPerSecond.Enabled = true;
                return;
            }
        }

        private void webBrowser2_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }


        private void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        private void SetBrowserFeatureControl()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // FeatureControl settings are per-process
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;

            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode()); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
            SetBrowserFeatureControlKey("FEATURE_AJAX_CONNECTIONEVENTS", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_DOMSTORAGE ", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_GPU_RENDERING ", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_IVIEWOBJECTDRAW_DMLT9_WITH_GDI  ", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_DISABLE_LEGACY_COMPRESSION", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_LOCALMACHINE_LOCKDOWN", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_OBJECT", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_SCRIPT", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_SPELLCHECKING", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_STATUS_BAR_THROTTLING", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_TABBED_BROWSING", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_VALIDATE_NAVIGATE_URL", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_WEBOC_DOCUMENT_ZOOM", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_WEBOC_POPUPMANAGEMENT", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_WEBOC_MOVESIZECHILD", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_ADDON_MANAGEMENT", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_WEBSOCKET", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_WINDOW_RESTRICTIONS ", fileName, 0);
            SetBrowserFeatureControlKey("FEATURE_XMLHTTP", fileName, 1);
        }

        private UInt32 GetBrowserEmulationMode()
        {
            int browserVersion = 7;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }

            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode. Default value for Internet Explorer 11.
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. Default value for applications hosting the WebBrowser Control.
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. Default value for Internet Explorer 8
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode. Default value for Internet Explorer 9.
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10. Webpages containing standards-based !DOCTYPE directives are displayed in IE10 mode. Default value for Internet Explorer 10.
                    break;
                default:
                    // use IE11 mode by default
                    break;
            }

            return mode;
        }

      

      
        
    }
}
