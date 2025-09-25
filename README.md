docker-compose --profile docker up -d

docker-compose --profile local up -d

docker build -t "luizfelipekohler/video-uploader-api" .

docker push "luizfelipekohler/video-uploader-api"
