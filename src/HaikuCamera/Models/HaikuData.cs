using System.Collections.Generic;

namespace HaikuCamera.Models
{
    public class HaikuData
    {
        public string ImageFileName { get; set; }
        public string Mp3FileName { get; set; }
        public string ImageUrl { get; set; }
        public string Mp3Url { get; set; }
        public string HaikuText { get; set; }
        public IEnumerable<string> Keywords { get; set; }
        public string UploadedFileName { get; set; }
        public string HaikuFormattedForPolly { get; set; }
    }
}