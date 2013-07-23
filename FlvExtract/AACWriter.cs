﻿using System.IO;

namespace FlvExtract
{
    internal class AACWriter : IAudioWriter
    {
        private int _aacProfile;
        private int _channelConfig;
        private Stream _fs;
        private int _sampleRateIndex;

        public AACWriter(Stream outputStream)
        {
            _fs = outputStream;
        }

        public AudioFormat AudioFormat
        {
            get { return AudioFormat.Aac; }
        }

        public void Dispose()
        {
            _fs.Dispose();
        }

        public void WriteChunk(byte[] chunk, uint timeStamp)
        {
            if (chunk.Length < 1) return;

            if (chunk[0] == 0)
            { // Header
                if (chunk.Length < 3) return;

                ulong bits = (ulong)BitConverterBE.ToUInt16(chunk, 1) << 48;

                _aacProfile = BitHelper.Read(ref bits, 5) - 1;
                _sampleRateIndex = BitHelper.Read(ref bits, 4);
                _channelConfig = BitHelper.Read(ref bits, 4);

                if ((_aacProfile < 0) || (_aacProfile > 3))
                    throw new ExtractionException("Unsupported AAC profile.");
                if (_sampleRateIndex > 12)
                    throw new ExtractionException("Invalid AAC sample rate index.");
                if (_channelConfig > 6)
                    throw new ExtractionException("Invalid AAC channel configuration.");
            }
            else
            { // Audio data
                int dataSize = chunk.Length - 1;
                ulong bits = 0;

                // Reference: WriteADTSHeader from FAAC's bitstream.c

                BitHelper.Write(ref bits, 12, 0xFFF);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 2, 0);
                BitHelper.Write(ref bits, 1, 1);
                BitHelper.Write(ref bits, 2, _aacProfile);
                BitHelper.Write(ref bits, 4, _sampleRateIndex);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 3, _channelConfig);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 1, 0);
                BitHelper.Write(ref bits, 13, 7 + dataSize);
                BitHelper.Write(ref bits, 11, 0x7FF);
                BitHelper.Write(ref bits, 2, 0);

                _fs.Write(BitConverterBE.GetBytes(bits), 1, 7);
                _fs.Write(chunk, 1, dataSize);
            }
        }
    }
}