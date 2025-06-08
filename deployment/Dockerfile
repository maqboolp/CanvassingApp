# Multi-stage build for .NET Core API
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

# Build React frontend
FROM node:18-alpine AS frontend-build
WORKDIR /frontend

# Copy package files and install dependencies
COPY frontend/package*.json ./
RUN npm ci --only=production

# Copy source and build
COPY frontend/ ./
RUN npm run build

# Final stage
FROM base AS final
WORKDIR /app

# Copy API
COPY --from=publish /app/publish .

# Copy React build to wwwroot
COPY --from=frontend-build /frontend/build ./wwwroot

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HooverCanvassingApi.dll"]