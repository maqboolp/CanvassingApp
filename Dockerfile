# Multi-stage build for .NET Core API only
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["backend/HooverCanvassingApi/HooverCanvassingApi.csproj", "HooverCanvassingApi/"]
RUN dotnet restore "HooverCanvassingApi/HooverCanvassingApi.csproj"

# Copy source code and build
COPY backend/HooverCanvassingApi/ HooverCanvassingApi/
WORKDIR "/src/HooverCanvassingApi"
RUN dotnet build "HooverCanvassingApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HooverCanvassingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Install FFmpeg for audio conversion
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy API only
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HooverCanvassingApi.dll"]