FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props NuGet.Config README.md CHANGELOG.md LICENSE ./
COPY src/ src/
RUN dotnet restore src/MelloSilveiraTools.WebApi.Host/MelloSilveiraTools.WebApi.Host.csproj
RUN dotnet publish src/MelloSilveiraTools.WebApi.Host/MelloSilveiraTools.WebApi.Host.csproj -c Release -o /app/publish --self-contained true -p:EnableReadyToRun=true -p:GeneratePackageOnBuild=false

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS="http://+:26324"
EXPOSE 26324
EXPOSE 24263
RUN mkdir -p /app/storage
COPY --from=build /app/publish .
ENTRYPOINT ["./MelloSilveiraTools.WebApi.Host"]
