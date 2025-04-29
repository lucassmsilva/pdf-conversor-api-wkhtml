# --- Stage 1: Base Runtime with Dependencies ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install required dependencies for DinkToPdf
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
    && rm -rf /var/lib/apt/lists/*

# Download and install wkhtmltopdf with dependencies for bookworm (Debian 12)
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
    && dpkg -i wkhtmltox_0.12.6.1-3.bookworm_amd64.deb \
    && rm wkhtmltox_0.12.6.1-3.bookworm_amd64.deb

# Create symbolic links for the libraries
RUN ln -s /usr/lib/libgdiplus.so /lib/x86_64-linux-gnu/libgdiplus.so \
    && ln -s /usr/local/lib/libwkhtmltox.so /usr/lib/libwkhtmltox.so

WORKDIR /app
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
RUN dotnet publish "pdf-conversor-api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

# --- Stage 4: Final ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy native libraries to the app directory
COPY ["libwkhtmltox.dll", "./"] 
COPY ["libwkhtmltox.so", "./"]
COPY ["libwkhtmltox.dylib", "./"]
ENTRYPOINT ["dotnet", "pdf-conversor-api.dll"]