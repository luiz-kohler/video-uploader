using API.Controllers;
using API.Interfaces;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Presentation;

namespace Tests.Unit.Tests.Controllers
{
    [Trait("Category", "Unit")]
    [Collection("VideoController")]
    public class VideoControllerTests
    {
        private readonly Faker _faker;
        private readonly VideoController _controller;
        private readonly IObjectStorageService _service;

        public VideoControllerTests()
        {
            _faker = new Faker();
            _service = Substitute.For<IObjectStorageService>();
            _controller = new VideoController(_service);
        }

        #region Upload Tests

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnFileIdSuccessfully()
        {
            // Arrange
            var fileId = _faker.Random.String();
            var file = CreateFormFile("video.mp4", "video/mp4", 1024);

            _service.Upload(Arg.Any<IFormFile>()).Returns(fileId);

            // Act
            var response = await _controller.Upload(file);

            // Assert
            await _service.Received(1).Upload(Arg.Any<IFormFile>());

            var acceptedResult = response.Should().BeOfType<AcceptedResult>().Subject;
            acceptedResult.StatusCode.Should().Be(202);
            acceptedResult.Value.Should().BeEquivalentTo(new { fileId });
        }

        [Fact]
        public async Task Upload_WithNullFile_ShouldReturnBadRequest()
        {
            // Arrange & Act
            var response = await _controller.Upload(null);

            // Assert
            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("File must be informed");
        }

        [Fact]
        public async Task Upload_WithEmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            var file = CreateFormFile("video.mp4", "video/mp4", 0);

            // Act
            var response = await _controller.Upload(file);

            // Assert
            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("File must be informed");
        }

        [Fact]
        public async Task Upload_WithNonMp4File_ShouldReturnBadRequest()
        {
            // Arrange
            var file = CreateFormFile("video.avi", "video/avi", 1024);

            // Act
            var response = await _controller.Upload(file);

            // Assert
            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("File must be .mp4");
        }

        [Fact]
        public async Task Upload_WithServiceThrowingException_ShouldThrowException()
        {
            // Arrange
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var file = CreateFormFile("video.mp4", "video/mp4", 1024);

            _service.Upload(Arg.Any<IFormFile>()).Throws(exception);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<Exception>(() => _controller.Upload(file));
            actualException.Message.Should().Be(expectedExceptionMessage);
            await _service.Received(1).Upload(Arg.Any<IFormFile>());
        }

        #endregion

        #region PreSigned Tests

        [Fact]
        public void PreSigned_WithValidRequest_ShouldReturnKeyAndUrl()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var expectedUrl = _faker.Internet.Url();
            var request = new PreSignedDto(fileName);

            _service.GeneratePreSignedUrl(Arg.Any<string>(), fileName).Returns(expectedUrl);

            // Act
            var response = _controller.PreSigned(request);

            // Assert
            _service.Received(1).GeneratePreSignedUrl(Arg.Any<string>(), fileName);

            var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull(expectedUrl);
            okResult.Value.ToString().Should().Contain(expectedUrl);
        }

        [Fact]
        public void PreSigned_WithServiceThrowingException_ShouldThrowException()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var request = new PreSignedDto(fileName);

            _service.GeneratePreSignedUrl(Arg.Any<string>(), fileName).Throws(exception);

            // Act & Assert
            var actualException = Assert.Throws<Exception>(() => _controller.PreSigned(request));
            actualException.Message.Should().Be(expectedExceptionMessage);
            _service.Received(1).GeneratePreSignedUrl(Arg.Any<string>(), fileName);
        }

        #endregion

        #region StartMultiPart Tests

        [Fact]
        public async Task StartMultiPart_WithValidRequest_ShouldReturnKeyAndUploadId()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var expectedUploadId = _faker.Random.String(20);
            var request = new StartMultiPartDto(fileName);

            _service.StartMultiPart(Arg.Any<string>(), fileName).Returns(expectedUploadId);

            // Act
            var response = await _controller.StartMultiPart(request);

            // Assert
            await _service.Received(1).StartMultiPart(Arg.Any<string>(), fileName);

            var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
            okResult.Value.ToString().Should().Contain(expectedUploadId);
        }

        [Fact]
        public async Task StartMultiPart_WithServiceThrowingException_ShouldThrowException()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var request = new StartMultiPartDto(fileName);

            _service.StartMultiPart(Arg.Any<string>(), fileName).Throws(exception);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<Exception>(() => _controller.StartMultiPart(request));
            actualException.Message.Should().Be(expectedExceptionMessage);
            await _service.Received(1).StartMultiPart(Arg.Any<string>(), fileName);
        }

        #endregion

        #region PreSignedPart Tests

        [Fact]
        public void PreSignedPart_WithValidRequest_ShouldReturnKeyAndUrl()
        {
            // Arrange
            var key = _faker.Random.String(10);
            var fileName = _faker.System.FileName("mp4");
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);
            var expectedUrl = _faker.Internet.Url();
            var request = new PreSignedPartDto(fileName, uploadId, partNumber);

            _service.PreSignedPart(key, fileName, uploadId, partNumber).Returns(expectedUrl);

            // Act
            var response = _controller.PreSignedPart(key, request);

            // Assert
            _service.Received(1).PreSignedPart(key, fileName, uploadId, partNumber);

            var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull(expectedUrl);
            okResult.Value.ToString().Should().Contain(expectedUrl);
        }

        [Fact]
        public void PreSignedPart_WithServiceThrowingException_ShouldThrowException()
        {
            // Arrange
            var key = _faker.Random.String(10);
            var fileName = _faker.System.FileName("mp4");
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var request = new PreSignedPartDto(fileName, uploadId, partNumber);

            _service.PreSignedPart(key, fileName, uploadId, partNumber).Throws(exception);

            // Act & Assert
            var actualException = Assert.Throws<Exception>(() => _controller.PreSignedPart(key, request));
            actualException.Message.Should().Be(expectedExceptionMessage);
            _service.Received(1).PreSignedPart(key, fileName, uploadId, partNumber);
        }

        #endregion

        #region CompleteMultiPart Tests

        [Fact]
        public async Task CompleteMultiPart_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10)),
                new PartETagInfoDto(2, _faker.Random.String(10))
            };
            var request = new CompleteMultiPartDto(uploadId, parts);

            // Act
            var response = await _controller.CompleteMultiPart(key, request);

            // Assert
            await _service.Received(1).CompleteMultiPart(key, uploadId, parts);

            var okResult = response.Should().BeOfType<OkResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CompleteMultiPart_WithServiceThrowingException_ShouldThrowException()
        {
            // Arrange
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10))
            };
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var request = new CompleteMultiPartDto(uploadId, parts);

            _service.CompleteMultiPart(key, uploadId, parts).Throws(exception);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<Exception>(() => _controller.CompleteMultiPart(key, request));
            actualException.Message.Should().Be(expectedExceptionMessage);
            await _service.Received(1).CompleteMultiPart(key, uploadId, parts);
        }

        [Fact]
        public async Task CompleteMultiPart_WithEmptyParts_ShouldCallServiceWithEmptyList()
        {
            // Arrange
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>();
            var request = new CompleteMultiPartDto(uploadId, parts);

            // Act
            var response = await _controller.CompleteMultiPart(key, request);

            // Assert
            await _service.Received(1).CompleteMultiPart(key, uploadId, parts);

            var okResult = response.Should().BeOfType<OkResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        #endregion

        #region Helper Methods

        private IFormFile CreateFormFile(string fileName, string contentType, long length)
        {
            var file = Substitute.For<IFormFile>();
            file.FileName.Returns(fileName);
            file.ContentType.Returns(contentType);
            file.Length.Returns(length);

            if (length > 0)
            {
                var stream = new MemoryStream(new byte[length]);
                file.OpenReadStream().Returns(stream);
            }

            return file;
        }

        #endregion
    }
}