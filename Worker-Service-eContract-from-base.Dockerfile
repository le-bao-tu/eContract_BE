#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

# Fix lỗi ký trên Linux
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

# Fix lỗi thiếu font trên linux

# MS Font
#RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
#RUN apt-get update; apt-get install -y ttf-mscorefonts-installer fontconfig

RUN apt-get update; apt-get install -y fontconfig fonts-liberation
RUN fc-cache -f -v

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["NetCore.WorkerService/NetCore.WorkerService.csproj", "NetCore.WorkerService/"]
COPY ["NetCore.Business/NetCore.Business.csproj", "NetCore.Business/"]
COPY ["NetCore.DataLog/NetCore.DataLog.csproj", "NetCore.DataLog/"]
COPY ["NetCore.Data/NetCore.Data.csproj", "NetCore.Data/"]
COPY ["NetCore.Shared/NetCore.Shared.csproj", "NetCore.Shared/"]
RUN dotnet restore "NetCore.WorkerService/NetCore.WorkerService.csproj"
COPY . .
WORKDIR "/src/NetCore.WorkerService"
RUN dotnet build "NetCore.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCore.WorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


#Licence Sprie.PDF
COPY ./Spire.License.dll .

ENV TZ=Asia/Ho_Chi_Minh
ENTRYPOINT ["dotnet", "NetCore.WorkerService.dll"]