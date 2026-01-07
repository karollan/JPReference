# Base stage for shared configuration
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# SDK stage for building and development
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk
WORKDIR /src
COPY ["JLPTReference.Api.csproj", "./"]
RUN dotnet restore "./JLPTReference.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "JLPTReference.Api.csproj" -c Release -o /app/build

# Development stage (used by dev docker-compose)
FROM sdk AS development
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000", "--project", ".", "--verbosity", "normal"]

# Publish stage
FROM sdk AS publish
RUN dotnet publish "JLPTReference.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Production stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "JLPTReference.Api.dll"]
