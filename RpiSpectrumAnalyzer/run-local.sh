sudo apt update

#install git and GitHub CLI
sudo apt install git -y
sudo apt install gh -y

#install PulseAudio
#Install PulseAudio and associated BT modules. PulseAudio server should be running in order to be able to use this application.
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
pulseaudio --start #start PulseAudio server
pulseaudio --check; echo $? #returns 0 if PulseAudio is running

#install ALSA (Advanced Linux Sound Architecture) APIs, which is required by the Alsa.Net C# library
sudo apt install libasound2-dev -y

#install wget
sudo apt install wget

#install .NET 8
wget https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-arm64.tar.gz
sudo mkdir -p /usr/share/dotnet
sudo tar -xvf dotnet-sdk-8.0.100-linux-*.tar.gz -C /usr/share/dotnet
export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:/usr/share/dotnet
rm dotnet-sdk-8.0.100-linux-arm64.tar.*

#clone this code repo, build, and launch
git clone https://github.com/nejimonraveendran/Audio-Spectrum-Analyzer-Display.git
cd Audio-Spectrum-Analyzer-Display/RpiSpectrumAnalyzer
dotnet build
cd bin/Debug/net8.0
dotnet RpiSpectrumAnalyzer.dll
