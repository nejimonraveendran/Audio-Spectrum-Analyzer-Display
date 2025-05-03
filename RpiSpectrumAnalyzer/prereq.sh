sudo apt update

#install git
sudo apt install git -y
sudo apt install gh -y

#install wget
sudo apt install wget

#install pulse audio
#PulseAudio server should be running in order to be able to connect devices via Bluetooth.  Devices will disconnet if you exit PulseAudio Server
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
sudo systemctl restart bluetooth
pulseaudio --start
pulseaudio --check; echo $?

#install ALSA (Advanced Linux Sound Architecture) APIs, which is required by the Alsa.Net C# library
sudo apt install libasound2-dev -y

#install .NET 8
wget https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-arm64.tar.gz
sudo mkdir -p /usr/share/dotnet
sudo tar -xvf dotnet-sdk-8.0.100-linux-*.tar.gz -C /usr/share/dotnet
echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:/usr/share/dotnet' >> ~/.bashrc
source ~/.bashrc
rm dotnet-sdk-8.0.100-linux-arm64.tar.gz

#clone this code repo
git clone https://github.com/nejimonraveendran/Audio-Spectrum-Analyzer-Display.git
cd Audio-Spectrum-Analyzer-Display/RpiSpectrumAnalyzer
dotnet build
cd /bin/Debug/net8.0
