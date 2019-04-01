FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as build_debug

COPY . /app
WORKDIR /app
RUN dotnet publish -c Debug

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 as debug

WORKDIR /app

COPY --from=build_debug /app/bin/Debug/netcoreapp2.2/publish /app
COPY --from=build_debug /app/Templates /app/Templates

EXPOSE 9966

CMD ["dotnet", "MindFlavor.SQLServerExporter.dll", "-c", "/config/config.json"]

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as build_release

COPY . /app
WORKDIR /app
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 as release

WORKDIR /app

COPY --from=build_release /app/bin/Release/netcoreapp2.2/publish /app
COPY --from=build_release /app/Templates /app/Templates

EXPOSE 9966

CMD ["dotnet", "MindFlavor.SQLServerExporter.dll", "-c", "/config/config.json"]


