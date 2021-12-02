FROM mcr.microsoft.com/dotnet/sdk:6.0 as build_debug

COPY . /app
WORKDIR /app
RUN dotnet publish -c Debug

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as debug

WORKDIR /app

COPY --from=build_debug /app/bin/Debug/netcoreapp6.0/publish /app
COPY --from=build_debug /app/Templates /app/Templates

EXPOSE 9966

CMD ["dotnet", "MindFlavor.SQLServerExporter.dll", "-c", "/config/config.json"]



FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

COPY . /app
WORKDIR /app
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as release

WORKDIR /app

COPY --from=build /app/bin/Release/netcoreapp6.0/publish /app
COPY --from=build /app/Templates /app/Templates

EXPOSE 9966

CMD ["dotnet", "MindFlavor.SQLServerExporter.dll", "-c", "/config/config.json"]


