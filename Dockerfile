# syntax=docker/dockerfile:1

# One image for every environment. No credentials are built in:
# Entra IDs and other settings arrive as environment variables at run time.

# ---- Build the React app -------------------------------------------------
FROM node:22-alpine AS web-build
WORKDIR /web

# Package files first, so the install layer is reused until a dependency changes.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./

# The API base path is the same everywhere because the app and the API share one origin.
# The web app has no mock mode: it always signs in and always calls the API.
ENV VITE_API_BASE_URL=/api/v1
RUN npm run build

# ---- Build the API -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

# Project files first, so the restore layer is reused until a dependency changes.
COPY backend/Directory.Build.props backend/Directory.Packages.props ./
COPY backend/src/Vas.Api/Vas.Api.csproj src/Vas.Api/
COPY backend/src/Vas.Application/Vas.Application.csproj src/Vas.Application/
COPY backend/src/Vas.Domain/Vas.Domain.csproj src/Vas.Domain/
COPY backend/src/Vas.Infrastructure/Vas.Infrastructure.csproj src/Vas.Infrastructure/
COPY backend/src/Vas.Shared/Vas.Shared.csproj src/Vas.Shared/
RUN dotnet restore src/Vas.Api/Vas.Api.csproj

COPY backend/src/ src/
RUN dotnet publish src/Vas.Api/Vas.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ---- Runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# App Service terminates TLS and forwards the original scheme and client address.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=api-build /app/publish ./
COPY --from=web-build /web/dist ./wwwroot

USER $APP_UID
ENTRYPOINT ["dotnet", "Vas.Api.dll"]
