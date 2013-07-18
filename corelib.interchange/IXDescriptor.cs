using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace InfoStream.Metadata
{
    [Flags]
    public enum FieldFlags
    {
        [EnumMember]
        NONE,
        [EnumMember]
        UNIQUEID
    }

    public enum FieldStore
    {
        [EnumMember]
        YES,
        [EnumMember]
        NO
    }

    public enum FieldIndex
    {
        [EnumMember]
        NO,
        [EnumMember]
        ANALYZED,
        [EnumMember]
        NOT_ANALYZED,
        [EnumMember]
        NOT_ANALYZED_NO_NORMS,
        [EnumMember]
        ANALYZED_NO_NORMS
    }

    /// <summary>
    /// classe di informazioni su di un documento indicizzato dall'indexer
    /// </summary>
    [Serializable]
    public sealed class IXDescriptor
    {
        public sealed class PropertyInfo
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private List<IXDescriptorProperty> properties = new List<IXDescriptorProperty>();
        public List<IXDescriptorProperty> Properties
        {
            get { return properties; }
            set
            {
                if(value!=null)
                    properties = value;
            }
        }

        public PropertyInfo UniqueIdentifier
        {
            get
            {
                IXDescriptorProperty ip = properties.SingleOrDefault(p => ((p.Flags & FieldFlags.UNIQUEID) == FieldFlags.UNIQUEID));
                return (ip==null) ? new PropertyInfo() : new PropertyInfo() { Name = ip.Name, Value = ip.SafeValue };
            }
        }

        public IXDescriptor(IEnumerable<IXDescriptorProperty> fields)
        {
            if (fields == null || fields.Count() < 1)
                throw new ArgumentNullException("fields");

            if (fields.Count(p => (p.Flags & FieldFlags.UNIQUEID) == FieldFlags.UNIQUEID) != 1)
                throw new TypeInitializationException(typeof(IXDescriptor).FullName, new Exception("fields must include a UNIQUEID")); 

            properties.AddRange(fields);
        }

        public IXDescriptor(params IXDescriptorProperty[] fields)
        {
            if (fields == null || fields.Count() < 1)
                throw new ArgumentNullException("fields");

            if (fields.Count(p => (p.Flags & FieldFlags.UNIQUEID) == FieldFlags.UNIQUEID) != 1)
                throw new TypeInitializationException(typeof(IXDescriptor).FullName, new Exception("fields must include a UNIQUEID"));

            properties.AddRange(fields);
        }
    }

    [Serializable]
    [ServiceKnownType(typeof(FieldFlags))]
    [ServiceKnownType(typeof(FieldStore))]
    [ServiceKnownType(typeof(FieldIndex))]
    public sealed class IXDescriptorProperty
    {
        public string Name { get; set; }
        public string StringValue { get; set; }
        public byte[] BinaryValue { get; set; }
        public bool IsBinary
        {
            get { return (BinaryValue == null) ? false : true; }
        }
        public string SafeValue
        {
            get
            {
                return (IsBinary) ? Convert.ToBase64String(BinaryValue) : StringValue;
            }
        }

        public FieldFlags Flags { get; set; }
        public FieldStore Store { get; set; }
        public FieldIndex Index { get; set; }

        public IXDescriptorProperty()
        {
        }

        public IXDescriptorProperty(string name, string value, byte[] binaryValue, FieldFlags flags, FieldStore store, FieldIndex index)
        {
            this.Name = name;
            this.StringValue = (binaryValue == null) ? value : null;
            this.BinaryValue = (binaryValue == null) ? null : binaryValue;
            this.Flags = flags;
            this.Store = store;
            this.Index = index;
        }
    }
}
