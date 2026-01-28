using eLogo.PdfService.Services.Interfaces;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace eLogo.PdfService.Services
{
    public class ImageResizer: IImageResizer
    {
        public string CleanImages(string htmlWithBase64Image)
        {
            Regex regex = new Regex(@"src=""data:image\/[^;]+;base64,([^""]+)""");
            var matches = regex.Matches(htmlWithBase64Image.Trim());
            if (matches.Count == 0)
                return htmlWithBase64Image;

            var sb = new System.Text.StringBuilder(htmlWithBase64Image);
            foreach (Match match in matches)
            {
                string base64ImageData = match.Groups[1].Value;
                byte[]? imageBytes = null;
                try
                {
                    imageBytes = Convert.FromBase64String(base64ImageData.Trim());
                }
                catch
                {
                    continue;
                }
                if (imageBytes.Length < 1024 * 80)
                {
                    imageBytes = null;
                    continue;
                }
                using var resizedImage = ResizeImage(imageBytes, 75);
                using var ms = new MemoryStream();
                resizedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                string newBase64ImageData = Convert.ToBase64String(ms.ToArray());
                sb.Replace(base64ImageData, newBase64ImageData);
                imageBytes = null;
            }
            return sb.ToString();
        }

        private Image ResizeImage(byte[] imageBytes, double percentage)
        {
            using var ms = new MemoryStream(imageBytes);
            using var originalImage = Image.FromStream(ms);
            int newWidth = (int)(originalImage.Width * (percentage / 100.0));
            int newHeight = (int)(originalImage.Height * (percentage / 100.0));
            var resizedImage = new Bitmap(originalImage, new Size(newWidth, newHeight));
            return resizedImage;
        }
    }
}
