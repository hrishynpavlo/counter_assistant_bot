FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env
WORKDIR /build
COPY ./src/ .
RUN dotnet restore
RUN dotnet tool restore
RUN dotnet build
RUN dotnet test --no-build --collect "XPlat Code Coverage" --settings ./CounterAssistant.UnitTests/coverlet.runsettings --filter TestCategory!=MongoIntegration
RUN dotnet reportgenerator -reports:**/TestResults/**/coverage.opencover.xml -targetdir:codecoverage  -reporttypes:textSummary
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
ARG APP_NAME="CounterAssistant.API"
COPY --from=build-env /build/$APP_NAME/bin/Release/net5.0/publish /app
COPY --from=build-env /build/codecoverage/Summary.txt /app
CMD ASPNETCORE_URLS=http://*:$PORT dotnet CounterAssistant.API.dll