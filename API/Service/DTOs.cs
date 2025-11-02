using System.ComponentModel.DataAnnotations;

namespace Service
{
    public record StartMultiPartDto(
        [Required]
        string FileName);

    public record PreSignedPartDto(
        [Required]
        string UploadId,
        [Required]
        [Range(0, 1000)]
        int PartNumber);

    public record CompleteMultiPartDto(
        [Required]
        string UploadId,
        List<PartETagInfoDto> Parts);

    public record PartETagInfoDto(
        [Range(0, 1000)]
        int PartNumber,
        [Required]
        string ETag);

}
