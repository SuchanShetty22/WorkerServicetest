# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the .csproj first and restore dependencies
COPY testingservice.csproj ./
RUN dotnet restore "./testingservice.csproj"

# Copy all source code
COPY . . 

# Build the project
RUN dotnet build "./testingservice.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the project
RUN dotnet publish "./testingservice.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Recommended environment variables for containerized .NET apps
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_NOLOGO=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "testingservice.dll"]
