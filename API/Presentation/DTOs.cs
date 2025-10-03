namespace Presentation
{
    public record PreSignedDto(string FileName);
    public record StartMultiPartDto(string FileName);
    public record PreSignedPartDto(string FileName, string UploadId, int PartNumber);
    public record CompleteMultiPartDto(string UploadId, List<PartETagInfoDto> Parts);
    public record PartETagInfoDto(int PartNumber, string ETag);
}
