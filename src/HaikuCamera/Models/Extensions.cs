using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HaikuCamera.Models
{
    public static class Extensions
    {
        private static readonly Random Random=new Random();

        public static T RandomOne<T>(this IList<T> items) => items[Random.Next(items.Count)];

        public static Image ScaleImage(this Image image, int maxSize) => ScaleImage(image, maxSize, maxSize);


        //http://stackoverflow.com/a/6501997/7586 , http://stackoverflow.com/a/87786/7586
        public static Image ScaleImage(this Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(1, Math.Min(ratioX, ratioY)); //Min(1,) - do not upscale the image;
            
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }
    }
}