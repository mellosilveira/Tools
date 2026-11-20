FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props NuGet.Config ./
COPY src/ src/
RUN dotnet restore src/MelloSilveiraTools.WebApi.Host/MelloSilveiraTools.WebApi.Host.csproj
RUN dotnet publish src/MelloSilveiraTools.WebApi.Host/MelloSilveiraTools.WebApi.Host.csproj -c Release -o /app/publish --no-restore -p:GeneratePackageOnBuild=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN mkdir -p /app/storage
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MelloSilveiraTools.WebApi.Host.dll"]
