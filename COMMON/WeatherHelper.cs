using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace COMMON;

public class WeatherHelper
{
    public class WeatherInfo
    {
        public string CityName { get; set; }
        public double Temperature { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
    }

    private static readonly string ApiKey = "7f714ddf252a38653c839e980a020779";
    private static readonly HttpClient Client = new HttpClient();

    public static async Task<WeatherInfo> GetWeatherByCityNameAsync(string cityName)
    {
        var requestUrl = $"http://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={ApiKey}&units=metric";

        var response = await Client.GetStringAsync(requestUrl);
        var weatherData = JObject.Parse(response);
        WeatherInfo weatherInfo = null;

        try
        {
            weatherInfo   = new WeatherInfo
            {
                CityName = cityName,
                Temperature = (double)weatherData["main"]?["temp"],
                Description = (string)weatherData["weather"]?[0]?["description"],
                IconUrl = GetWeatherIconUrl((string)weatherData["weather"]?[0]?["icon"])
                
            };
        }
        catch 
        {
            return null;
        }

        return weatherInfo;
    }

    public static string GetWeatherIconUrl(string iconCode)
    {
        if (string.IsNullOrEmpty(iconCode))
        {
            return string.Empty;
        }

        return $"http://openweathermap.org/img/wn/{iconCode}@2x.png";
    }


    public static async Task<WeatherInfo> GetWeatherByIpAsync(string ipAddress)
    {
        var cityName = await GetCityByIpAsync(ipAddress);
        if (!string.IsNullOrEmpty(cityName))
        {
            return await GetWeatherByCityNameAsync(cityName);
        }
        else
        {
            cityName = "Almaty";
            return await GetWeatherByCityNameAsync(cityName);
        }
    }

    public static async Task<string> GetCityByIpAsync(string ipAddress)
    {
        var ipInfoUrl = $"http://ipinfo.io/{ipAddress}/json";
        var response = await Client.GetStringAsync(ipInfoUrl);
        var ipData = JObject.Parse(response);
        return ipData["city"]?.ToString() ?? "Almaty";
    }
}