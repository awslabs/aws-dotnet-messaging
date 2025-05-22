FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["sampleapps/PublisherAPI/PublisherAPI.csproj", "sampleapps/PublisherAPI/"]
COPY ["src/AWS.Messaging/AWS.Messaging.csproj", "src/AWS.Messaging/"]
RUN dotnet restore "sampleapps/PublisherAPI/PublisherAPI.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet build "sampleapps/PublisherAPI/PublisherAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sampleapps/PublisherAPI/PublisherAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "PublisherAPI.dll"]
