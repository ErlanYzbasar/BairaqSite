using System.Net.Http.Headers;
using MODEL.FormatModels;
using Serilog;

namespace COMMON;

public static class LanguagePackHelper
{
    private const string BaseAddress = "https://www.sozdikqor.org";

    public static string GetLanguagePackJsonString()
    {
        var result = string.Empty;
        var localFilePath = Path.Combine(AppContext.BaseDirectory, "language_pack.txt");

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri(BaseAddress);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/packs");
            var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var temp = response.Content.ReadAsStringAsync().Result;

            if (IsValidResult(temp, out var ajaxResult) && Directory.Exists(AppContext.BaseDirectory))
            {
                result = ajaxResult.Data.ToString();
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                }

                File.WriteAllText(localFilePath, result);
            }
            else
            {
                throw new Exception("Not valid result from api");
            }
        }
        catch (Exception ex)
        {
            if (File.Exists(localFilePath))
            {
                result = File.ReadAllText(localFilePath);
            }
            else
            {
                Log.Error(ex, "COMMON:GetLanguagePackJsonString");
            }
        }

        return result;
    }

    private static bool IsValidResult(string result, out AjaxMsgModel ajaxResult)
    {
        ajaxResult = JsonHelper.DeserializeObject<AjaxMsgModel>(result);
        return ajaxResult is { Status: "success" };
    }
}