# Use the official .NET SDK image for development with hot reload
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

# Install dotnet-ef tools for migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.0

# Add dotnet tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore dependencies
COPY ["MMGC.csproj", "./"]
RUN dotnet restore "MMGC.csproj"

# Copy everything else
COPY . .

# Expose port (will be overridden by docker-compose)
EXPOSE 8080

# Use dotnet watch for hot reload
# The command will be overridden by docker-compose for flexibility
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

