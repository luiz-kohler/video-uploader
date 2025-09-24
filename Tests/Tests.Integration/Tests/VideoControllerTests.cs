using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
            // ARRANGE
            var fileName = "test-video.mp4";
            var fileContent = "fake video content for integration test";

            using var formData = new MultipartFormDataContent();
            var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
            var byteArrayContent = new ByteArrayContent(fileContentBytes);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            formData.Add(byteArrayContent, "file", fileName);

            // ACTION
            var response = await _httpClient.PostAsync("/videos/upload", formData);

            // ASSERT
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
            response.Content.Should().NotBeNull();
        }
    }
}
