using API.Controllers;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Presentation;
using Service;
using Service.Services;

namespace Tests.Unit.Tests.Controllers
{
    [Trait("Category", "Unit")]
    [Collection("VideoController")]
    public class VideoControllerTests
    {
        private readonly Faker _faker;
        private readonly VideoController _controller;
        private readonly IS3Service _service;

        public VideoControllerTests()
        {
            _faker = new Faker();
            _service = Substitute.For<IS3Service>();
            _controller = new VideoController(_service);
        }

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
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);
            var expectedUrl = _faker.Internet.Url();
            var request = new PreSignedPartDto(uploadId, partNumber);

            _service.PreSignedPart(key, uploadId, partNumber).Returns(expectedUrl);

            // Act
            var response = _controller.PreSignedPart(key, request);

            // Assert
            _service.Received(1).PreSignedPart(key, uploadId, partNumber);

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
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);
            var request = new PreSignedPartDto(uploadId, partNumber);

            _service.PreSignedPart(key, uploadId, partNumber).Throws(exception);

            // Act & Assert
            var actualException = Assert.Throws<Exception>(() => _controller.PreSignedPart(key, request));
            actualException.Message.Should().Be(expectedExceptionMessage);
            _service.Received(1).PreSignedPart(key, uploadId, partNumber);
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