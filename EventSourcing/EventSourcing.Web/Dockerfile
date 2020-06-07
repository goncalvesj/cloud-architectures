#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ENV APT_KEY_DONT_WARN_ON_DANGEROUS_USAGE=1
RUN apt-get update -yq 
RUN apt-get install curl gnupg -yq 
RUN curl -sL https://deb.nodesource.com/setup_13.x | bash -
RUN apt-get install -y nodejs

WORKDIR /src
COPY [".", "EventSourcing/"]
RUN dotnet restore "EventSourcing/EventSourcing.Web/EventSourcing.Web.csproj"
COPY . .
WORKDIR "/src/EventSourcing"
RUN dotnet build "EventSourcing.Web/EventSourcing.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventSourcing.Web/EventSourcing.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventSourcing.Web.dll"]