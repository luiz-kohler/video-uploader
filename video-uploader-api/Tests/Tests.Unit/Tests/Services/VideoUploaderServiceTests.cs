using Bogus;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service;
using Service.Services;

namespace Tests.Unit.Tests.Services
{
    [Trait("Category", "Unit")]
    [Collection("VideoUploaderService")]
    public class VideoUploaderServiceTests
    {
        private readonly Faker _faker;
        private readonly VideoUploaderService _service;
        private readonly IS3Service _s3Service;

        public VideoUploaderServiceTests()
        {
            _faker = new Faker();
            _s3Service = Substitute.For<IS3Service>();
            _service = new VideoUploaderService(_s3Service);
        }

        [Fact]
        public async Task StartMultiPart_WithValidRequest_ShouldReturnKeyAndUploadId()
        {
            // Arrange
            var request = new StartMultiPartRequest(_faker.System.FileName());
            var expectedUploadId = _faker.Random.AlphaNumeric(20);

            _s3Service.StartMultiPart(Arg.Any<string>(), request.FileName)
                      .Returns(expectedUploadId);

            // Act
            var result = await _service.StartMultiPart(request);

            // Assert
            result.Should().NotBeNull();
            result.Key.Should().NotBeNullOrEmpty();
            result.UploadId.Should().Be(expectedUploadId);
            await _s3Service.Received(1).StartMultiPart(Arg.Is<string>(k => !string.IsNullOrEmpty(k)), request.FileName);
        }

        [Fact]
        public async Task StartMultiPart_WhenS3ServiceFails_ShouldThrowException()
        {
            // Arrange
            var request = new StartMultiPartRequest(_faker.System.FileName());
            var s3Exception = new Amazon.S3.AmazonS3Exception("S3 error");

            _s3Service.StartMultiPart(Arg.Any<string>(), request.FileName)
                      .ThrowsAsync(s3Exception);

            // Act & Assert
            await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(() =>
                _service.StartMultiPart(request));

            await _s3Service.Received(1).StartMultiPart(Arg.Any<string>(), request.FileName);
        }

        [Fact]
        public async Task StartMultiPart_ShouldGenerateUniqueKeyForEachRequest()
        {
            // Arrange
            var request = new StartMultiPartRequest(_faker.System.FileName());
            var expectedUploadId = _faker.Random.AlphaNumeric(20);

            _s3Service.StartMultiPart(Arg.Any<string>(), request.FileName)
                      .Returns(expectedUploadId);

            // Act
            var result1 = await _service.StartMultiPart(request);
            var result2 = await _service.StartMultiPart(request);

            // Assert
            result1.Key.Should().NotBe(result2.Key);
            result1.UploadId.Should().Be(expectedUploadId);
            result2.UploadId.Should().Be(expectedUploadId);
            await _s3Service.Received(2).StartMultiPart(Arg.Any<string>(), request.FileName);
        }

        [Fact]
        public void PreSignedPart_WithValidRequest_ShouldReturnPreSignedUrl()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var request = new PreSignedPartRequest(
                _faker.Random.AlphaNumeric(20),
                _faker.Random.Int(1, 1000));
            var expectedUrl = _faker.Internet.Url();

            _s3Service.PreSignedPart(key, request.UploadId, request.PartNumber)
                      .Returns(expectedUrl);

            // Act
            var result = _service.PreSignedPart(key, request);

            // Assert
            result.Should().NotBeNull();
            result.Url.Should().Be(expectedUrl);
            _s3Service.Received(1).PreSignedPart(key, request.UploadId, request.PartNumber);
        }

        [Fact]
        public void PreSignedPart_WhenS3ServiceFails_ShouldThrowException()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var request = new PreSignedPartRequest(
                _faker.Random.AlphaNumeric(20),
                _faker.Random.Int(1, 1000));
            var s3Exception = new Amazon.S3.AmazonS3Exception("S3 error");

            _s3Service.PreSignedPart(key, request.UploadId, request.PartNumber)
                      .Throws(s3Exception);

            // Act & Assert
            Assert.Throws<Amazon.S3.AmazonS3Exception>(() =>
                _service.PreSignedPart(key, request));

            _s3Service.Received(1).PreSignedPart(key, request.UploadId, request.PartNumber);
        }

        [Fact]
        public void PreSignedPart_WithInvalidPartNumber_ShouldCallS3ServiceWithSamePartNumber()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var partNumber = _faker.Random.Int(1, 1000);
            var request = new PreSignedPartRequest(_faker.Random.AlphaNumeric(20), partNumber);
            var expectedUrl = _faker.Internet.Url();

            _s3Service.PreSignedPart(key, request.UploadId, partNumber)
                      .Returns(expectedUrl);

            // Act
            var result = _service.PreSignedPart(key, request);

            // Assert
            result.Url.Should().Be(expectedUrl);
            _s3Service.Received(1).PreSignedPart(key, request.UploadId, partNumber);
        }

        [Fact]
        public async Task CompleteMultiPart_WithValidRequest_ShouldCompleteSuccessfully()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.AlphaNumeric(10)),
                new PartETagInfoDto(2, _faker.Random.AlphaNumeric(10))
            };
            var request = new CompleteMultiPartRequest(_faker.Random.AlphaNumeric(20), parts);

            // Act
            await _service.CompleteMultiPart(key, request);

            // Assert
            await _s3Service.Received(1).CompleteMultiPart(key, request.UploadId, request.Parts);
        }

        [Fact]
        public async Task CompleteMultiPart_WhenS3ServiceFails_ShouldThrowException()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.AlphaNumeric(10))
            };
            var request = new CompleteMultiPartRequest(_faker.Random.AlphaNumeric(20), parts);
            var s3Exception = new Amazon.S3.AmazonS3Exception("Completion failed");

            _s3Service.CompleteMultiPart(key, request.UploadId, request.Parts)
                      .ThrowsAsync(s3Exception);

            // Act & Assert
            await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(() =>
                _service.CompleteMultiPart(key, request));

            await _s3Service.Received(1).CompleteMultiPart(key, request.UploadId, request.Parts);
        }

        [Fact]
        public async Task CompleteMultiPart_WithEmptyPartsList_ShouldCallS3ServiceWithEmptyParts()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var request = new CompleteMultiPartRequest(_faker.Random.AlphaNumeric(20), new List<PartETagInfoDto>());

            // Act
            await _service.CompleteMultiPart(key, request);

            // Assert
            await _s3Service.Received(1).CompleteMultiPart(key, request.UploadId, request.Parts);
        }

        [Fact]
        public async Task CompleteMultiPart_WithNullParts_ShouldCallS3ServiceWithNullParts()
        {
            // Arrange
            var key = _faker.Random.Guid().ToString();
            var request = new CompleteMultiPartRequest(_faker.Random.AlphaNumeric(20), null);

            // Act
            await _service.CompleteMultiPart(key, request);

            // Assert
            await _s3Service.Received(1).CompleteMultiPart(key, request.UploadId, request.Parts);
        }

        [Fact]
        public void PreSignedPart_WithNullKey_ShouldCallS3ServiceWithNullKey()
        {
            // Arrange
            string key = null;
            var request = new PreSignedPartRequest(_faker.Random.AlphaNumeric(20), _faker.Random.Int(1, 1000));
            var expectedUrl = _faker.Internet.Url();

            _s3Service.PreSignedPart(key, request.UploadId, request.PartNumber)
                      .Returns(expectedUrl);

            // Act
            var result = _service.PreSignedPart(key, request);

            // Assert
            result.Url.Should().Be(expectedUrl);
            _s3Service.Received(1).PreSignedPart(key, request.UploadId, request.PartNumber);
        }

        [Fact]
        public async Task StartMultiPart_WithEmptyFileName_ShouldCallS3ServiceWithEmptyFileName()
        {
            // Arrange
            var request = new StartMultiPartRequest("");
            var expectedUploadId = _faker.Random.AlphaNumeric(20);

            _s3Service.StartMultiPart(Arg.Any<string>(), "")
                      .Returns(expectedUploadId);

            // Act
            var result = await _service.StartMultiPart(request);

            // Assert
            result.UploadId.Should().Be(expectedUploadId);
            await _s3Service.Received(1).StartMultiPart(Arg.Any<string>(), "");
        }
    }
}