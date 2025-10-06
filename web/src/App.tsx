import React, { useState } from 'react';
import './global.css'
import { Button, Grid, LinearProgress, Typography, styled } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { StartMultiPart, PreSignedPart, CompleteMultiPart } from './api/videos/index'
import {
  StartMultiPartRequest,
  StartMultiPartResponse,
  PreSignedPartResponse,
  CompleteMultiPartRequest,
  PartETagInfo
} from './api/videos/models'

const VisuallyHiddenInput = styled('input')({
  clip: 'rect(0 0 0 0)',
  clipPath: 'inset(50%)',
  height: 1,
  overflow: 'hidden',
  position: 'absolute',
  bottom: 0,
  left: 0,
  whiteSpace: 'nowrap',
  width: 1,
});

const CHUNK_SIZE = 5 * 1024 * 1024;
function App() {
  const [uploadStatus, setUploadStatus] = useState<'initial' | 'uploading' | 'success' | 'error'>('initial');
  const [uploadProgress, setUploadProgress] = useState(0);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const splitFileIntoChunks = (file: File): Blob[] => {
    const chunks: Blob[] = [];
    let start = 0;

    while (start < file.size) {
      const end = Math.min(start + CHUNK_SIZE, file.size);
      const chunk = file.slice(start, end);
      chunks.push(chunk);
      start = end;
    }

    return chunks;
  };

  const uploadChunk = async (presignedUrl: string, chunk: Blob, partNumber: number): Promise<PartETagInfo> => {
    const response = await fetch(presignedUrl, {
      method: 'PUT',
      body: chunk,
      headers: {
        'Content-Type': 'application/octet-stream',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to upload chunk ${partNumber}`);
    }

    const etag = response.headers.get('ETag');
    if (!etag) {
      throw new Error(`No ETag received for chunk ${partNumber}`);
    }

    return {
      partNumber,
      etag: etag.replace(/"/g, '')
    };
  };

  const handleFileUpload = async (file: File) => {
    if (!file) return;

    setUploadStatus('uploading');
    setUploadProgress(0);

    try {
      const startRequest: StartMultiPartRequest = {
        fileName: file.name
      };

      const startResponse = await StartMultiPart(startRequest);
      const { key, uploadId } = startResponse.data as StartMultiPartResponse;

      const chunks = splitFileIntoChunks(file);
      const totalChunks = chunks.length;
      const uploadedParts: PartETagInfo[] = [];

      for (let i = 0; i < chunks.length; i++) {
        const partNumber = i + 1;
        const chunk = chunks[i];

        try {
          const presignedRequest = {
            key,
            fileName: file.name,
            uploadId,
            partNumber
          };

          const presignedResponse = await PreSignedPart(presignedRequest);

          const { url: presignedUrl } = presignedResponse.data as PreSignedPartResponse;

          const partInfo = await uploadChunk(presignedUrl, chunk, partNumber);
          uploadedParts.push(partInfo);

          const progress = ((i + 1) / totalChunks) * 100;
          setUploadProgress(progress);

        } catch (error) {
          throw new Error(`Part ${partNumber} upload failed`);
        }
      }

      const completeRequest: CompleteMultiPartRequest = {
        key,
        uploadId,
        parts: uploadedParts.sort((a, b) => a.partNumber - b.partNumber)
      };

      await CompleteMultiPart(completeRequest);

      setUploadStatus('success');
      setUploadProgress(100);

    } catch (error) {
      console.error('Upload failed:', error);
      setUploadStatus('error');
    }
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (files && files.length > 0) {
      const file = files[0];
      setSelectedFile(file);
      handleFileUpload(file);
    }
  };

  return (
    <Grid
      container
      spacing={0}
      direction="column"
      alignItems="center"
      justifyContent="center"
      sx={{ minHeight: '100vh', padding: 2 }}
    >
      <Button
        component="label"
        role={undefined}
        variant="contained"
        tabIndex={-1}
        startIcon={<CloudUploadIcon />}
        disabled={uploadStatus === 'uploading'}
      >
        Upload Video
        <VisuallyHiddenInput
          type="file"
          onChange={handleFileChange}
          accept='.mp4'
        />
      </Button>

      {selectedFile && (
        <div style={{ marginTop: 16, width: '100%', maxWidth: 400 }}>
          <Typography variant="body2" gutterBottom>
            Selected: {selectedFile.name} ({(selectedFile.size / (1024 * 1024)).toFixed(2)} MB)
          </Typography>
        </div>
      )}

      {uploadStatus === 'uploading' && (
        <div style={{ marginTop: 16, width: '100%', maxWidth: 400 }}>
          <LinearProgress
            variant="determinate"
            value={uploadProgress}
            sx={{ marginBottom: 1 }}
          />
          <Typography variant="body2" align="center">
            Uploading... {Math.round(uploadProgress)}%
          </Typography>
        </div>
      )}

      {uploadStatus === 'success' && (
        <Typography variant="body2" color="success.main" sx={{ marginTop: 2 }}>
          File uploaded successfully!
        </Typography>
      )}

      {uploadStatus === 'error' && (
        <Typography variant="body2" color="error" sx={{ marginTop: 2 }}>
          File upload failed.
        </Typography>
      )}
    </Grid>
  );
}

export default App;