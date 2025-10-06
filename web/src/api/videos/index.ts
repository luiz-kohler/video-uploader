import { api } from '../api';
import { 
    CompleteMultiPartRequest, 
    PreSignedPartRequest, 
    StartMultiPartRequest } from './models';

const VIDEOS_CONTROLLER = 'videos';

export const StartMultiPart = async (request : StartMultiPartRequest) => {
    return api.post(`${VIDEOS_CONTROLLER}/start-multipart`, request)
        .then(res => res);
}

export const PreSignedPart = async (request: PreSignedPartRequest) => {
    return api.post(`${VIDEOS_CONTROLLER}/${request.key}/pre-signed-part`)
        .then(res => res)
}

export const CompleteMultiPart = async (request: CompleteMultiPartRequest) => {
    return api.post(`${VIDEOS_CONTROLLER}/${request.key}/complete-multipart`)
        .then(res => res)
}