using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace MnistReader
{
    public static class MNIST
    {
        private static readonly string MnistImages_extension = ".idx3-ubyte";
        private static readonly string MnistLabels_extension = ".idx1-ubyte";
        public class MNISTData
        {
            public int Width { get; }
            public int Height { get; }
            public byte[] Data { get; }
            public int Label { get; set; }
            public int Index { get; set; }

            public MNISTData(int width, int height, byte[] data)
            {
                Width = width;
                Height = height;
                Data = data;
            }
        }

        public static MNISTData[] LoadDataFromFiles(string imagesPath, string labelsPath, int limit = -1, int offset = 0)
        {
            byte[] imagesRaw = LoadRawDataFromFile(imagesPath);
            byte[] labelsRaw = LoadRawDataFromFile(labelsPath);

            List<MNISTData> dataset = new List<MNISTData>();
            MemoryStream imagesStream = new MemoryStream(imagesRaw);

            int items_count = 0;

            using (BinaryReader br = new BinaryReader(imagesStream))
            {
                //magic number
                int magic_number = br.ReadInt32();

                byte[] header = br.ReadBytes(4);
                Array.Reverse(header);
                int count = BitConverter.ToInt32(header, 0);
                items_count = count;

                header = br.ReadBytes(4);
                Array.Reverse(header);
                int width = BitConverter.ToInt32(header);
                header = br.ReadBytes(4);
                Array.Reverse(header);
                int height = BitConverter.ToInt32(header);
                
                if (offset > 0) 
                {
                    if (offset >= items_count) { return dataset.ToArray(); }

                    imagesStream.Seek((offset * width * height), SeekOrigin.Current);
                    items_count = count - offset;
                }
                if (limit > 0 && limit < items_count) { items_count = limit; }
                
                for (int i = 0; i < items_count; i++)
                {
                    byte[] image = br.ReadBytes(width * height);
                    MNISTData data = new MNISTData(width, height, image);
                    data.Index = i + offset;
                    dataset.Add(data);
                }
            }

            MemoryStream labelsStream = new MemoryStream(labelsRaw);
            using (BinaryReader br = new BinaryReader(labelsStream))
            {
                //magic number
                br.ReadInt32();
                byte[] header = br.ReadBytes(4);
                Array.Reverse(header);
                int count = BitConverter.ToInt32(header, 0);

                if (offset > 0) { labelsStream.Seek(offset, SeekOrigin.Current); }

                for (int i = 0; i < items_count; i++)
                {
                    dataset[i].Label = br.ReadByte();
                }
            }

            return dataset.ToArray();
        }

        private static byte[] LoadRawDataFromFile(string filepath)
        {
            FileInfo f = new FileInfo(filepath);
            if (!f.Exists) { throw new FileNotFoundException($"Wrong file path {filepath}"); }

            byte[] result = null;
            if (f.Extension.ToLower() == ".gz")
            {
                using (FileStream fStream = f.OpenRead())
                {
                    using (MemoryStream dataStream = new MemoryStream()) 
                    {
                        using (GZipStream decompressionStream = new GZipStream(fStream, CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(dataStream);
                            result = dataStream.ToArray();
                        }
                    }
                }
            }
            else if (f.Extension.ToLower() == MnistImages_extension || f.Extension.ToLower() == MnistLabels_extension) 
            { 
                result = File.ReadAllBytes(f.FullName);
            }
            else { throw new FileLoadException($"Wrong file extension {f.FullName}"); }

            return result;
        }
    }
}
