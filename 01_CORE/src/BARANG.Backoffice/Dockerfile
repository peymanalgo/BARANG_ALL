FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SOURCE_COMMIT=unknown
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props BARANG.Core.slnx ./
COPY src/BARANG.Backoffice/BARANG.Backoffice.csproj src/BARANG.Backoffice/
COPY src/BARANG.Core.Application/BARANG.Core.Application.csproj src/BARANG.Core.Application/
COPY src/BARANG.Core.Domain/BARANG.Core.Domain.csproj src/BARANG.Core.Domain/
RUN dotnet restore src/BARANG.Backoffice/BARANG.Backoffice.csproj
COPY src/BARANG.Backoffice/ src/BARANG.Backoffice/
COPY src/BARANG.Core.Application/ src/BARANG.Core.Application/
COPY src/BARANG.Core.Domain/ src/BARANG.Core.Domain/
RUN dotnet publish src/BARANG.Backoffice/BARANG.Backoffice.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final
ARG SOURCE_COMMIT=unknown
RUN apt-get update && apt-get install --yes --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/.keys && chown app:app /app/.keys
USER app
ENV ASPNETCORE_URLS=http://+:5100 \
    BARANG_BUILD_COMMIT=$SOURCE_COMMIT
EXPOSE 5100
ENTRYPOINT ["sh", "-c", "echo BARANG Backoffice build commit ${BARANG_BUILD_COMMIT:-unknown}; exec dotnet BARANG.Backoffice.dll"]
