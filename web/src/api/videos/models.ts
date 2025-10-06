type StartMultiPartRequest = {
    fileName: string
}

type StartMultiPartResponse = {
    key: string,
    uploadId: string
}

type PreSignedPartRequest = {
    key: string,
    fileName: string,
    uploadId: string,
    partNumber: number
}

type PreSignedPartResponse = {
    key: string,
    url: string
}

type PartETagInfo = {
    partNumber: number,
    etag: string
}

type CompleteMultiPartRequest = {
    key: string, 
    uploadId: string,
    parts: PartETagInfo[]
}

export type {
    StartMultiPartRequest,
    StartMultiPartResponse,
    PreSignedPartRequest,
    PreSignedPartResponse,
    PartETagInfo,
    CompleteMultiPartRequest
}