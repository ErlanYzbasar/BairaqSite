// using Aspose.Pdf;
// using Aspose.Pdf.Devices;
// using SkiaSharp;

// namespace COMMON;

// public class PdfHelper
// {
//     public static void ConvertPdfPageToImage(string pdfFilePath, string imageOutputPath)
//     {
//         new License().SetLicense("./libs/Aspose.Total.NET.lic");
//         var pdfDocument = new Document(pdfFilePath);

//         var page = pdfDocument.Pages[1];
//         var resolution = new Resolution(150);
//         var pngDevice = new PngDevice(resolution);
//         FileHelper.EnsureDir(imageOutputPath);
//         using (var imageStream = new MemoryStream())
//         {
//             pngDevice.Process(page, imageStream);
//             imageStream.Position = 0;

//             using (var skImage = SKImage.FromEncodedData(imageStream))
//             {
//                 using (var data = skImage.Encode(SKEncodedImageFormat.Png, 100))
//                 {
//                     using (var fileStream = File.OpenWrite(imageOutputPath))
//                     {
//                         data.SaveTo(fileStream);
//                     }
//                 }
//             }
//         }
//     }
// }