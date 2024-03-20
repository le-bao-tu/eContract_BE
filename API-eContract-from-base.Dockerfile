#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM savisdockerhub/base-econtract AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["NetCore.API/NetCore.API.csproj", "NetCore.API/"]
COPY ["NetCore.Business/NetCore.Business.csproj", "NetCore.Business/"]
COPY ["NetCore.DataLog/NetCore.DataLog.csproj", "NetCore.DataLog/"]
COPY ["NetCore.Data/NetCore.Data.csproj", "NetCore.Data/"]
COPY ["NetCore.Shared/NetCore.Shared.csproj", "NetCore.Shared/"]
RUN dotnet restore "NetCore.API/NetCore.API.csproj"
COPY . .
WORKDIR "/src/NetCore.API"
# RUN dotnet build "NetCore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCore.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


#Licence Sprie.PDF
COPY ./Spire.License.dll .

ENV TZ=Asia/Ho_Chi_Minh
ENTRYPOINT ["dotnet", "NetCore.API.dll"]