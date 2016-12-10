using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using HaikuCamera.Models;

namespace HaikuCamera.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        private string BaseFolder => Server.MapPath("~/App_Data");

        [HttpPost]
        public async Task<JsonResult> Upload()
        {
            var uploadedFile = Request.Files.Get(0);
            if (uploadedFile == null)
            {
                throw new InvalidOperationException(
                    "There should be a file,\nbut there was null instead.Please,\ntry POSTing again.");
            }
            Image image = new Bitmap(uploadedFile.InputStream).ScaleImage(1200);
            var data = await new Haiku().HandleImage(image, uploadedFile.FileName, BaseFolder);
            data.ImageUrl = Url.Action("Archive", new {id = data.ImageFileName});
            data.Mp3Url = Url.Action("Archive", new {id = data.Mp3FileName});
            return Json(data);
        }

        [HttpPost]
        public async Task<JsonResult> Stub()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            var data = new HaikuData
            {
                ImageUrl = Url.Action("Archive", new { id = "stub.jpg" }),
                Mp3Url = Url.Action("Archive", new { id = "stub.mp3" }),
                HaikuText = "there once was a gecko\nwho ate a small potato\nand danced the flamenco.",
                Keywords = new[] {"stub", "example", DateTime.UtcNow.ToString("s")},
            };
            return Json(data);
        }

        public ActionResult Archive(string id)
        {
            if (!Regex.IsMatch(id, @"^[\w\-]+\.\w+$"))
            {
                Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return View("Haiku", 
                    model: "The parameter\nshould be a simple file name.\nPlease do not hack us.");
            }
            var path = Path.Combine(BaseFolder, id);
            var contentType = id.EndsWith(".jpg") ? "image/jpeg" : id.EndsWith(".mp3") ? "audio/mpeg" : "application/octet-stream";
            return File(path, contentType);
        }

        public ActionResult Haiku(string id)
        {
            string haiku = new Haiku().FindHaiku(new[] {id});
            return View(model: haiku);
        }
    }
}