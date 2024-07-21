using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DpMapSubscribeTool.Desktop.Utils.MethodExtensions;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DpMapSubscribeTool.Desktop.Utils;
//part of code is from https://markheath.net/post/fire-and-forget-audio-playback-with

public class SimpleSoundPlayer : IDisposable
{
    private readonly ILogger<SimpleSoundPlayer> logger;
    private readonly MixingSampleProvider mixer;
    private CachedSound soundFile;
    private IWavePlayer waveOutDevice;

    public SimpleSoundPlayer(ILogger<SimpleSoundPlayer> logger)
    {
        this.logger = logger;

        waveOutDevice = new WasapiOut(AudioClientShareMode.Shared, 0);
        mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };
        waveOutDevice.Init(mixer);
        waveOutDevice.Play();
    }

    public void Dispose()
    {
        waveOutDevice?.Stop();
        waveOutDevice?.Dispose();
        waveOutDevice = default;
    }

    public async Task LoadAudioFile(string filePath)
    {
        try
        {
            soundFile = await CachedSound.Create(filePath);
            logger.LogInformationEx($"load sound file: {filePath}");
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, $"load sound file {filePath} failed: {e.Message}");
        }
    }

    public void PlaySound()
    {
        if (soundFile == null)
        {
            logger.LogWarningEx("soundFile is null");
            return;
        }

        if (waveOutDevice == null)
            logger.LogWarningEx("waveOutDevice is null");

        mixer.AddMixerInput(new CachedSoundSampleProvider(soundFile));
    }

    private class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cachedSound;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this.cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return (int) samplesToCopy;
        }

        public WaveFormat WaveFormat => cachedSound.WaveFormat;
    }

    private class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        public static async Task<CachedSound> Create(string audioFileName)
        {
            await using var audioFileReader = new AudioFileReader(audioFileName);
            var process = await CheckCompatible(audioFileReader, 44100);
            var cache = new CachedSound
            {
                WaveFormat = audioFileReader.WaveFormat
            };
            cache.AudioData = process.ToArray();
            return cache;
        }

        private static async Task<ISampleProvider> CheckCompatible(ISampleProvider waveProvider, int targetSampleRate)
        {
            var outProvider = waveProvider;

            if (outProvider.WaveFormat.SampleRate != targetSampleRate)
                outProvider = await Task.Run(() => ResampleCacheSound(outProvider, targetSampleRate));

            if (outProvider.WaveFormat.Channels == 1)
                outProvider = await Task.Run(() => MonoToStereoSound(outProvider));

            return outProvider;
        }

        private static ISampleProvider ResampleCacheSound(ISampleProvider outProvider, int targetSampleRate)
        {
            var format = outProvider.WaveFormat;
            var m = WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, format.Channels);
            var resampler = new MediaFoundationResampler(outProvider.ToWaveProvider(), m);
            //var resampler = new WdlResamplingSampleProvider(outProvider, targetSampleRate);
            var outFormat = resampler.WaveFormat;
            return new BufferSampleProvider(resampler.ToSampleProvider().ToArray(), outFormat);
        }

        private static ISampleProvider MonoToStereoSound(ISampleProvider outProvider)
        {
            var converter = new MonoToStereoSampleProvider(outProvider);
            return new BufferSampleProvider(converter.ToArray(), converter.WaveFormat);
        }
    }

    private class BufferSampleProvider : ISampleProvider
    {
        private readonly float[] buffer;

        private int position;

        public BufferSampleProvider(float[] buffer, WaveFormat format)
        {
            this.buffer = buffer;
            WaveFormat = format;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var beforePosition = position;
            for (var i = 0; i < count && position < this.buffer.Length; i++)
                buffer[offset + i] = this.buffer[position++];
            return position - beforePosition;
        }
    }
}