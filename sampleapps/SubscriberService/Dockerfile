FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["src/AWS.Messaging/AWS.Messaging.csproj", "src/AWS.Messaging/"]
COPY ["test/AWS.Messaging.UnitTests/AWS.Messaging.UnitTests.csproj", "test/AWS.Messaging.UnitTests/"]
COPY ["test/AWS.Messaging.IntegrationTests/AWS.Messaging.IntegrationTests.csproj", "test/AWS.Messaging.IntegrationTests/"]
COPY ["sampleapps/PublisherAPI/PublisherAPI.csproj", "sampleapps/PublisherAPI/"]
COPY ["sampleapps/SubscriberService/SubscriberService.csproj", "sampleapps/SubscriberService/"]
COPY ["test/AWS.Messaging.Tests.Common/AWS.Messaging.Tests.Common.csproj", "test/AWS.Messaging.Tests.Common/"]
COPY ["src/AWS.Messaging.Lambda/AWS.Messaging.Lambda.csproj", "src/AWS.Messaging.Lambda/"]
COPY ["sampleapps/LambdaMessaging/LambdaMessaging.csproj", "sampleapps/LambdaMessaging/"]
COPY ["src/AWS.Messaging.Telemetry.OpenTelemetry/AWS.Messaging.Telemetry.OpenTelemetry.csproj", "src/AWS.Messaging.Telemetry.OpenTelemetry/"]
COPY ["test/AWS.Messaging.Tests.LambdaFunctions/AWS.Messaging.Tests.LambdaFunctions.csproj", "test/AWS.Messaging.Tests.LambdaFunctions/"]
COPY ["test/AWS.Messaging.Benchmarks/AWS.Messaging.Benchmarks.csproj", "test/AWS.Messaging.Benchmarks/"]
COPY ["sampleapps/PollyIntegration/PollyIntegration.csproj", "sampleapps/PollyIntegration/"]
COPY ["sampleapps/AppHost/AppHost.csproj", "sampleapps/AppHost/"]
COPY ["sampleapps/InvokeLambda/src/InvokeLambda/InvokeLambda.csproj", "sampleapps/InvokeLambda/src/InvokeLambda/"]
RUN dotnet restore "sampleapps/SubscriberService/SubscriberService.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/sampleapps/SubscriberService"
RUN dotnet build "SubscriberService.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
RUN apt-get update -yq \
    && apt-get install -yq ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update -yq \
    && apt-get install nodejs -yq
RUN dotnet publish "SubscriberService.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubscriberService.dll"]
