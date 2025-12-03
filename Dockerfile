# Use the official .NET SDK image for development with hot reload
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

# Install dotnet-ef tools for migrations
# Retry logic for network issues
RUN for i in 1 2 3; do \
    dotnet tool install --global dotnet-ef --version 8.0.0 && break || \
    (echo "Attempt $i failed, retrying in $((i*5)) seconds..." && sleep $((i*5))); \
    done

# Add dotnet tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore dependencies
COPY ["MMGC.csproj", "./"]
# Retry restore with exponential backoff for network issues
RUN for i in 1 2 3; do \
    dotnet restore "MMGC.csproj" && break || \
    (echo "Restore attempt $i failed, retrying in $((i*5)) seconds..." && sleep $((i*5))); \
    done

# Copy everything else
COPY . .

# Expose port (will be overridden by docker-compose)
EXPOSE 8080

# Use dotnet watch for hot reload
# The command will be overridden by docker-compose for flexibility
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]