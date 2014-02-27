using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace utils
{
    /// <summary>
    /// .NET 3.0 DataContractSerializer based serialization
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataContractSerialization<T>
    {
        private Type tT = typeof(T);
        private List<Type> KnownTypes = new List<Type>();


        /// <summary>
        /// constructor
        /// </summary>
        public DataContractSerialization()
        {
        }

        /// <summary>
        /// Serialize object to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] Serialize(T value)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                SerializeToStream(value, ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Serialize object to a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="value"></param>
        public void SerializeToFile(T value, string fileName)
        {
            using (FileStream stream = File.Create(fileName))
            {
                SerializeToStream(value, stream);
            }
        }

        /// <summary>
        /// Serialize object to a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        public void SerializeToStream(T value, Stream stream)
        {
            DataContractSerializer sz = null;

            if (tT.IsGenericType)
            {
                foreach (Type tg in tT.GetGenericArguments())
                {
                    if (!KnownTypes.Contains(tg))
                        KnownTypes.Add(tg);
                }
            }

            if ((typeof(T) == typeof(object)) &&
                KnownTypes.Count > 0)
            {
                sz = new DataContractSerializer(KnownTypes.ElementAt(0), new Type[] { });
            }
            else
                sz = new DataContractSerializer(typeof(T), KnownTypes);

            XmlDictionaryWriter binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream);
            sz.WriteObject(binaryDictionaryWriter, value);
            binaryDictionaryWriter.Flush();
        }

        /// <summary>
        /// Deserialize object from byte array (binary format)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public T Deserialize(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value))
            {
                return DeserializeFromStream(ms);
            }
        }

        /// <summary>
        /// Deserialize object from a file (binary format)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public T DeserializeFromFile(string fileName)
        {
            using (FileStream stream = File.OpenRead(fileName))
            {
                return DeserializeFromStream(stream);
            }
        }

        /// <summary>
        /// Deserialize object from a stream (binary format)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public T DeserializeFromStream(Stream stream)
        {
            object obj = null;

            XmlDictionaryReader binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max);
            DataContractSerializer szk = null;

            if (tT.IsGenericType)
            {
                foreach (Type tg in tT.GetGenericArguments())
                {
                    if (!KnownTypes.Contains(tg))
                        KnownTypes.Add(tg);
                }
            }

            if ((typeof(T) == typeof(object)) &&
                KnownTypes.Count > 0)
            {
                szk = new DataContractSerializer(KnownTypes.ElementAt(0), new Type[] { });
            }
            else
                szk = new DataContractSerializer(typeof(T), KnownTypes);

            try { obj = szk.ReadObject(binaryDictionaryReader); }
            catch
            {
                throw new SerializationException("unable to deserialize object");
            }

            if (obj == null)
                throw new SerializationException("unable to deserialize object");

            return (T)obj;
        }
    }
}
