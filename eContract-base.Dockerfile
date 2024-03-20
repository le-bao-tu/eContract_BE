#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base

# Fix lỗi ký trên Linux
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

# Fix lỗi thiếu font trên linux

# MS Font
RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update; apt-get install -y ttf-mscorefonts-installer fontconfig

# RUN apt-get update; apt-get install -y fontconfig fonts-liberation
# RUN fc-cache -f -v

# RUN apt-get update && apt-get install -y libfontconfig1
RUN  ["rm", "-rf", "/etc/localtime"]
RUN  ["ln", "-s", "/usr/share/zoneinfo/Asia/Ho_Chi_Minh", "/etc/localtime"]
