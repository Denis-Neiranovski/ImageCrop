using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace ImageCrop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IWebHostEnvironment environment;

        public ImageController(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        [HttpGet]
        [Route("")]
        public IEnumerable<string> GetAllFileNames()
        {
            if (!Directory.Exists(environment.WebRootPath + "\\Images"))
            {
                return Enumerable.Empty<string>();
            }

            var names = new DirectoryInfo(environment.WebRootPath + "\\Images").GetFiles().Select(x => x.Name);
            return names;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadAsync([FromForm] IFormFile file)
        {
            if (file.FileName == null || file.FileName.Length == 0)
            { 
                return BadRequest("No image");
            }

            if (!Directory.Exists(environment.WebRootPath + "\\Images"))
            {
                Directory.CreateDirectory(environment.WebRootPath + "\\Images\\");
            }

            var path = Path.Combine(environment.WebRootPath, "Images/", file.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(JsonSerializer.Serialize(file.FileName));
        }

        [HttpGet]
        [Route("crop")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public IActionResult CropImage(
            double relativeTop,
            double relativeLeft,
            double relativeWidth,
            double relativeHeight,
            string imageName)
        {
            if (imageName == null || imageName.Length == 0)
            {
                return BadRequest("No image");
            }

            if (new FileExtensionContentTypeProvider().TryGetContentType(imageName, out var extension) == false)
            {
                return BadRequest("No extension provided for this image");
            }

            if (!System.IO.File.Exists(Path.Combine(environment.WebRootPath, "Images/", imageName)))
            {
                return BadRequest("This file does not exist");
            }

            using var cropFromImage = new Bitmap(Path.Combine(environment.WebRootPath, "Images/", imageName));

            int x = (int)(cropFromImage.Width * relativeLeft);
            int y = (int)(cropFromImage.Height * relativeTop);
            int width = (int)(cropFromImage.Width * relativeWidth);
            int height = (int)(cropFromImage.Height * relativeHeight);

            var cropArea = new Rectangle(x, y, width, height);

            using var croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
            using var graphics = Graphics.FromImage(croppedImage);

            graphics.DrawImage(
                cropFromImage,
                new Rectangle(0, 0, width, height),
                cropArea,
                GraphicsUnit.Pixel);

            if (!Directory.Exists(environment.WebRootPath + "\\CroppedImages"))
            {
                Directory.CreateDirectory(environment.WebRootPath + "\\CroppedImages\\");
            }

            var path = Path.Combine(environment.WebRootPath, "CroppedImages/", imageName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                croppedImage.Save(stream, ParseImageFormat(extension));
            }

            return Ok(JsonSerializer.Serialize(imageName));
        }

        [HttpGet]
        [Route("downloadCroppedImage")]
        public IActionResult DownloadCroppedImage(string imageName)
        {
            var files = new DirectoryInfo(environment.WebRootPath + "\\CroppedImages").GetFiles(imageName);

            if (files.Length == 0)
            { 
                return BadRequest("No image");
            }

            if (new FileExtensionContentTypeProvider().TryGetContentType(files[0].Extension, out var extension) == false)
            {
                return BadRequest("No extension provided for this image");
            }

            var bytes = System.IO.File.ReadAllBytes(files[0].FullName);
            return File(bytes, extension);
        }

        private ImageFormat ParseImageFormat(string ext)
        {
            return ext switch
            {
                "image/jpeg" => ImageFormat.Jpeg,
                "image/gif" => ImageFormat.Gif,
                "image/png" => ImageFormat.Png,
                "image/tiff" => ImageFormat.Tiff,
                "image/bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Jpeg,
            };
        }
    }
}
