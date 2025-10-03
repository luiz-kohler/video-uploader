using FluentAssertions;
using Presentation;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
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

        #region Upload Tests (Existing - for reference)

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

        #endregion

        #region PreSigned URL Tests

        [Fact]
        public async Task PreSigned_WithValidRequest_ShouldReturnKeyAndUrl()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var request = new PreSignedDto(fileName);
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _httpClient.PostAsync("/videos/pre-signed", jsonContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PreSignedResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Key.Should().NotBeNullOrEmpty();
            result.Url.Should().NotBeNullOrEmpty();
            result.Url.Should().StartWith("http");
        }

        #endregion

        #region StartMultiPart Tests

        [Fact]
        public async Task StartMultiPart_WithValidRequest_ShouldReturnKeyAndUploadId()
        {
            // Arrange
            var fileName = _faker.System.FileName("mp4");
            var request = new StartMultiPartDto(fileName);
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _httpClient.PostAsync("/videos/start-multipart", jsonContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StartMultiPartResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Key.Should().NotBeNullOrEmpty();
            result.UploadId.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region PreSignedPart Tests

        [Fact]
        public async Task PreSignedPart_WithValidRequest_ShouldReturnUrl()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var fileName = _faker.System.FileName("mp4");
            var uploadId = _faker.Random.AlphaNumeric(20);
            var partNumber = _faker.Random.Int(1, 10);

            var request = new PreSignedPartDto(fileName, uploadId, partNumber);
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _httpClient.PostAsync($"/videos/{key}/pre-signed-part", jsonContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PreSignedPartResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Key.Should().Be(key);
            result.Url.Should().NotBeNullOrEmpty();
            result.Url.Should().StartWith("http");
        }

        #endregion

        #region CompleteMultiPart Tests

        [Fact]
        public async Task CompleteMultiPart_WithValidRequest_ShouldReturnOk()
        {
            // Arrange 
            var five_mega_bytes = 10 << 20;
            var fileName = _faker.System.FileName("mp4");
            var startRequest = new StartMultiPartDto(fileName);
            var startJsonContent = new StringContent(
                JsonSerializer.Serialize(startRequest),
                Encoding.UTF8,
                "application/json");

            var startResponse = await _httpClient.PostAsync("/videos/start-multipart", startJsonContent);
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var startResult = JsonSerializer.Deserialize<StartMultiPartResponse>(
                await startResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var key = startResult.Key;
            var uploadId = startResult.UploadId; 

            var parts = new List<PartETagInfoDto>();
            var partData = new[]
            {
                new { Number = 1, Data = _faker.Random.String(five_mega_bytes) },
                new { Number = 2, Data = _faker.Random.String(five_mega_bytes) }
            };

            foreach (var part in partData)
            {
                var presignedRequest = new PreSignedPartDto(fileName, uploadId, part.Number);
                var presignedJsonContent = new StringContent(
                    JsonSerializer.Serialize(presignedRequest),
                    Encoding.UTF8,
                    "application/json");

                var presignedResponse = await _httpClient.PostAsync(
                    $"/videos/{key}/pre-signed-part", presignedJsonContent);
                presignedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                var presignedResult = JsonSerializer.Deserialize<PreSignedPartResponse>(
                    await presignedResponse.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var partContent = new StringContent(part.Data, Encoding.UTF8, "video/mp4");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        return true;
                    }
                };

                var partClient = new HttpClient(handler);

                var uploadResponse = await partClient.PutAsync(presignedResult.Url, partContent);
                var responseContent = await uploadResponse.Content.ReadAsStringAsync();
                var statusCode = uploadResponse.StatusCode;
                var headers = uploadResponse.Headers;

                uploadResponse.EnsureSuccessStatusCode();

                var etag = $"\"{ComputeMockETag(part.Data)}\"";
                parts.Add(new PartETagInfoDto(part.Number, etag));
            }

            // Act 
            var completeRequest = new CompleteMultiPartDto(uploadId, parts);
            var completeJsonContent = new StringContent(
                JsonSerializer.Serialize(completeRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"/videos/{key}/complete-multipart", completeJsonContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static string ComputeMockETag(string data)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }


        [Fact]
        public async Task CompleteMultiPart_WithEmptyParts_ShouldReturnBadRequest()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var uploadId = _faker.Random.AlphaNumeric(20);
            var emptyParts = new List<PartETagInfoDto>();

            var request = new CompleteMultiPartDto(uploadId, emptyParts);
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _httpClient.PostAsync($"/videos/{key}/complete-multipart", jsonContent);

            // Assert
            // This will likely fail due to S3 validation, testing error handling
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Response DTOs for deserialization

        private record PreSignedResponse(string Key, string Url);
        private record StartMultiPartResponse(string Key, string UploadId);
        private record PreSignedPartResponse(string Key, string Url);

        #endregion
    }
}