using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FromMeteoZaOknom2
{
    class WindDirection
    {
        public static string getWindDirection(int windDegree)
        {
            string wind_meteo = "";
            if ((windDegree >= 338) || (windDegree <=  22)) wind_meteo = "ветер северный";
            else
            if ((windDegree >=  23) && (windDegree <=  67)) wind_meteo = "ветер северо-восточный";
            else
            if ((windDegree >=  68) && (windDegree <= 112)) wind_meteo = "ветер восточный";
            else
            if ((windDegree >= 113) && (windDegree <= 157)) wind_meteo = "ветер юго-восточный";
            else
            if ((windDegree >= 158) && (windDegree <= 202)) wind_meteo = "ветер южный";
            else
            if ((windDegree >= 203) && (windDegree <= 247)) wind_meteo = "ветер юго-западный";
            else
            if ((windDegree >= 248) && (windDegree <= 292)) wind_meteo = "ветер западный";
            else
            if ((windDegree >= 293) && (windDegree <= 337)) wind_meteo = "ветер северо-западный";
            return wind_meteo;
        }

    }
}
