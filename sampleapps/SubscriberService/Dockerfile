FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["sampleapps/SubscriberService/SubscriberService.csproj", "sampleapps/SubscriberService/"]
COPY ["src/AWS.Messaging/AWS.Messaging.csproj", "src/AWS.Messaging/"]
RUN dotnet restore "sampleapps/SubscriberService/SubscriberService.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet build "sampleapps/SubscriberService/SubscriberService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sampleapps/SubscriberService/SubscriberService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SubscriberService.dll"]
