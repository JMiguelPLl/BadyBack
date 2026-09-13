# =======================================================
# 1. ETAPA DE COMPILACIÓN (SDK de .NET 9)
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["BadyBackend/BadyBackend.csproj", "BadyBackend/"]
RUN dotnet restore "BadyBackend/BadyBackend.csproj"

# Copiar todo el código fuente y compilar en modo Release
COPY . .
WORKDIR "/src/BadyBackend"
RUN dotnet publish "BadyBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =======================================================
# 2. ETAPA DE EJECUCIÓN (Runtime ligero de ASP.NET Core)
# =======================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Crear directorio de almacenamiento físico de imágenes de productos
RUN mkdir -p wwwroot/imagenes

# Copiar archivos compilados desde la etapa de build
COPY --from=build /app/publish .

# Render asigna el puerto mediante la variable de entorno PORT
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BadyBackend.dll"]
