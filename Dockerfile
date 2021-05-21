FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env
ARG APP_NAME="CounterAssistant.API"
WORKDIR /build
COPY ./src/$APP_NAME .
RUN dotnet restore 
RUN dotnet build
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:5.0
ARG GITHUB_SHA
ARG GITHUB_BRANCH
ENV GITHUB_BRANCH $GITHUB_BRANCH
ENV GITHUB_SHA $GITHUB_SHA
WORKDIR /app
COPY --from=build-env /build/bin/Release/net5.0/publish /app
CMD ASPNETCORE_URLS=http://*:$PORT dotnet CounterAssistant.API.dll