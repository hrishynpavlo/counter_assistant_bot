FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
ARG TARGETARCH
WORKDIR /build
COPY ./src/ .
RUN dotnet restore
RUN dotnet tool restore 
RUN dotnet build -p:TargetArch=$TARGETARCH 
RUN dotnet test --no-build --collect "XPlat Code Coverage" --settings ./CounterAssistant.UnitTests/coverlet.runsettings --filter TestCategory!=MongoIntegration
RUN dotnet reportgenerator -reports:**/TestResults/**/coverage.opencover.xml -targetdir:codecoverage  -reporttypes:textSummary
RUN dotnet publish -c Release -p:TargetArch=$TARGETARCH -o published-app

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
ARG TARGETARCH
ARG GITHUB_SHA
ENV COMMIT_HASH=$GITHUB_SHA
COPY --from=build-env /build/published-app /app
COPY --from=build-env /build/codecoverage/Summary.txt /app
ENTRYPOINT ["dotnet", "CounterAssistant.API.dll"]
