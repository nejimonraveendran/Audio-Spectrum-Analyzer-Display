using Alsa.Net;

namespace RpiSpectrumAnalyzer;

static class AudioCapture
{
    /// <summary>
    /// Starts capturing system audio and a callback is made to the caller every time a new sample (byte array buffer) is available 
    /// </summary>
    /// <param name="captureParams">Specifies basic capture parameters</param>
    /// <param name="cts">Cancellation token to be passed so that the action can be cancelled</param>
    /// <param name="callback">Callback function to receive the captured sample (byte array buffer)</param>
    public static void StartCapture(Action<byte[]> callback, CaptureParams captureParams, CancellationTokenSource cts){
        Task.Factory.StartNew(() => {

            var settings = new SoundDeviceSettings
            {
                RecordingSampleRate = captureParams.SampleRate,
                RecordingBitsPerSample = captureParams.BitsPerSample,
                RecordingChannels = captureParams.Channels,
            };
            
            using (var alsaDevice = AlsaDeviceBuilder.Create(settings))
            {
                alsaDevice.Record((buffer) => {
                    if(buffer.Length < 4096) return; //Alsa.Net tries to write Wav Header, which is not required in our case. 
                    callback.Invoke(buffer);
                }, cts.Token);
            }

        }, cts.Token);
    }
}


