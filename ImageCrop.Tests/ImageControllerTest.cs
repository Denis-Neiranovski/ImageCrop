using ImageCrop.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImageCrop.Tests
{
    public class ImageControllerTest
    {
        public readonly string wwwrootTestPath = Directory.GetCurrentDirectory() + "\\wwwrootTest\\";

        [Test]
        public void GetAllFileNames_WhenExistingFolder_ReturnsFolderFiles()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var fileNames = imageController.GetAllFileNames();

            //assert
            Assert.That(fileNames.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAllFileNames_WhenInvalidFolder_ReturnsNoFiles()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
            
            envMock
                .Setup(e => e.WebRootPath)
                .Returns($"{wwwrootTestPath}invalid\\");

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var fileNames = imageController.GetAllFileNames();

            //assert
            Assert.That(fileNames.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task UploadAsync_WhenValidImage_ReturnsOk()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
            var fileFormMock = new Mock<IFormFile>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            fileFormMock
                .Setup(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, ct) => Task.CompletedTask);
            fileFormMock
                .Setup(ff => ff.FileName)
                .Returns("fileName.jpeg");

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = await imageController.UploadAsync(fileFormMock.Object) as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            fileFormMock.Verify(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once());

            //delete created image
            File.Delete(Path.Combine(wwwrootTestPath, "Images", fileFormMock.Object.FileName));
        }

        [Test]
        public async Task UploadAsync_WhenInvalidImage_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
            var fileFormMock = new Mock<IFormFile>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            fileFormMock
                .Setup(ff => ff.FileName)
                .Returns("");

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = await imageController.UploadAsync(fileFormMock.Object) as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            fileFormMock.Verify(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public void CropImage_WhenValidImageName_ReturnsOk()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
            
            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.CropImage(0, 0, 0.5, 0.5, "example.jpg") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public void CropImage_WhenInvalidImageName_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
            
            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.CropImage(0, 0, 0.5, 0.5, "") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public void CropImage_WhenInvalidImageExtension_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.CropImage(0, 0, 0.5, 0.5, "file.ccc") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public void CropImage_WhenImageDoesNotExist_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.CropImage(0, 0, 0.5, 0.5, "file.jpg") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public void DownloadCroppedImage_WhenImageExists_ReturnsImage()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.DownloadCroppedImage("example.jpg") as FileContentResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.ContentType, Is.EqualTo("image/jpeg"));
            Assert.That(result.FileContents, Is.Not.Empty);
        }

        [Test]
        public void DownloadCroppedImage_WhenImageDoesNotExist_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.DownloadCroppedImage("ccc.jpg") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public void DownloadCroppedImage_WhenInvalidExtension_ReturnsBadRequest()
        {
            //arrange
            var envMock = new Mock<IWebHostEnvironment>(MockBehavior.Loose);

            envMock
                .Setup(e => e.WebRootPath)
                .Returns(wwwrootTestPath);

            var environment = envMock.Object;
            var imageController = new ImageController(environment);

            //act
            var result = imageController.DownloadCroppedImage("example.ccc") as ObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }
    }
}
