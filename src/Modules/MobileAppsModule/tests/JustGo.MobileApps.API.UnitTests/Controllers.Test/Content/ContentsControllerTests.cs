using System.Text;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.API.Controllers.Global;
using MobileApps.Application.Features.Content.Query.GetClubImage;
using MobileApps.Application.Features.Content.Query.GetEventImage;
using MobileApps.Application.Features.Content.Query.GetUserImage;
using MobileApps.Domain.Entities.Content;
using Moq;
using Xunit;

namespace JustGo.MobileApps.API.Test.Controllers.Test.Content
{
    public class ContentsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ISystemSettingsService> _settingsMock;
        private readonly Mock<ICryptoService> _cryptoMock;
        private readonly ContentsController _controller;

        public ContentsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _settingsMock = new Mock<ISystemSettingsService>();
            _cryptoMock = new Mock<ICryptoService>();
            _controller = new ContentsController(_mediatorMock.Object, _settingsMock.Object, _cryptoMock.Object);
        }

        //[Fact]
        //public async Task GetUserImageAsync_ReturnsOk()
        //{
        //    var query = new GetUserImageQuery { UserId = 1, ImagePath = "", Gender = "Male" };
        //    var fileResult = new FileContentResult(Encoding.UTF8.GetBytes("test"), "image/png");
        //    _mediatorMock.Setup(m => m.Send(query, It.IsAny<CancellationToken>())).ReturnsAsync(fileResult);

        //    var result = await _controller.GetUserImageAsync(query, CancellationToken.None);

        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    Assert.NotNull(okResult.Value);
        //}

        [Fact]
        public async Task GetUserImageEncryptAsync_ReturnsOk_WhenPayloadIsEmpty()
        {
            var result = await _controller.GetUserImageEncryptAsync("", CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("valid payload is required!", okResult.Value);
        }

        [Fact]
        public async Task GetUserImageEncryptAsync_ReturnsFile_WhenPayloadIsValid()
        {
            var payload = new ImageQueryParam { UserId = 1, ImagePath = "img.png", Gender = "Male" };
            var encrypted = "encrypted-string";
            var decrypted = new { UserId = 1, ImagePath = "img.png", Gender = "Male" };
            var fileResult = new FileContentResult(Encoding.UTF8.GetBytes("test"), "image/png");

            _cryptoMock.Setup(c => c.DecryptObject<dynamic>(encrypted)).Returns(decrypted);
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(fileResult);

            // Setup HttpContext for Response.Headers
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.GetUserImageEncryptAsync(encrypted, CancellationToken.None);

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", file.ContentType);
            Assert.Equal("public, max-age=31536000", httpContext.Response.Headers["Cache-Control"]);
        }

        [Fact]
        public async Task GetClubImageAsync_ReturnsOk_WhenPayloadIsEmpty()
        {
            var result = await _controller.GetClubImageAsync("", CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("valid payload is required!", okResult.Value);
        }

        [Fact]
        public async Task GetClubImageAsync_ReturnsFile_WhenPayloadIsValid()
        {
            var encrypted = "encrypted-string";
            var decrypted = new { DocId = 1, ImagePath = "", Location = "test" };
            var fileResult = new FileContentResult(Encoding.UTF8.GetBytes("test"), "image/png");

            _cryptoMock.Setup(c => c.DecryptObject<dynamic>(encrypted)).Returns(decrypted);
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetClubImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(fileResult);

            // Setup HttpContext for Response.Headers
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.GetClubImageAsync(encrypted, CancellationToken.None);

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", file.ContentType);
            Assert.Equal("public, max-age=31536000", httpContext.Response.Headers["Cache-Control"]);
        }

        [Fact]
        public async Task GetEventImageAsync_ReturnsOk_WhenPayloadIsEmpty()
        {
            var result = await _controller.GetEventImageAsync("", CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("valid payload is required!", okResult.Value);
        }

        [Fact]
        public async Task GetEventImageAsync_ReturnsFile_WhenPayloadIsValid()
        {
            var encrypted = "encrypted-string";
            var decrypted = new { DocId = 1, ImagePath = "", Location = "loc" };
            var fileResult = new FileContentResult(Encoding.UTF8.GetBytes("test"), "image/png");

            _cryptoMock.Setup(c => c.DecryptObject<dynamic>(encrypted)).Returns(decrypted);
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetEventImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(fileResult);

            // Setup HttpContext for Response.Headers
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.GetEventImageAsync(encrypted, CancellationToken.None);

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", file.ContentType);
            Assert.Equal("public, max-age=31536000", httpContext.Response.Headers["Cache-Control"]);
        }
    }
}