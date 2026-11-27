FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props NuGet.Config README.md CHANGELOG.md LICENSE ./
COPY src/MelloSilveiraTools.Core/MelloSilveiraTools.Core.csproj src/MelloSilveiraTools.Core/
COPY src/MelloSilveiraTools.Database/MelloSilveiraTools.Database.csproj src/MelloSilveiraTools.Database/
COPY src/MelloSilveiraTools.Database.Migrator/MelloSilveiraTools.Database.Migrator.csproj src/MelloSilveiraTools.Database.Migrator/
RUN dotnet restore src/MelloSilveiraTools.Database.Migrator/MelloSilveiraTools.Database.Migrator.csproj

COPY src/MelloSilveiraTools.Core/ src/MelloSilveiraTools.Core/
COPY src/MelloSilveiraTools.Database/ src/MelloSilveiraTools.Database/
COPY src/MelloSilveiraTools.Database.Migrator/ src/MelloSilveiraTools.Database.Migrator/
RUN dotnet publish src/MelloSilveiraTools.Database.Migrator/MelloSilveiraTools.Database.Migrator.csproj -c Release -o /app/publish --self-contained true -p:EnableReadyToRun=true -p:GeneratePackageOnBuild=false

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./MelloSilveiraTools.Database.Migrator"]
