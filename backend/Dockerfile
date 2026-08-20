FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ProductApi.csproj ./
RUN dotnet restore ProductApi.csproj

COPY . ./
RUN dotnet publish ProductApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

USER $APP_UID
ENTRYPOINT ["dotnet", "ProductApi.dll"]
