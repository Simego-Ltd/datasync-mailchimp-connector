using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simego.DataSync.Providers.MailChimp
{
    internal enum MailChimpDataSchemaItemType
    {
        String,
        StringArray,
        Integer,
        Number,
        DateTime,
        Boolean        
    }

    class MailChimpDataSchemaItem
    {
        public string Name { get { return IsSubValue ? string.Format("{0}|{1}", ObjectName, FieldName) : FieldName; } }
        public string FieldName { get; set; }
        public string ObjectName { get; set; }
        public bool IsSubValue { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsArray { get; set; }
        public MailChimpDataSchemaItemType DataType { get; set; }
    }
}
