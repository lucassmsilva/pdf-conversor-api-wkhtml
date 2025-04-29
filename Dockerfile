# --- Stage 1: Base Runtime with Dependencies ---
    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

    # Configuração para evitar prompts interativos durante a instalação
    ENV DEBIAN_FRONTEND=noninteractive
    
    # Instalar dependências necessárias
    RUN apt-get update && apt-get install -y --no-install-recommends \
        libgdiplus \
        libc6-dev \
        wget \
        fontconfig \
        libfreetype6 \
        libpng16-16 \
        libx11-6 \
        libxcb1 \
        libxext6 \
        libxrender1 \
        xfonts-75dpi \
        xfonts-base \
        zlib1g \
        ca-certificates \
        gnupg \
        software-properties-common \
        unzip \
        curl \
        && rm -rf /var/lib/apt/lists/*
    
    # Adicionar repositório contrib para fontes Microsoft
    RUN echo "deb http://deb.debian.org/debian/ bookworm main contrib" > /etc/apt/sources.list.d/debian-contrib.list
    
    # Configurar aceitação automática da licença de fontes Microsoft
    RUN echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections
    
    # Atualizar e instalar fontes
    RUN apt-get update && apt-get install -y --no-install-recommends \
        # Fontes Microsoft
        ttf-mscorefonts-installer \
        # Fontes Google Noto (cobertura ampla de idiomas)
        fonts-noto \
        fonts-noto-cjk \
        fonts-noto-color-emoji \
        fonts-noto-core \
        fonts-noto-mono \
        # Fontes de compatibilidade com o Chrome
        fonts-liberation \
        # Fontes adicionais comuns
        fonts-dejavu \
        fonts-freefont-ttf \
        fonts-open-sans \
        fonts-roboto \
        && apt-get clean \
        && rm -rf /var/lib/apt/lists/*
    
    # Atualizar cache de fontes
    RUN fc-cache -f -v
    
    # Download e instalação do wkhtmltopdf com dependências para bookworm (Debian 12)
    RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
        && dpkg -i wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
        && rm wkhtmltox_0.12.6.1-3.bookworm_amd64.deb
    
    # Criar links simbólicos para as bibliotecas
    RUN ln -s /usr/lib/libgdiplus.so /lib/x86_64-linux-gnu/libgdiplus.so \
        && ln -s /usr/local/lib/libwkhtmltox.so /usr/lib/libwkhtmltox.so
    
    # Definir diretório de trabalho
    WORKDIR /app
    
    # Expor a porta
    EXPOSE 80
    ENV ASPNETCORE_URLS=http://+:80
    
    # --- Stage 2: Build ---
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    ARG configuration=Release
    WORKDIR /src
    COPY ["pdf-conversor-api.csproj", "./"]
    RUN dotnet restore "pdf-conversor-api.csproj"
    COPY . .
    WORKDIR "/src/."
    RUN dotnet build "pdf-conversor-api.csproj" -c $configuration -o /app/build
    
    # --- Stage 3: Publish ---
    FROM build AS publish
    ARG configuration=Release
    RUN dotnet publish "pdf-conversor-api.csproj" -c $configuration -o /app/publish /p:SelfContained=false /p:UseAppHost=false /p:PublishAot=false
    
    # --- Stage 4: Final ---
    FROM base AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    
    # Definir o ponto de entrada
    ENTRYPOINT ["dotnet", "pdf-conversor-api.dll"]