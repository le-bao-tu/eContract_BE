#region BuildAPI
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /opt/core.api/src

# Copy file to image
COPY NetCore.TestConsole/NetCore.TestConsole.csproj NetCore.TestConsole/
COPY NetCore.Business/NetCore.Business.csproj NetCore.Business/
COPY NetCore.Data/NetCore.Data.csproj NetCore.Data/
COPY NetCore.DataLog/NetCore.DataLog.csproj NetCore.DataLog/
COPY NetCore.Shared/NetCore.Shared.csproj NetCore.Shared/
COPY NetCore.WorkerService/NetCore.WorkerService.csproj NetCore.WorkerService/

# Run restore dependency
RUN dotnet restore NetCore.TestConsole/NetCore.TestConsole.csproj

# Copy file all file to dir
COPY NetCore.TestConsole NetCore.TestConsole
COPY NetCore.Business NetCore.Business
COPY NetCore.Data NetCore.Data
COPY NetCore.DataLog NetCore.DataLog
COPY NetCore.Shared NetCore.Shared
COPY NetCore.WorkerService NetCore.WorkerService

#Replace appsettings.json with appsettings 
#COPY NetCore.API/appsettings.Testing.json NetCore.API/appsettings.json

#region test
# Set work dir
WORKDIR /opt/core.api/src/NetCore.TestConsole

# Build image 
RUN dotnet build NetCore.TestConsole.csproj -c Release -o /opt/core.api/build
# Publish image
RUN dotnet publish NetCore.TestConsole.csproj -c Release -o /opt/core.api/publish

#endregion 

# Final Image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS final
WORKDIR /opt/core.api/publish

COPY --from=build /opt/core.api/publish .


#Licence Sprie.PDF
COPY ./Spire.License.dll .

# # Add docker-entrypoint.sh
# ADD ./docker-entrypoint.sh ./
# RUN chmod +x ./docker-entrypoint.sh 

# ENTRYPOINT  ["./docker-entrypoint.sh"]

#RUN apt-get update; apt-get install -y fontconfig fonts-liberation
#RUN fc-cache -f -v

# MS Font
# RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
# RUN apt-get update; apt-get install -y ttf-mscorefonts-installer fontconfig

#Add these two lines for fonts-liberation instead
RUN apt-get update; apt-get install -y fontconfig fonts-liberation
RUN fc-cache -f -v

RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

# RUN apt-get update && apt-get install -y libfontconfig1
RUN  ["rm", "-rf", "/etc/localtime"]
RUN  ["ln", "-s", "/usr/share/zoneinfo/Asia/Ho_Chi_Minh", "/etc/localtime"]
EXPOSE 80
EXPOSE 587/tcp
CMD ["dotnet","NetCore.TestConsole.dll"]