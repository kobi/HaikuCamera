using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Xml.Linq;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using JpgImage = System.Drawing.Image;
using RekognitionImage = Amazon.Rekognition.Model.Image;

namespace HaikuCamera.Models
{
    public class Haiku
    {
        public async Task<HaikuData> HandleImage(JpgImage image, string uploadedImageFileName, string dataFolder)
        {
            //todo: handle image over 5 MB
            var baseFileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}_{Guid.NewGuid():D}";
            var imageFileName = baseFileName + ".jpg";
            var mp3FileName = baseFileName + ".mp3";
            image.Save(Path.Combine(dataFolder, imageFileName), ImageFormat.Jpeg);
            var keywords = await DescribeImage(image);
            var foundHaiku = keywords.Any();
            if (!foundHaiku)
            {
                keywords = new List<string> {"fail", "sad", "error"};
            }
            var haikuText = FindHaiku(keywords);
            var formatForPolly = FormatForPolly(haikuText);
            await CreateMp3(formatForPolly, Path.Combine(dataFolder, mp3FileName));
            var data = new HaikuData
            {
                HaikuText = haikuText,
                HaikuFormattedForPolly = formatForPolly,
                ImageFileName = imageFileName,
                Keywords = keywords,
                Mp3FileName = mp3FileName,
                UploadedFileName = uploadedImageFileName,
            };
            File.WriteAllText(Path.Combine(dataFolder, baseFileName + ".json"), Json.Encode(data));
            return data;
        }

        public async Task<List<string>> DescribeImage(JpgImage image)
        {
            //todo: max image size - 5MB.
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Jpeg);
                stream.Seek(0, SeekOrigin.Begin);

                var content = await new AmazonRekognitionClient().DetectLabelsAsync(new DetectLabelsRequest
                {
                    MaxLabels = 5,
                    Image = new RekognitionImage
                    {
                        Bytes = stream,
                    },
                });
                return content.Labels.Select(l => l.Name).ToList();
            }
        }

        public async Task CreateMp3(string text, string targetMp3FilePath)
        {
            //text = text.Replace("\n", ",\n") + "."; //add commas at end of lines, for a dramatic stop.
            var polly = new AmazonPollyClient();
            var speak = await polly.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
            {
                OutputFormat = OutputFormat.Mp3,
                Text = text,
                VoiceId = GetEnglishVoice().Id,
            });
            using (var fileStream = File.Create(targetMp3FilePath))
            {
                speak.AudioStream.CopyTo(fileStream);
            }
        }

        private static readonly List<Voice> Voices = new List<Voice>();

        public IEnumerable<Voice> GetAllVoices()
        {
            lock (Voices)
            {
                if (Voices.Any()) return Voices;

                var polly = new AmazonPollyClient();
                string pagingToken = null;
                do
                {
                    var page = polly.DescribeVoices(new DescribeVoicesRequest { NextToken = pagingToken });
                    pagingToken = page.NextToken;
                    Voices.AddRange(page.Voices);
                } while (pagingToken != null);

                return Voices;
            }
        }

        public Voice GetEnglishVoice() =>
             GetAllVoices().Where(v => v.LanguageCode.Value.StartsWith("en")).ToList().RandomOne();

        public string FindHaiku(IList<string> subject)
        {
            //https://www.reddit.com/r/haiku/search.rss?q=title%3A(apple+OR+banana)&restrict_sr=on&sort=relevance&t=all
            var keywords = String.Join(" OR ", subject.Select(s => $"({s})"));
            var url = $"https://www.reddit.com/r/haiku/search.rss?q=title%3A({Uri.EscapeUriString(keywords)})&restrict_sr=on&sort=relevance&t=all";
            var all = XDocument.Load(url).Descendants().Where(e => e.Name.LocalName == "title")
                .Select(e => e.Value).Where(h => h?.Count('/'.Equals) == 2).ToList();
            if (!all.Any())
            {
                var failureHaiku =
                    FindHaiku(new[] {"fail", "sad", "error", "failure", "sadness", "mystery", "unknown", "lost"});
                return $"We could not find a haiku about {String.Join(", ", subject)}. " +
                       $"Please enjoy this failure related haiku:\n{failureHaiku}";
            }
            return FormatForDisplay(all.RandomOne());
        }

        public static string FormatForDisplay(string haiku)
        {
            if (haiku == null) return null;
            var lines =  haiku.Split('/').Select(s => s.Trim());
            return String.Join("\n", lines);
        }

        public static string FormatForPolly(string haiku)
        {
            if (haiku == null) return null;
            haiku = FormatForDisplay(haiku);
            return Regex.Replace(haiku, @"\b$", m => m.Index < haiku.Length - 3 ? "," : ".", RegexOptions.Multiline);
        }
    }
}