using API.Services;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;

namespace Unit.Tests.Services
{
    [Trait("Category", "Unit")]
    [Collection("MinIOService")]
    public class MinIOServiceTests
    {
        private readonly Faker _faker;
        private readonly MinIOService _service;
        private readonly MinIOVariables _minIOVaraibles;
        private readonly IMinioClient _client;

        public MinIOServiceTests()
        {
            _faker = new Faker();

            var factory = Substitute.For<IMinioClientFactory>();
            var options = Substitute.For<IOptions<MinIOVariables>>();

            _client = Substitute.For<IMinioClient>();

            _minIOVaraibles = new Faker<MinIOVariables>().Generate();
            options.Value.Returns(_minIOVaraibles);

            factory.CreateClient().Returns(_client);

            _service = new MinIOService(factory, options);
        }

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnFileId()
        {
            var expectedResponse = _faker.Random.String();
            var bucketName = _minIOVaraibles.BucketName;

            var putObjectResponse = new Faker<PutObjectResponse>()
                .CustomInstantiator(f =>
                    new PutObjectResponse(f.PickRandom<HttpStatusCode>(),
                        f.Random.String(),
                        new Dictionary<string, string>(),
                        f.Random.Long(),
                        expectedResponse)
                );

            var file = Substitute.For<IFormFile>();

            _client.BucketExistsAsync(Arg.Any<BucketExistsArgs>()).Returns(false);
            _client.PutObjectAsync(Arg.Any<PutObjectArgs>()).Returns(putObjectResponse);

            var response = await _service.Upload(file);

            await _client.Received(1).BucketExistsAsync(Arg.Any<BucketExistsArgs>());
            await _client.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>());

            response.Should().NotBe(expectedResponse);
        }

        [Fact]
        public async Task Upload_WithInValidFile_ShouldReturnEmptyString()
        {
            var expectedResponse = string.Empty;
            var bucketName = _minIOVaraibles.BucketName;

            var putObjectResponse = new Faker<PutObjectResponse>()
                .CustomInstantiator(f =>
                    new PutObjectResponse(f.PickRandom<HttpStatusCode>(),
                        f.Random.String(),
                        new Dictionary<string, string>(),
                        f.Random.Long(),
                        expectedResponse)
                );

            var file = Substitute.For<IFormFile>();

            _client.BucketExistsAsync(Arg.Any<BucketExistsArgs>()).Returns(false);
            _client.PutObjectAsync(Arg.Any<PutObjectArgs>()).Throws<Exception>();

            var response = await _service.Upload(file);

            await _client.Received(1).BucketExistsAsync(Arg.Any<BucketExistsArgs>());
            await _client.Received(1).PutObjectAsync(Arg.Any<PutObjectArgs>());

            response.Should().Be(expectedResponse);
        }
    }
}