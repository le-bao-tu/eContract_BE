#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM savisdockerhub/base-econtract AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["NetCore.TestConsole/NetCore.TestConsole.csproj", "NetCore.TestConsole/"]
COPY ["NetCore.Business/NetCore.Business.csproj", "NetCore.Business/"]
COPY ["NetCore.DataLog/NetCore.DataLog.csproj", "NetCore.DataLog/"]
COPY ["NetCore.Data/NetCore.Data.csproj", "NetCore.Data/"]
COPY ["NetCore.Shared/NetCore.Shared.csproj", "NetCore.Shared/"]
COPY ["NetCore.WorkerService/NetCore.WorkerService.csproj", "NetCore.WorkerService/"]
RUN dotnet restore "NetCore.TestConsole/NetCore.TestConsole.csproj"
COPY . .
WORKDIR "/src/NetCore.TestConsole"
# RUN dotnet build "NetCore.TestConsole.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCore.TestConsole.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


#Licence Sprie.PDF
COPY ./Spire.License.dll .

ENV TZ=Asia/Ho_Chi_Minh
ENTRYPOINT ["dotnet", "NetCore.TestConsole.dll"]