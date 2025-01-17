using SkiaSharp;

namespace COMMON;

public static class ImgHandler
{
    #region Суретті қию  +CutImage(string webRoot, string fileName, MemoryStream avatar_file, int pointX = 0, int pointY = 0, int width = 0, int height = 0, int rotate = 0, int smallWidth = 100, int smallHeight = 100)

    public static string CutImage(string webRoot, string fileName, MemoryStream avatarFile, int pointX = 0,
        int pointY = 0, int width = 0, int height = 0, int rotate = 0, int smallWidth = 100, int smallHeight = 100)
    {
        SKBitmap croppedBitmap = null; //Қимақшы болған үлкендікте Bitmap құру
        SKBitmap thumbImg = null; //Қимақшы болған сурет 
        // SKGraphics gps = null;    //Graphics   
        SKBitmap finalImgBig = null; //Үлкен сурет 
        SKBitmap finalImgMiddle = null; //Орта сурет 
        SKBitmap finalImgSmall = null; //Кішкене суре 
        try
        {
            var finalWidthBig = smallWidth * 6;
            var finalHeightBig = smallHeight * 6;
            var finalWidthMiddle = smallWidth * 2;
            var finalHeightMiddle = smallHeight * 2;
            var finalWidthSmall = smallWidth;
            var finalHeightSmall = smallHeight;
            croppedBitmap = new SKBitmap(width, height);
            thumbImg = SKBitmap.Decode(avatarFile.ToArray());
            thumbImg = Rotate(thumbImg, rotate);

            if (!Directory.Exists(webRoot))
            {
                Directory.CreateDirectory(webRoot);
            }

            var dest = new SKRect(0, 0, width, height);
            var source = new SKRect(pointX, pointY, pointX + width, pointY + height);
            using (var canvas = new SKCanvas(croppedBitmap))
            {
                canvas.DrawBitmap(thumbImg, source, dest);
            }

            finalImgBig = croppedBitmap.Resize(new SKImageInfo(finalWidthBig, finalHeightBig), SKFilterQuality.High);
            finalImgMiddle = croppedBitmap.Resize(new SKImageInfo(finalWidthMiddle, finalHeightMiddle),
                SKFilterQuality.High);
            finalImgSmall =
                croppedBitmap.Resize(new SKImageInfo(finalWidthSmall, finalHeightSmall), SKFilterQuality.High);


            var fArr = fileName.Split('.');
            var finalPathBig = webRoot + fArr[0] + "_big.webp";
            var finalPathMiddle = webRoot + fArr[0] + "_middle.webp";
            var finalPathSmall = webRoot + fArr[0] + "_small.webp";


            var saveFormat = SKEncodedImageFormat.Webp; //GetSaveFormat(fileName);
            using (var image = SKImage.FromBitmap(finalImgBig))
            using (var data = image.Encode(saveFormat, 100))
            using (var stream = File.OpenWrite(finalPathBig))
            {
                // save the data to a stream
                data.SaveTo(stream);
            }

            using (var image = SKImage.FromBitmap(finalImgMiddle))
            using (var data = image.Encode(saveFormat, 100))
            using (var stream = File.OpenWrite(finalPathMiddle))
            {
                // save the data to a stream
                data.SaveTo(stream);
            }

            using (var image = SKImage.FromBitmap(finalImgSmall))
            using (var data = image.Encode(saveFormat, 100))
            using (var stream = File.OpenWrite(finalPathSmall))
            {
                // save the data to a stream
                data.SaveTo(stream);
            }

            return finalPathBig;
        }
        catch (Exception)
        {
            return "";
        }
        finally
        {
            if (croppedBitmap != null)
            {
                croppedBitmap.Dispose();
            }

            if (thumbImg != null)
            {
                thumbImg.Dispose();
            }

            if (finalImgBig != null)
            {
                finalImgBig.Dispose();
            }

            if (finalImgMiddle != null)
            {
                finalImgMiddle.Dispose();
            }

            if (finalImgSmall != null)
            {
                finalImgSmall.Dispose();
            }
        }
    }

    #endregion

    #region Файлды сақтау кеңейтімін алу  GetSaveFormat(string filePath)

    private static SKEncodedImageFormat GetSaveFormat(string filePath)
    {
        var fileFormat = Path.GetExtension(filePath).ToLower();
        var saveFormat = SKEncodedImageFormat.Png;
        switch (fileFormat)
        {
            case ".png":
            {
                saveFormat = SKEncodedImageFormat.Png;
            }
                break;
            case ".jpg":
            case ".jpeg":
            {
                saveFormat = SKEncodedImageFormat.Jpeg;
            }
                break;
            case ".gif":
            {
                saveFormat = SKEncodedImageFormat.Gif;
            }
                break;
            case ".webp":
            {
                saveFormat = SKEncodedImageFormat.Webp;
            }
                break;
            case ".avif":
            {
                saveFormat = SKEncodedImageFormat.Avif;
            }
                break;
        }

        return saveFormat;
    }

    #endregion

    #region Суретті қысу +CompressImage(MemoryStream imageStream, string fullPathName, int compressLevel)

    public static void CompressImage(MemoryStream imageStream, string fullPathName, int compressLevel)
    {
        var saveFormat = GetSaveFormat(fullPathName);
        var sKBitmap = SKBitmap.Decode(imageStream.ToArray());
        using var image = SKImage.FromBitmap(sKBitmap);
        using var data = image.Encode(saveFormat, compressLevel);
        using var stream = File.OpenWrite(fullPathName);
        // save the data to a stream
        data.SaveTo(stream);
    }

    #endregion

    #region Суретті бұру -Rotate(SKBitmap bitmap, double angle)

    private static SKBitmap Rotate(SKBitmap bitmap, double angle)
    {
        var radians = Math.PI * angle / 180;
        var sine = (float)Math.Abs(Math.Sin(radians));
        var cosine = (float)Math.Abs(Math.Cos(radians));
        var originalWidth = bitmap.Width;
        var originalHeight = bitmap.Height;
        var rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
        var rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

        var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

        using var surface = new SKCanvas(rotatedBitmap);
        surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
        surface.RotateDegrees((float)angle);
        surface.Translate(-originalWidth / 2, -originalHeight / 2);
        surface.DrawBitmap(bitmap, new SKPoint());

        return rotatedBitmap;
    }

    #endregion

    #region Convert Image Colors To White +ConvertImageColorsToWhite(MemoryStream image_file, string outputImagePath)

    public static void ConvertImageColorsToWhite(MemoryStream imageFile, string outputImagePath)
    {
        // Decode the original image
        using var originalBitmap = SKBitmap.Decode(imageFile.ToArray());

        // Create a new bitmap to store the processed image
        using var resultBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);

        // Define the color white with full opacity
        var whiteOpaque = new SKColor(255, 255, 255, 255);
        // Define the color white with zero opacity (transparent)
        var whiteTransparent = new SKColor(255, 255, 255, 0);

        // Loop through all the pixels of the original image
        for (var y = 0; y < originalBitmap.Height; y++)
        {
            for (var x = 0; x < originalBitmap.Width; x++)
            {
                // Get the current pixel
                var pixel = originalBitmap.GetPixel(x, y);

                // If the pixel is white and opaque, make it transparent
                if (pixel == whiteOpaque)
                {
                    resultBitmap.SetPixel(x, y, whiteTransparent);
                }
                else
                {
                    // If the pixel is not white, change its color to white but keep its original alpha
                    resultBitmap.SetPixel(x, y, new SKColor(255, 255, 255, pixel.Alpha));
                }
            }
        }

        // Encode the new image as PNG and save it
        using var image = SKImage.FromBitmap(resultBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputImagePath);
        data.SaveTo(stream);
    }

    #endregion

    #region Get Crop Parameters +GetCropParameters(string imagePath)

    public static (int x, int y, int width, int height) GetCropParameters(string imagePath)
    {
        using var bitmap = SKBitmap.Decode(imagePath);
        var targetAspectRatio = 16f / 9f;
        var srcWidth = bitmap.Width;
        var srcHeight = bitmap.Height;
        var srcAspectRatio = (float)srcWidth / srcHeight;

        int cropWidth, cropHeight;

        if (srcAspectRatio > targetAspectRatio)
        {
            cropHeight = srcHeight;
            cropWidth = (int)(srcHeight * targetAspectRatio);
        }
        else
        {
            cropWidth = srcWidth;
            cropHeight = (int)(srcWidth / targetAspectRatio);
        }

        var x = (srcWidth - cropWidth) / 2;
        var y = (srcHeight - cropHeight) / 2;
        return (x, y, cropWidth, cropHeight);
    }

    #endregion

    #region Delete Image +DeleteImage(string path)

    public static void DeleteImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var size = string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(path);

        if (fileName.EndsWith("_big")) size = "_big.";
        else if (fileName.EndsWith("_middle")) size = "_middle.";
        else if (fileName.EndsWith("_small")) size = "_small.";

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (string.IsNullOrEmpty(size)) return;

        if (File.Exists(path.Replace(size, "_big.")))
        {
            File.Delete(path.Replace(size, "_big."));
        }

        if (File.Exists(path.Replace(size, "_middle.")))
        {
            File.Delete(path.Replace(size, "_middle."));
        }

        if (File.Exists(path.Replace(size, "_small.")))
        {
            File.Delete(path.Replace(size, "_small."));
        }
    }

    #endregion

    #region Generate Ratio Image +GenerateRatioImage(float width, float height,string picturePath, string savePath)

    public static byte[] GenerateRatioImage(float width, float height, string picturePath, string savePath)
    {
        var imageWidth = (int)width;
        var imageHeight = (int)height;
        var info = new SKImageInfo(imageWidth, imageHeight);

        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(0xEE, 0xEE, 0xEE));

        using var providedImage = SKBitmap.Decode(picturePath);
        var targetWidth = imageWidth * 0.6f;
        var targetHeight = imageHeight * 0.6f;

        var widthRatio = targetWidth / providedImage.Width;
        var heightRatio = targetHeight / providedImage.Height;
        var minRatio = Math.Min(widthRatio, heightRatio);

        int newWidth, newHeight;
        if (providedImage.Width > targetWidth || providedImage.Height > targetHeight)
        {
            newWidth = (int)(providedImage.Width * minRatio);
            newHeight = (int)(providedImage.Height * minRatio);
        }
        else
        {
            newWidth = providedImage.Width;
            newHeight = providedImage.Height;
        }

        using var resizedImage = providedImage.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        var x = (imageWidth - newWidth) / 2;
        var y = (imageHeight - newHeight) / 2;
        canvas.DrawBitmap(resizedImage, x, y);

        canvas.Flush();


        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using (var fileStream = File.OpenWrite(savePath))
        {
            data.SaveTo(fileStream);
        }

        using var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream.ToArray();
    }

    #endregion


     #region ConvertImageColorsToUlyWhite
    public static void ConvertImageColorsToUlyWhite(MemoryStream image_file, string outputImagePath)
    {
        using var originalBitmap = SKBitmap.Decode(image_file.ToArray());
        using var resultBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);
        for (int y = 0; y < originalBitmap.Height; y++)
        {
            for (int x = 0; x < originalBitmap.Width; x++)
            {
                SKColor pixel = originalBitmap.GetPixel(x, y);
                resultBitmap.SetPixel(x, y, pixel);
            }
        }
        using var image = SKImage.FromBitmap(resultBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputImagePath);
        data.SaveTo(stream);
    }

    #endregion
}