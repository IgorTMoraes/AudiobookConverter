# Esta fase é usada como base de execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Define a variável de ambiente para o Kestrel escutar na porta 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Instala o FFmpeg e dependências necessárias para áudio e downloads
USER root
RUN apt-get update && apt-get install -y \
    ffmpeg \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Cria os diretórios necessários com permissão de escrita
RUN mkdir -p /app/storage /app/TTS/models /app/database && chmod -R 777 /app/storage /app/TTS /app/database

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AudiobookConverter.Api/AudiobookConverter.Api.csproj", "AudiobookConverter.Api/"]
COPY ["AudiobookConverter.Application/AudiobookConverter.Application.csproj", "AudiobookConverter.Application/"]
COPY ["AudiobookConverter.Domain/AudiobookConverter.Domain.csproj", "AudiobookConverter.Domain/"]
COPY ["AudiobookConverter.Infrastructure/AudiobookConverter.Infrastructure.csproj", "AudiobookConverter.Infrastructure/"]
RUN dotnet restore "./AudiobookConverter.Api/AudiobookConverter.Api.csproj"
COPY . .
WORKDIR "/src/AudiobookConverter.Api"
RUN dotnet build "./AudiobookConverter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AudiobookConverter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é a final, usada em produção ou no VS em modo normal
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AudiobookConverter.Api.dll"]