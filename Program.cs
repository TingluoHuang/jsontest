using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace jsontest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var serializer = new OrchestrationSerializer();

            Int32 i = 32;
            Console.WriteLine(serializer.Serialize(i));
            File.WriteAllText("int.log", serializer.Serialize(i));

            string s = "test";
            Console.WriteLine(serializer.Serialize(s));
            File.WriteAllText("string.log", serializer.Serialize(s));

            var l = new List<int>();
            l.Add(1);
            Console.WriteLine(serializer.Serialize(l));
            File.WriteAllText("list.log", serializer.Serialize(l));

            var d = new Dictionary<string, string>();
            d["foo"] = "bar";
            Console.WriteLine(serializer.Serialize(d));
            File.WriteAllText("dic.log", serializer.Serialize(d));

            object o = new object();
            Console.WriteLine(serializer.Serialize(o));
            File.WriteAllText("object.log", serializer.Serialize(o));
        }
    }

    public class OrchestrationSerializer
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        private readonly JsonSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationSerializer"/> using default
        /// serialization settings.
        /// </summary>
        public OrchestrationSerializer()
            : this((JsonSerializerSettings)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationSerializer"/> class using the
        /// <see cref="JsonSerializerSettings"/> specified. Settings can be null in which case the
        /// default settings are be used.
        /// </summary>
        /// <remarks>
        /// This constructor ignores <see cref="JsonSerializerSettings.TypeNameHandling"/> settings.
        /// </remarks>
        /// <param name="settings">The settings to be used.</param>
        public OrchestrationSerializer(JsonSerializerSettings settings)
        {
            this.serializer = JsonSerializer.Create(settings);

            // Make suere TypeNameHandling is always set to Objects regardless of what was specified
            // in the settings. 
            this.serializer.TypeNameHandling = TypeNameHandling.Objects;
        }

        public OrchestrationSerializer(JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public String Serialize(Object value, Formatting formatting)
        {
            var sb = new StringBuilder(256);
            var textWriter = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(textWriter))
            {
                writer.Formatting = formatting;
                this.serializer.Serialize(writer, value);
            }
            return textWriter.ToString();
        }

        public String Serialize(Object value)
        {
            return this.Serialize(value, this.serializer.Formatting);
        }

        public Object Deserialize(String data, Type objectType)
        {
            if (data == null)
            {
                return data;
            }

            using (var reader = new StringReader(data))
            {
                return this.serializer.Deserialize(new JsonTextReader(reader), objectType);
            }
        }

        public T Deserialize<T>(String data)
        {
            return (T)this.Deserialize(data, typeof(T));
        }

        public Byte[] SerializeToBytes(Object value)
        {
            var stream = new MemoryStream();
            using (stream)
            using (var streamWriter = new StreamWriter(stream, DefaultEncoding))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                this.serializer.Serialize(jsonWriter, value);
            }
            return stream.ToArray();
        }

        public Object DeserializeFromBytes(Byte[] buffer, Type objectType)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (objectType == null) throw new ArgumentNullException("objectType");

            using (var stream = new MemoryStream(buffer, false))
            using (var streamReader = new StreamReader(stream, DefaultEncoding))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return this.serializer.Deserialize(jsonReader, objectType);
            }
        }

        public T DeserializeFromBytes<T>(Byte[] buffer)
        {
            return (T)this.DeserializeFromBytes(buffer, typeof(T));
        }

        public Object DeserializeFromCompressedBytes(Byte[] buffer, Type objectType)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (objectType == null) throw new ArgumentNullException("objectType");

            using (var compressedStream = new MemoryStream(buffer))
            using (var decompressStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            using (var streamReader = new StreamReader(decompressStream, DefaultEncoding))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return this.serializer.Deserialize(jsonReader, objectType);
            }
        }

        public T DeserializeFromCompressedBytes<T>(Byte[] buffer)
        {
            return (T)this.DeserializeFromCompressedBytes(buffer, typeof(T));
        }

        public T DeserializeFromBytes<T>(Byte[] buffer, bool compressed)
        {
            return compressed
                ? (T)this.DeserializeFromCompressedBytes(buffer, typeof(T))
                : (T)this.DeserializeFromBytes(buffer, typeof(T));
        }

        public Byte[] Compress(Byte[] objBytes)
        {
            if (objBytes == null)
            {
                return null;
            }

            Byte[] compressedBytes = null;
            using (var outputStream = new MemoryStream())
            {
                using (var compressedStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
                {
                    compressedStream.Write(objBytes, 0, objBytes.Length);
                }

                compressedBytes = new Byte[outputStream.Length];
                Array.Copy(outputStream.GetBuffer(), 0, compressedBytes, 0, compressedBytes.Length);
            }

            return compressedBytes;
        }

        public Byte[] Decompress(Byte[] objectBytes)
        {
            if (objectBytes == null)
            {
                return null;
            }

            Byte[] decompressedBytes = null;
            using (var outputStream = new MemoryStream())
            {
                using (var inputStream = new MemoryStream(objectBytes))
                using (var decompressStream = new GZipStream(inputStream, CompressionMode.Decompress, false))
                {
                    decompressStream.CopyTo(outputStream);
                }

                decompressedBytes = new Byte[outputStream.Length];
                Array.Copy(outputStream.GetBuffer(), 0, decompressedBytes, 0, decompressedBytes.Length);
            }

            return decompressedBytes;
        }
    }
}
