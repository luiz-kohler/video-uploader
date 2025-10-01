using API.Controllers;
using API.Interfaces;
using API.Services;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using Tests.Unit.Setup.Builders;

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

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnFileIdSuccessfully()
        {
            var fileId = _faker.Random.String();

            var file = new IFormFileBuilder()
                .Build();

            _service.Upload(Arg.Any<IFormFile>()).Returns(fileId);

            var response = await _controller.Upload(file);

            await _service.Received(1).Upload(Arg.Any<IFormFile>());

            response.Should().NotBeNull();
            var acceptedResult = response as AcceptedResult;
            acceptedResult.Should().NotBeNull();
            acceptedResult.StatusCode.Should().Be(202);
            acceptedResult.Value.Should().BeEquivalentTo(new { fileId });
        }

        [Fact]
        public async Task Upload_WithNullFile_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be informed";

            var response = await _controller.Upload(null);

            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task Upload_WithEmptyFile_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be informed";

            var file = new IFormFileBuilder()
                .WithLength(0)
                .Build();

            var response = await _controller.Upload(file);

            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task Upload_WithNonMp4File_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be .mp4";

            var file = new IFormFileBuilder()
                .WithContentType("image/png")
                .Build();

            var response = await _controller.Upload(file);

            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task Upload_WithServiceThrowingException_ShouldReturnInternalServerError()
        {
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);

            var file = new IFormFileBuilder()
                .Build();

            _service.Upload(Arg.Any<IFormFile>()).Throws(exception);

            var response = await _controller.Upload(file);

            await _service.Received(1).Upload(Arg.Any<IFormFile>());

            var objectResult = response.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            var problemDetails = objectResult.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Detail.Should().Be(expectedExceptionMessage);
        }

        [Fact]
        public void PreSignedUrl_ShouldReturnUrlSuccessfully()
        {
            var expectedUrl = _faker.Random.String();
            _service.GeneratePreSignedUrl(Arg.Any<string>()).Returns(expectedUrl);

            var response = _controller.PreSignedUrl();

            _service.Received(1).GeneratePreSignedUrl(Arg.Any<string>());

            response.Should().NotBeNull();
            var okResult = response as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull(expectedUrl);
            okResult.Value.ToString().Should().Contain(expectedUrl);
        }

        [Fact]
        public void PreSignedUrl_WithServiceThrowingException_ShouldReturnInternalServerError()
        {
            var expectedExceptionMessage = _faker.Random.String();
            var exception = new Exception(expectedExceptionMessage);

            _service.GeneratePreSignedUrl(Arg.Any<string>()).Throws(exception);

            var response = _controller.PreSignedUrl();

            _service.Received(1).GeneratePreSignedUrl(Arg.Any<string>());

            var objectResult = response.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            var problemDetails = objectResult.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails.Detail.Should().Be(expectedExceptionMessage);
        }
    }
}
