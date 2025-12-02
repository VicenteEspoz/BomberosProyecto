using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // Convert an AudioClip to WAV byte[] (PCM 16-bit)
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null) throw new ArgumentNullException(nameof(clip));

        int channels = clip.channels;
        int sampleCount = clip.samples * channels;
        float[] data = new float[sampleCount];
        clip.GetData(data, 0);

        short[] intData = new short[sampleCount];
        byte[] bytesData = new byte[sampleCount * 2];

        const float rescaleFactor = 32767; // to convert float to Int16

        for (int i = 0; i < data.Length; i++)
        {
            float f = data[i];
            f = Mathf.Clamp(f, -1f, 1f);
            short val = (short)(f * rescaleFactor);
            intData[i] = val;
            byte[] byteArr = BitConverter.GetBytes(val);
            bytesData[i * 2] = byteArr[0];
            bytesData[i * 2 + 1] = byteArr[1];
        }

        using (MemoryStream memoryStream = new MemoryStream())
        {
            // RIFF header
            memoryStream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(36 + bytesData.Length), 0, 4);
            memoryStream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // fmt subchunk
            memoryStream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size (16 for PCM)
            memoryStream.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat (1 = PCM)
            memoryStream.Write(BitConverter.GetBytes((short)channels), 0, 2);
            memoryStream.Write(BitConverter.GetBytes(clip.frequency), 0, 4);
            int byteRate = clip.frequency * channels * 2; // sampleRate * channels * bytesPerSample
            memoryStream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            short blockAlign = (short)(channels * 2);
            memoryStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
            memoryStream.Write(BitConverter.GetBytes((short)16), 0, 2); // bitsPerSample

            // data subchunk
            memoryStream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
            memoryStream.Write(bytesData, 0, bytesData.Length);

            return memoryStream.ToArray();
        }
    }
}
