FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/BARANG.Backoffice/BARANG.Backoffice.csproj src/BARANG.Backoffice/
RUN dotnet restore src/BARANG.Backoffice/BARANG.Backoffice.csproj
COPY src/BARANG.Backoffice/ src/BARANG.Backoffice/
RUN dotnet publish src/BARANG.Backoffice/BARANG.Backoffice.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final
RUN apt-get update && apt-get install --yes --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/.keys && chown app:app /app/.keys
USER app
ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100
ENTRYPOINT ["dotnet", "BARANG.Backoffice.dll"]
