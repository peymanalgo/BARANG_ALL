FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props BARANG.Core.slnx ./
COPY src/BARANG.Core.Api/BARANG.Core.Api.csproj src/BARANG.Core.Api/
COPY src/BARANG.Core.Application/BARANG.Core.Application.csproj src/BARANG.Core.Application/
COPY src/BARANG.Core.Domain/BARANG.Core.Domain.csproj src/BARANG.Core.Domain/
COPY src/BARANG.Core.Infrastructure/BARANG.Core.Infrastructure.csproj src/BARANG.Core.Infrastructure/
RUN dotnet restore src/BARANG.Core.Api/BARANG.Core.Api.csproj

COPY src/ src/
RUN dotnet publish src/BARANG.Core.Api/BARANG.Core.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/App_Data/locks && chown -R app:app /app/App_Data
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "BARANG.Core.Api.dll"]
