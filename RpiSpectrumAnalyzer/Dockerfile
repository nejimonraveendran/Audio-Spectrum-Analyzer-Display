FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /tmp/src
COPY . .
RUN dotnet publish -c Release -r linux-arm64 -o ./publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /tmp/src/publish .
RUN apt update -y
RUN apt install pulseaudio -y
RUN apt install libasound2-dev -y

ENTRYPOINT ["dotnet", "RpiSpectrumAnalyzer.dll"]

# CMD ["bash"]

# usage:
# docker run --rm -it -p 8090:8090 \
#   --privileged \
#   --user $(id -u):$(id -g) \
#   --group-add $(getent group spi | cut -d: -f3) \
#   --env PULSE_SERVER=unix:${XDG_RUNTIME_DIR}/pulse/native \
#   --env PULSE_COOKIE=/tmp/pulseaudio.cookie \
#   --volume ${XDG_RUNTIME_DIR}/pulse/native:${XDG_RUNTIME_DIR}/pulse/native \
#   --device /dev/snd \
#   rpispectrumanalyzer:v1