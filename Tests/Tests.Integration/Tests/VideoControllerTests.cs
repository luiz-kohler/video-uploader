using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Tests.Integration.Setup;

namespace Tests.Integration.Tests
{
    [Trait("Category", "Integration")]
    [Collection("VideoController")]
    public class VideoControllerTests : BaseIntegrationTest
    {
        public VideoControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnAccepted()
        {
            var fileName = _faker.System.FileName("mp4");
            var fileContent = _faker.Random.String();

            using var formData = new MultipartFormDataContent();
            var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
            var byteArrayContent = new ByteArrayContent(fileContentBytes);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            formData.Add(byteArrayContent, "file", fileName);

            var response = await _httpClient.PostAsync("/videos/upload", formData);

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            response.Content.Should().NotBeNull();

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("fileId");
        }

        [Fact]
        public async Task Upload_WithNullFile_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be informed";

            using var emptyFormData = new MultipartFormDataContent();
            var response = await _httpClient.PostAsync("/videos/upload", emptyFormData);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task Upload_WithEmptyFile_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be informed";
            var fileName = _faker.System.FileName("mp4");

            using var formData = new MultipartFormDataContent();
            var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
            emptyFileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            formData.Add(emptyFileContent, "file", fileName);

            var response = await _httpClient.PostAsync("/videos/upload", formData);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task Upload_WithNonMp4File_ShouldReturnBadRequest()
        {
            var expectedMessage = "File must be .mp4";
            var fileName = _faker.System.FileName("png");
            var fileContent = _faker.Random.String();

            using var formData = new MultipartFormDataContent();
            var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
            var byteArrayContent = new ByteArrayContent(fileContentBytes);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(byteArrayContent, "file", fileName);

            var response = await _httpClient.PostAsync("/videos/upload", formData);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Be(expectedMessage);
        }
    }
}
