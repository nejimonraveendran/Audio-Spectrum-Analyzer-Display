using Alsa.Net;
using Alsa.Net.Internal;

namespace RpiSpectrumAnalyzer;

static class AudioCapture
{
    /// <summary>
    /// Starts capturing system audio and a callback is made to the caller every time a new sample (byte array buffer) is available 
    /// </summary>
    /// <param name="captureParams">Specifies basic capture parameters</param>
    /// <param name="cts">Cancellation token to be passed so that the action can be cancelled</param>
    /// <param name="callback">Callback function to receive the captured sample (byte array buffer)</param>
    public static void StartCapture(Action<CaptureResult> callback, uint sampleRate, CancellationTokenSource cts)
    {
        Task.Factory.StartNew(() => {

            var settings = new SoundDeviceSettings
            {
                RecordingSampleRate = sampleRate,
                RecordingBitsPerSample = 16, //16 bits per sample
                RecordingChannels = 1 //just need one channel for our use case
            };
            
            try
            {
                using (var alsaDevice = AlsaDeviceBuilder.Create(settings))
                {
                    alsaDevice.Record((buffer) => {
                        try
                        {
                            if(buffer.Length < 4096) return; //Alsa.Net tries to write Wav Header, which is not required in our case. 
                            callback.Invoke(new CaptureResult{ Buffer = buffer, Exception = null});                             
                        }
                        catch (Exception ex)
                        {
                            callback.Invoke(new CaptureResult{ Buffer = null, Exception = ex});
                        }
                        
                    }, cts.Token);
                }                                
            }
            catch (Exception ex)
            {
                callback.Invoke(new CaptureResult{ Buffer = null, Exception = ex});
            }

        }, cts.Token);

    }
}


