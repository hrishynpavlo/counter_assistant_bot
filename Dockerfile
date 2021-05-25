FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env
WORKDIR /build
COPY ./src/ .
RUN dotnet restore && build
RUN dotnet test
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
ARG APP_NAME="CounterAssistant.API"
COPY --from=build-env /build/$APP_NAME/bin/Release/net5.0/publish /app
CMD ASPNETCORE_URLS=http://*:$PORT dotnet CounterAssistant.API.dll