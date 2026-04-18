# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenMES.MigrationService/OpenMES.MigrationService.csproj", "OpenMES.MigrationService/"]
COPY ["OpenMES.Data.Pgsql/OpenMES.Data.Pgsql.csproj", "OpenMES.Data.Pgsql/"]
COPY ["OpenMES.Data/OpenMES.Data.csproj", "OpenMES.Data/"]
COPY ["OpenMES.Data.Common/OpenMES.Data.Common.csproj", "OpenMES.Data.Common/"]
COPY ["OpenMES.Data.Dtos/OpenMES.Data.Dtos.csproj", "OpenMES.Data.Dtos/"]
COPY ["OpenMES.Data.SqlServer/OpenMES.Data.SqlServer.csproj", "OpenMES.Data.SqlServer/"]
COPY ["OpenMES.ServiceDefaults/OpenMES.ServiceDefaults.csproj", "OpenMES.ServiceDefaults/"]
RUN dotnet restore "./OpenMES.MigrationService/OpenMES.MigrationService.csproj"
COPY . .
WORKDIR "/src/OpenMES.MigrationService"
RUN dotnet build "./OpenMES.MigrationService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenMES.MigrationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenMES.MigrationService.dll"]