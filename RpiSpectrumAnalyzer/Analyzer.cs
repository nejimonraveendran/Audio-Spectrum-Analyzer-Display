using System.Numerics;

namespace RpiSpectrumAnalyzer;

class Analyzer
{
    
    const double _16bitMaxVal = 32768.0f; //decimal represenation of the max value 16 bit can hold.  Need this later in the code to normalize the raw buffer values.
    private readonly AnalyzerParams _analyzerParams;
    private double[] _freqBandsOld;

    public Analyzer(AnalyzerParams analyzerParams)
    {
        _analyzerParams = analyzerParams;
        _freqBandsOld = new double[_analyzerParams.Bands.Length];
    }

    /// <summary>
    /// Converts raw samples to frequency bands in terms of their magnitudes and additionally smoothens the transitions out by a speed filter (useful for display purposes) 
    /// </summary>
    /// <param name="buffer">samples buffer array</param>
    /// <param name="speedFilter">speed filter</param>
    /// <returns></returns>
    public BandInfo[] ConvertToFrequencyBands(byte[] buffer, double speedFilter)
    {
        var freqBands = ConvertToFrequencyBands(buffer);
        
        // Smooth out the transitions
        var freqBandsNew = new double[freqBands.Length];
        for (int i = 0; i < freqBands.Length; i++)
        {
            freqBandsNew[i] = freqBands[i].Magnitude;

            if (freqBandsNew[i] < _freqBandsOld[i])
            {
                freqBands[i].Magnitude = Math.Max(_freqBandsOld[i] - speedFilter, freqBandsNew[i]);
            }
            else if (freqBandsNew[i] > _freqBandsOld[i])
            {
                freqBands[i].Magnitude = freqBandsNew[i];
            }

            _freqBandsOld[i] = freqBands[i].Magnitude;
        }

        return freqBands;
    }

    /// <summary>
    /// Converts raw samples to frequency bands in terms of their magnitudes 
    /// </summary>
    /// <param name="buffer">samples buffer array</param>
    /// <returns></returns>
    public BandInfo[] ConvertToFrequencyBands(byte[] buffer)
    {
        int numSamples = buffer.Length / 2; //i.e., buffer size / (bits per sample / 8) / number of channels. Buffer size is usually 4096, so numSamples = 2048 
        double[] samples = new double[numSamples];  

        //in PCM 16-bit each sample is represented using 2 bytes (16 bits), so we need to convert every 2 bytes to a 16-bit whole number (data type short). 
        //Iterate from 0 to half of the buffer size, convert every 2 bytes to a proper sample (short) and store it in samples array.
        for (int i = 0; i < numSamples; i++)
        {
            short sample = BitConverter.ToInt16(buffer, i * 2);  // Convert 16-bit PCM to float (-1 to 1)
            double normalizedSample = sample / _16bitMaxVal; //normalize it to the range -1 to 1 floating point by dividing the 2-byte (16-bit) value by 16-bit max (32768)
            samples[i] = normalizedSample; //store in the array
        }

        //do FFT operations on the samples
        var window = new FftSharp.Windows.Hanning();
        window.ApplyInPlace(samples, true);
        Complex[] spectrum = FftSharp.FFT.Forward(samples);
        double[] magnitudes = FftSharp.FFT.Magnitude(spectrum);
        double[] scale = FftSharp.FFT.FrequencyScale(magnitudes.Length, _analyzerParams.SampleRate);

        //go through the bands and check if the input frequencies fall into the range specified in the band table.  If found, add up the magnitudes.
        var freqBands = new BandInfo[_analyzerParams.Bands.Length];

        for (int i = 0; i < magnitudes.Length; i++)
        {
            double freq = scale[i];  //internal logic: i * (_sampleRate / magnitude.Length) / 2

            // Add to the appropriate frequency bands
            for (int b = 0; b < _analyzerParams.Bands.Length; b++)
            {
                int startFreq = b == 0 ? 0 : _analyzerParams.Bands[b-1];
                int endFreq = _analyzerParams.Bands[b];

                if(freq > startFreq && freq <= endFreq){
                    freqBands[b].Band = _analyzerParams.Bands[b];
                    freqBands[b].Magnitude += magnitudes[i];
                }
            }
        }

        return freqBands;
    }

}