FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 as build-env
WORKDIR /build
COPY ./src/ .
RUN dotnet restore
RUN dotnet build --runtime linux-arm64
RUN dotnet publish CounterAssistant.MCP/CounterAssistant.MCP.csproj -c Release --runtime linux-arm64 -o published-app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ARG GITHUB_SHA
ENV COMMIT_HASH=$GITHUB_SHA
COPY --from=build-env /build/published-app /app
ENTRYPOINT ["dotnet", "CounterAssistant.MCP.dll"]
