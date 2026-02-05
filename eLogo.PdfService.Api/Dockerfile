FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8100

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Install dependencies for wkhtmltopdf
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends \
    wget \
    gdebi-core \
    libpango1.0-0 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libxcb1 \
    libfontconfig1 \
    libfreetype6 \
    zlib1g \
    libjpeg62-turbo \
    libpng16-16 \
    libssl3 \
    libgdiplus \
    fontconfig && \
    ln -s /usr/lib/libgdiplus.so /lib/x86_64-linux-gnu/libgdiplus.so

# Install wkhtmltopdf
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb && \
    gdebi --n wkhtmltox_0.12.6.1-2.bullseye_amd64.deb && \
    rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb && \
    ln -s /usr/local/lib/libwkhtmltox.so /usr/lib/libwkhtmltox.so

# Copy fonts
COPY --from=publish /src/Fonts /usr/share/fonts/
RUN fc-cache -f -v

# Create non-root user
RUN groupadd -g 1001 appuser && useradd -r -u 1001 -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "eLogo.PdfService.Api.dll"]
