docker compose build

docker image tag ocm-web ubuntu-ocm-01:5000/ocm-web:latest
docker image tag ocm-api ubuntu-ocm-01:5000/ocm-api:latest

docker push ubuntu-ocm-01:5000/ocm-web:latest
docker push ubuntu-ocm-01:5000/ocm-api:latest