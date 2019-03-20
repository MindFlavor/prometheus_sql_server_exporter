FROM mcr.microsoft.com/dotnet/core/aspnet:2.2

WORKDIR /app

COPY bin/Debug/netcoreapp2.2/publish /app
COPY Templates /app/Templates

EXPOSE 9966

CMD ["dotnet", "MindFlavor.SQLServerExporter.dll", "-c", "/config/config.json"]


