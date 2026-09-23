# Estágio Base com FFmpeg e Piper para Linux
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# TLS é terminado na borda (Render/proxy); o container serve apenas HTTP.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=

USER root
# 1. Instala o FFmpeg e ferramentas utilitárias
RUN apt-get update && apt-get install -y \
    ffmpeg \
    curl \
    ca-certificates \
    tar \
    libgomp1 \
    espeak-ng \
    && rm -rf /var/lib/apt/lists/*

# Baixar e extrair o Piper
ARG PIPER_VERSION=2023.11.14-2
RUN curl -L "https://github.com/rhasspy/piper/releases/download/${PIPER_VERSION}/piper_linux_x86_64.tar.gz" -o piper.tar.gz \
    && mkdir -p /opt/piper \
    && tar -xzf piper.tar.gz -C /opt/piper --strip-components=1 \
    && rm piper.tar.gz \
    && chmod +x /opt/piper/piper \
    && ln -s /opt/piper/piper /usr/local/bin/piper
ENV LD_LIBRARY_PATH="/opt/piper:${LD_LIBRARY_PATH}"

# Criar diretórios com permissão de escrita
RUN mkdir -p /app/storage /app/TTS/models /app/database && chmod -R 777 /app/storage /app/TTS /app/database

# Estágio de Build do .NET
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

# Estágio de Publicação
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AudiobookConverter.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Estágio Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copiar explicitamente os modelos ONNX
COPY AudiobookConverter.Api/TTS/models /app/TTS/models
ENTRYPOINT ["dotnet", "AudiobookConverter.Api.dll"]