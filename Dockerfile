FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env
WORKDIR /build
RUN apt-get update && \
      apt-get -y install sudo
RUN sudo apt-get install gnupg && \
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv 7F0CEB10 && \ 
    sudo apt-get install -y mongodb-org && \
    echo 'deb http://downloads-distro.mongodb.org/repo/ubuntu-upstart dist 10gen' | sudo tee /etc/apt/sources.list.d/mongodb.list && \
    sudo apt-get update && \
    sudo apt-get install mongodb-org
RUN sudo systemctl start mongod
COPY ./src/ .
RUN dotnet restore
RUN dotnet tool restore
RUN dotnet build
RUN dotnet test --no-build --collect "XPlat Code Coverage"  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
RUN dotnet reportgenerator -reports:**/TestResults/**/coverage.opencover.xml -targetdir:codecoverage  -reporttypes:textSummary
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
ARG APP_NAME="CounterAssistant.API"
COPY --from=build-env /build/$APP_NAME/bin/Release/net5.0/publish /app
COPY --from=build-env /build/codecoverage/Summary.txt /app
CMD ASPNETCORE_URLS=http://*:$PORT dotnet CounterAssistant.API.dll