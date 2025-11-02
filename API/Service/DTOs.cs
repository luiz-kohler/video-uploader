using System.ComponentModel.DataAnnotations;

namespace Service
{
    #region Requests

    public record StartMultiPartRequest(
        [Required]
        string FileName);

    public record PreSignedPartRequest(
        [Required]
        string UploadId,
        [Required]
        [Range(0, 1000)]
        int PartNumber);

    public record CompleteMultiPartRequest(
        [Required]
        string UploadId,
        List<PartETagInfoDto> Parts);

    #endregion

    #region Responses

    public record StartMultiPartResponse(
        string Key,
        string UploadId);

    public record PreSignedPartResponse(
        string Url);

    #endregion

    #region Generics

    public record PartETagInfoDto(
        [Range(0, 1000)]
        int PartNumber,
        [Required]
        string ETag);

    #endregion
}
