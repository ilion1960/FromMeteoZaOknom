using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;



namespace FromMeteoZaOknom2
{
    class weatherPicture
    {
                
        public static Image WeatherIconToPicture(string icon)
        {
            
            Image weather_pict = Image.FromFile(@"D:\50d.png");
            switch (icon)
            {
                case "01d":
                    weather_pict = Bitmap.FromFile(@"D:\01d.png");
                    break;
                case "02d":
                    weather_pict = Bitmap.FromFile(@"D:\02d.png");
                    break;
                case "03d":
                    weather_pict = Bitmap.FromFile(@"D:\03d.png");
                    break;
                case "04d":
                    weather_pict = Bitmap.FromFile(@"D:\04d.png");
                    break;
                case "09d":
                    weather_pict = Bitmap.FromFile(@"D:\09d.png");
                    break;
                case "10d":
                    weather_pict = Bitmap.FromFile(@"D:\10d.png");
                    break;
                case "11d":
                    weather_pict = Bitmap.FromFile(@"D:\11d.png");
                    break;
                case "13d":
                    weather_pict = Bitmap.FromFile(@"D:\13d.png");
                    break;
                case "50d":
                    weather_pict = Bitmap.FromFile(@"D:\50d.png");
                    break;
                case "01n":
                    weather_pict = Bitmap.FromFile(@"D:\01n.png");
                    break;
                case "02n":
                    weather_pict = Bitmap.FromFile(@"D:\02n.png");
                    break;
                case "03n":
                    weather_pict = Bitmap.FromFile(@"D:\03n.png");
                    break;
                case "04n":
                    weather_pict = Bitmap.FromFile(@"D:\04n.png");
                    break;
                case "09n":
                    weather_pict = Bitmap.FromFile(@"D:\09n.png");
                    break;
                case "10n":
                    weather_pict = Bitmap.FromFile(@"D:\10n.png");
                    break;
                case "11n":
                    weather_pict = Bitmap.FromFile(@"D:\11n.png");
                    break;
                case "13n":
                    weather_pict = Bitmap.FromFile(@"D:\13n.png");
                    break;
                case "50n":
                    weather_pict = Bitmap.FromFile(@"D:\50n.png");
                    break;
            
            }

            return weather_pict;

        }



    }
}
