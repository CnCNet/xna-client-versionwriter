using System;
using System.IO;
using SevenZip.Compression.LZMA;

namespace VersionWriter
{
    /// <summary>
    /// LZMA compression helper.
    /// </summary>
    public static class CompressionHelper
    {
        /// <summary>
        /// Compress file using LZMA.
        /// </summary>
        /// <param name="inputFilename">Input file path.</param>
        /// <param name="outputFilename">Output file path.</param>
        public static void CompressFile(string inputFilename, string outputFilename)
        {
            var encoder = new Encoder();

            using (FileStream inputStream = File.OpenRead(inputFilename))
            {
                using (FileStream outputStream = File.Create(outputFilename))
                {
                    encoder.WriteCoderProperties(outputStream);
                    outputStream.Write(BitConverter.GetBytes(inputStream.Length), 0, 8);

                    encoder.Code(inputStream, outputStream,
                        inputStream.Length, outputStream.Length, null);
                }
            }
        }

        /// <summary>
        /// Decompress file using LZMA.
        /// </summary>
        /// <param name="inputFilename">Input file path.</param>
        /// <param name="outputFilename">Output file path.</param>
        public static void DecompressFile(string inputFilename, string outputFilename)
        {
            var decoder = new Decoder();

            using (FileStream inputStream = File.OpenRead(inputFilename))
            {
                using (FileStream outputStream = File.Create(outputFilename))
                {
                    byte[] properties = new byte[5];
                    inputStream.Read(properties, 0, properties.Length);

                    byte[] fileLengthArray = new byte[sizeof(long)];
                    inputStream.Read(fileLengthArray, 0, fileLengthArray.Length);
                    long fileLength = BitConverter.ToInt64(fileLengthArray, 0);

                    decoder.SetDecoderProperties(properties);

                    decoder.Code(inputStream, outputStream,
                        inputStream.Length, fileLength, null);
                }
            }
        }

    }
}
