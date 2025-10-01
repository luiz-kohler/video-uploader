using Amazon.S3;
using Amazon.S3.Model;
using API.Services;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Tests.Unit.Tests.Services
{
    [Trait("Category", "Unit")]
    [Collection("MinIOService")]
    public class AwsS3ServiceTests
    {
        private readonly Faker _faker;
        private readonly AwsS3Service _service;
        private readonly AwsS3Settings _settings;
        private readonly IAmazonS3 _client;

        public AwsS3ServiceTests()
        {
            _faker = new Faker();

            var options = Substitute.For<IOptions<AwsS3Settings>>();
            _client = Substitute.For<IAmazonS3>();

            _settings = new Faker<AwsS3Settings>().Generate();
            options.Value.Returns(_settings);

            _service = new AwsS3Service(options, _client);
        }

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnFileIdSuccessfully()
        {
            var expectedResponse = _faker.Random.String();
            var bucketName = _settings.BucketName;

            var file = Substitute.For<IFormFile>();

            _client.PutObjectAsync(Arg.Any<PutObjectRequest>()).ReturnsNull();

            var response = await _service.Upload(file);

            await _client.Received(1).PutObjectAsync(Arg.Any<PutObjectRequest>());

            response.Should().NotBeNull(expectedResponse);
        }
    }
}