using Amazon.S3;
using Amazon.S3.Model;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service;
using Service.Services;
using System.Net;

namespace Tests.Unit.Tests.Services
{
    [Trait("Category", "Unit")]
    [Collection("AwsS3Service")]
    public class AwsS3ServiceTests
    {
        private readonly Faker _faker;
        private readonly S3Service _service;
        private readonly S3Settings _settings;
        private readonly IAmazonS3 _client;

        public AwsS3ServiceTests()
        {
            _faker = new Faker();

            var options = Substitute.For<IOptions<S3Settings>>();
            _client = Substitute.For<IAmazonS3>();

            _settings = new Faker<S3Settings>()
                .RuleFor(x => x.BucketName, f => f.Random.Word())
                .RuleFor(x => x.Endpoint, f => f.Internet.Url())
                .RuleFor(x => x.AccessKey, f => f.Random.AlphaNumeric(20))
                .RuleFor(x => x.SecretKey, f => f.Random.AlphaNumeric(40))
                .Generate();

            options.Value.Returns(_settings);

            _service = new S3Service(options, _client);
        }

        [Fact]
        public async Task StartMultiPart_ShouldReturnUploadIdSuccessfully()
        {
            var key = _faker.Random.String(10);
            var fileName = _faker.System.FileName();
            var expectedUploadId = _faker.Random.String(20);

            _client.InitiateMultipartUploadAsync(Arg.Any<InitiateMultipartUploadRequest>())
                   .Returns(new InitiateMultipartUploadResponse
                   {
                       UploadId = expectedUploadId
                   });

            var response = await _service.StartMultiPart(key, fileName);

            await _client.Received(1).InitiateMultipartUploadAsync(Arg.Any<InitiateMultipartUploadRequest>());
            response.Should().Be(expectedUploadId);
        }

        [Fact]
        public async Task StartMultiPart_WhenInitiateFails_ShouldThrowException()
        {
            var key = _faker.Random.String(10);
            var fileName = _faker.System.FileName();

            _client.InitiateMultipartUploadAsync(Arg.Any<InitiateMultipartUploadRequest>())
                   .Throws(new AmazonS3Exception("S3 error"));

            await Assert.ThrowsAsync<AmazonS3Exception>(() => _service.StartMultiPart(key, fileName));
            await _client.Received(1).InitiateMultipartUploadAsync(Arg.Any<InitiateMultipartUploadRequest>());
        }

        [Fact]
        public void PreSignedPart_ShouldReturnUrlSuccessfully()
        {
            var expectedUrl = _faker.Internet.Url();
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);

            _client.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>()).Returns(expectedUrl);

            var response = _service.PreSignedPart(key, uploadId, partNumber);

            _client.Received(1).GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>());
            response.Should().Be(expectedUrl);
        }

        [Fact]
        public void PreSignedPart_WhenGenerateUrlFails_ShouldThrowException()
        {
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var partNumber = _faker.Random.Int(1, 10);

            _client.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>())
                   .Throws(new AmazonS3Exception("S3 error"));

            Assert.Throws<AmazonS3Exception>(() =>
                _service.PreSignedPart(key, uploadId, partNumber));
            _client.Received(1).GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>());
        }

        [Fact]
        public async Task CompleteMultiPart_WithValidParts_ShouldCompleteSuccessfully()
        {
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10)),
                new PartETagInfoDto(2, _faker.Random.String(10))
            };

            _client.CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>())
                   .Returns(new CompleteMultipartUploadResponse
                   {
                       HttpStatusCode = HttpStatusCode.OK,
                       Location = _faker.Internet.Url()
                   });

            await _service.CompleteMultiPart(key, uploadId, parts);

            await _client.Received(1).CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>());
        }

        [Fact]
        public async Task CompleteMultiPart_WhenResponseIsNotOk_ShouldThrowException()
        {
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10))
            };

            _client.CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>())
                   .Returns(new CompleteMultipartUploadResponse
                   {
                       HttpStatusCode = HttpStatusCode.BadRequest,
                       Location = null
                   });

            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() =>
                _service.CompleteMultiPart(key, uploadId, parts));

            exception.Message.Should().Be("Multipart couldn't be completed.");
            await _client.Received(1).CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>());
        }

        [Fact]
        public async Task CompleteMultiPart_WhenLocationIsEmpty_ShouldThrowException()
        {
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10))
            };

            _client.CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>())
                   .Returns(new CompleteMultipartUploadResponse
                   {
                       HttpStatusCode = HttpStatusCode.OK,
                       Location = string.Empty
                   });

            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() =>
                _service.CompleteMultiPart(key, uploadId, parts));

            exception.Message.Should().Be("Multipart couldn't be completed.");
            await _client.Received(1).CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>());
        }

        [Fact]
        public async Task CompleteMultiPart_WhenS3Fails_ShouldThrowException()
        {
            var key = _faker.Random.String(10);
            var uploadId = _faker.Random.String(20);
            var parts = new List<PartETagInfoDto>
            {
                new PartETagInfoDto(1, _faker.Random.String(10))
            };

            _client.CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>())
                   .Throws(new AmazonS3Exception("S3 error"));

            await Assert.ThrowsAsync<AmazonS3Exception>(() =>
                _service.CompleteMultiPart(key, uploadId, parts));
            await _client.Received(1).CompleteMultipartUploadAsync(Arg.Any<CompleteMultipartUploadRequest>());
        }
    }
}