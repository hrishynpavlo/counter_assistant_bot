FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env
WORKDIR /build
RUN apt-get update && \
      apt-get -y install sudo
RUN sudo apt-get -y install gnupg 
RUN echo "deb http://repo.mongodb.org/apt/debian buster/mongodb-org/4.4 main" | sudo tee /etc/apt/sources.list.d/mongodb-org-4.4.list
RUN sudo apt-key list
RUN sudo apt-get update 
RUN sudo apt-get install -y mongodb-org
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