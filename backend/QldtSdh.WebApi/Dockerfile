# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["QldtSdh.WebApi/QldtSdh.WebApi.csproj", "QldtSdh.WebApi/"]
COPY ["QldtSdh.Data/QldtSdh.Data.csproj", "QldtSdh.Data/"]
COPY ["QldtSdh.Shared/QldtSdh.Shared.csproj", "QldtSdh.Shared/"]
RUN dotnet restore "QldtSdh.WebApi/QldtSdh.WebApi.csproj"

# Copy all source files and build
COPY . .
WORKDIR "/src/QldtSdh.WebApi"
RUN dotnet build "QldtSdh.WebApi.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "QldtSdh.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QldtSdh.WebApi.dll"]
