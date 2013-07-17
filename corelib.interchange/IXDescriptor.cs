using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace InfoStream.Metadata
{
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
        private string uniqueIdentifierField = null;
        public string UniqueIdentifierField
        {
            get { return uniqueIdentifierField; }
            set { uniqueIdentifierField = value; }
        }

        private List<IXDescriptorProperty> properties = new List<IXDescriptorProperty>();
        public List<IXDescriptorProperty> Properties
        {
            get { return properties; }
            set { properties = value; }
        }

        public IXDescriptor(string uniqueIdentifierField)
        {
            if (String.IsNullOrEmpty(uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");
        }

        public IXDescriptor(string uniqueIdentifierField, IEnumerable<IXDescriptorProperty> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            if (String.IsNullOrEmpty(uniqueIdentifierField) || !fields.Any(p => p.Name == uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");

            properties.AddRange(fields);
        }
    }

    [Serializable]
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

        public FieldStore Store { get; set; }
        public FieldIndex Index { get; set; }

        public IXDescriptorProperty()
        {
        }

        public IXDescriptorProperty(string name, string value, byte[] binaryValue, FieldStore store, FieldIndex index)
        {
            this.Name = name;
            this.StringValue = (binaryValue == null) ? value : null;
            this.BinaryValue = (binaryValue == null) ? null : binaryValue;
            this.Store = store;
            this.Index = index;
        }
    }
}
