using System.Diagnostics;

namespace COMMON;

public static class FileHelper
{
    #region Ensure Directory Exists +EnsureDir(string path, bool isDir = false)

    public static void EnsureDir(string path, bool isDir = false)
    {
        var dir = isDir ? path : Path.GetDirectoryName(path);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    #endregion

    #region Delete File +Delete(string path)

    public static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Download File By Url +Download(string filPath, string url)

    public static async Task Download(string filPath, string url)
    {
        using var client = new HttpClient();
        using var stream = await client.GetStreamAsync(new Uri(url));
        using var fs = new FileStream(filPath, FileMode.CreateNew);
        await stream.CopyToAsync(fs);
    }

    #endregion

    #region Download File By Url +DownloadFile(string savePath, string fileUrl)

    public static async Task DownloadFile(string savePath, string fileUrl)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(fileUrl);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();

        File.WriteAllBytes(savePath, bytes);
    }

    #endregion

    #region Append A Line To File +AppendLine(string filePath, string content)

    public static void AppendLine(string filePath, string content)
    {
        File.AppendAllText(filePath, content + Environment.NewLine);
    }

    #endregion

    #region Get All File Path In A Directory +GetAllFilePath(string pDirectoryPath)

    public static List<string> GetAllFilePath(string pDirectoryPath)
    {
        var res = new List<string>();

        foreach (var filePath in Directory.GetFiles(pDirectoryPath))
        {
            res.Add(filePath);
        }

        foreach (var directoryPath in Directory.GetDirectories(pDirectoryPath))
        {
            res.AddRange(GetAllFilePath(directoryPath));
        }

        return res;
    }

    #endregion

    #region Convert Mp4 File's Audio To Mp3 File +ConvertMp4ToMp3(string sourcePath, string destinationPath, out string output, out string error)

    public static void ConvertMp4ToMp3(string sourcePath, string destinationPath, out string output, out string error)
    {
        var ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.FileName = "ffmpeg";
        ffmpegProcess.StartInfo.Arguments = $"-i \"{sourcePath}\" -vn -acodec libmp3lame -q:a 2 \"{destinationPath}\"";
        ffmpegProcess.StartInfo.RedirectStandardOutput = true;
        ffmpegProcess.StartInfo.RedirectStandardError = true;
        ffmpegProcess.StartInfo.UseShellExecute = false;
        ffmpegProcess.StartInfo.CreateNoWindow = true;
        ffmpegProcess.Start();

        output = ffmpegProcess.StandardOutput.ReadToEnd();
        error = ffmpegProcess.StandardError.ReadToEnd();
        ffmpegProcess.WaitForExit();
        if (!ffmpegProcess.HasExited)
        {
            ffmpegProcess.Kill();
        }
    }

    #endregion
}