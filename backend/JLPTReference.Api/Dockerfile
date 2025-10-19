# Use the official .NET 8 SDK image for development
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Set working directory
WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source code
COPY . ./

# Expose port 5000
EXPOSE 5000

# Set environment variables for development
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Start the application with hot-reload for development
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000", "--project", ".", "--verbosity", "normal"]
