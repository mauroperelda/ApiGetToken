# Dockerfile para Render
# Build context: directorio ApiGetToken/ (configurado como "Root Directory" en Render)
# Multi-stage build: SDK para compilar, ASP.NET runtime para ejecutar (imagen final liviana)

# ── Stage 1: build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero el .csproj para que la restore quede en una layer cacheada.
# Si después cambia solo código (no dependencias), no re-restauramos.
COPY ["ApiGetToken.csproj", "./"]
RUN dotnet restore "ApiGetToken.csproj"

# Ahora el resto del código fuente
COPY . .

# Publicamos en modo Release al directorio /app/publish
# /p:UseAppHost=false evita generar un ejecutable nativo (no se necesita en Linux)
RUN dotnet publish "ApiGetToken.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render inyecta la variable $PORT en runtime (default 10000).
# El shell form ("sh -c") permite expandir la variable de entorno al arranque.
# Si $PORT no estuviera seteada, cae al default 10000.
ENTRYPOINT ["sh", "-c", "dotnet ApiGetToken.dll --urls=http://0.0.0.0:${PORT:-10000}"]
