FROM mcr.microsoft.com/dotnet/sdk:3.1 AS base

COPY . /usr/src/app
WORKDIR /usr/src/app

RUN dotnet restore
RUN dotnet build

ENTRYPOINT sh -c 'echo $ENVIRONMENT | grep -i dev >/dev/null; [ $? -gt 0 ] && dotnet run || dotnet watch run'
