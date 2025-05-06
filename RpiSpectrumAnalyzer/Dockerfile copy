FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt update -y
RUN apt install alsa-utils -y
RUN apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y
RUN apt install libasound2-dev -y

WORKDIR /tmp/src
COPY . .

RUN dotnet build

WORKDIR /tmp/src/bin/Debug/net8.0

ENTRYPOINT ["dotnet", "RpiSpectrumAnalyzer.dll"]

# CMD ["bash"]

# docker run --rm -it -p 8090:8090 \
#   --privileged \
#   --user $(id -u):$(id -g) \
#   --group-add $(getent group spi | cut -d: -f3) \
#   --env PULSE_SERVER=unix:${XDG_RUNTIME_DIR}/pulse/native \
#   --env PULSE_COOKIE=/tmp/pulseaudio.cookie \
#   --volume ${XDG_RUNTIME_DIR}/pulse/native:${XDG_RUNTIME_DIR}/pulse/native \
#   --device /dev/snd \
#   rpispectrumanalyzer:v1