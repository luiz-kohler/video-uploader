using Bogus;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Text;

namespace Tests.Unit.Setup.Builders
{
    public class IFormFileBuilder
    {
        private readonly Faker _faker;

        private string _contentType;
        private string _fileName;
        private int? _length;

        public IFormFileBuilder()
        {
            _faker = new Faker();
        }

        public IFormFileBuilder WithFileName(string fileName)
        {
            _fileName = fileName;
            return this;
        }

        public IFormFileBuilder WithLength(int length)
        {
            _length = length;
            return this;
        }

        public IFormFileBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        public IFormFile Build()
        {
            var file = CreateMockMp4File();

            file.FileName.Returns(_fileName ?? _faker.Random.String());
            file.Length.Returns(_length ?? _faker.Random.Number(100, 1000));
            file.ContentType.Returns(_contentType ?? "video/mp4");

            return file;
        }


        private static IFormFile CreateMockMp4File()
        {
            var videoBytes = CreateMinimalMp4Bytes(1024);
            var stream = new MemoryStream(videoBytes);

            var file = Substitute.For<IFormFile>();
            file.OpenReadStream().Returns(stream);

            file.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    var targetStream = x.Arg<Stream>();
                    var token = x.Arg<CancellationToken>();
                    return stream.CopyToAsync(targetStream, token);
                });

            return file;
        }

        private static byte[] CreateMinimalMp4Bytes(int fileSizeKB)
        {
            var bytes = new List<byte>();

            bytes.AddRange(Encoding.ASCII.GetBytes("ftypmp42"));

            while (bytes.Count < fileSizeKB * 1024)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes("mdat"));
            }

            return bytes.Take(fileSizeKB * 1024).ToArray();
        }
    }

}
