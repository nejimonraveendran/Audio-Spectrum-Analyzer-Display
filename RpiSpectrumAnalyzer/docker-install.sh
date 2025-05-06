sudo apt update

#install PulseAudio.  PulseAudio server should be running in order to be able to use this application.
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
pulseaudio --start
pulseaudio --check; echo $?

#install ALSA (Advanced Linux Sound Architecture) APIs, which is required by the Alsa.Net C# library
sudo apt install libasound2-dev -y

#run docker container
docker run --rm -it -p 8090:8090 \
  --privileged \
  --user $(id -u):$(id -g) \
  --group-add $(getent group spi | cut -d: -f3) \
  --env PULSE_SERVER=unix:${XDG_RUNTIME_DIR}/pulse/native \
  --env PULSE_COOKIE=/tmp/pulseaudio.cookie \
  --volume ${XDG_RUNTIME_DIR}/pulse/native:${XDG_RUNTIME_DIR}/pulse/native \
  --device /dev/snd \
  nejimonraveendran/rpispectrumanalyzer:v1 --disable-led-display