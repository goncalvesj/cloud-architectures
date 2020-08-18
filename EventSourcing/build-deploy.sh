DOCKER_IMAGE="goncalvesj/event-sourcing"
DOCKER_TAG="$DOCKER_IMAGE:dev"

#docker build --compress --tag $DOCKER_TAG -f ./EventSourcing.Web/Dockerfile .

#docker login -u goncalvesj

#docker push $DOCKER_TAG

rm *.tgz

helm package ./Helm/event-sourcing/

filename=$(find . -maxdepth 1 -type f -name "*.tgz")

echo $filename

helm upgrade ev $filename -i

# kubectl set image deployment/remote-monitoring-webui remote-monitoring-webui-pod=$DOCKER_TAG --record
# kubectl rollout status deployment/remote-monitoring-webui