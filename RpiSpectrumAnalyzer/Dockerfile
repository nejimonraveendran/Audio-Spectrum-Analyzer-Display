FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt update
RUN apt install mpg123 -y

WORKDIR /tmp/src
COPY . .

RUN dotnet build

WORKDIR /tmp/src/bin/Debug/net8.0
COPY song.mp3 .

CMD ["bash"]

#ENTRYPOINT ["dotnet", "RpiSpectrumAnalyzer.dll", "--disable-led-display"]
