#install Docker:
sudo apt update && sudo apt upgrade -y
sudo apt install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo tee /etc/apt/keyrings/docker.asc > /dev/null
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io
sudo apt-get install docker-buildx-plugin docker-compose-plugin -y
sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker $USER
newgrp docker

#Install PulseAudio and associated BT modules. PulseAudio server should be running in order to be able to use this application.
sudo apt update
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
pulseaudio --start #start PulseAudio server
pulseaudio --check; echo $? #returns 0 if PulseAudio is running

#install ALSA (Advanced Linux Sound Architecture) APIs, which is required by the Alsa.Net C# library
sudo apt install libasound2-dev -y

#run docker container (basic usage)
docker run --rm -it -p 8090:8090 \
  --privileged \
  --user $(id -u):$(id -g) \
  --group-add $(getent group spi | cut -d: -f3) \
  --env PULSE_SERVER=unix:${XDG_RUNTIME_DIR}/pulse/native \
  --env PULSE_COOKIE=/tmp/pulseaudio.cookie \
  --volume ${XDG_RUNTIME_DIR}/pulse/native:${XDG_RUNTIME_DIR}/pulse/native \
  --device /dev/snd \
  nejimonraveendran/rpispectrumanalyzer:v1