using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Simego.DataSync.Providers.MailChimp
{
    class MailChimpDataSchema
    {
        public static Dictionary<string, MailChimpDataSchemaItem> MailChimpListSchema()
        {
            var schema = new Dictionary<string, MailChimpDataSchemaItem>(StringComparer.OrdinalIgnoreCase);

            schema.Add("id", new MailChimpDataSchemaItem
            {
                FieldName = "id",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = true
            });

            schema.Add("name", new MailChimpDataSchemaItem
            {
                FieldName = "name",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            return schema;            
        }

        public static Dictionary<string, MailChimpDataSchemaItem> MailChimpMemberSchema()
        {
            var schema = new Dictionary<string, MailChimpDataSchemaItem>(StringComparer.OrdinalIgnoreCase);

            schema.Add("id", new MailChimpDataSchemaItem
            {
                FieldName = "id",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = true
            });

            schema.Add("unique_email_id", new MailChimpDataSchemaItem
            {
                FieldName = "unique_email_id",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = true
            });

            schema.Add("email_type", new MailChimpDataSchemaItem
            {
                FieldName = "email_type",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("email_address", new MailChimpDataSchemaItem
            {
                FieldName = "email_address",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("status", new MailChimpDataSchemaItem
            {
                FieldName = "status",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("firstname", new MailChimpDataSchemaItem
            {
                FieldName = "FNAME",
                ObjectName = "merge_fields",
                IsSubValue = true,
                DataType = MailChimpDataSchemaItemType.String
            });

            schema.Add("lastname", new MailChimpDataSchemaItem
            {
                FieldName = "LNAME",
                ObjectName = "merge_fields",
                IsSubValue = true,
                DataType = MailChimpDataSchemaItemType.String
            });

            schema.Add("ip_signup", new MailChimpDataSchemaItem
            {
                FieldName = "ip_signup",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("timestamp_signup", new MailChimpDataSchemaItem
            {
                FieldName = "timestamp_signup",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.DateTime,
                ReadOnly = false
            });

            schema.Add("ip_opt", new MailChimpDataSchemaItem
            {
                FieldName = "ip_signup",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("timestamp_opt", new MailChimpDataSchemaItem
            {
                FieldName = "timestamp_opt",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.DateTime,
                ReadOnly = false
            });

            schema.Add("member_rating", new MailChimpDataSchemaItem
            {
                FieldName = "member_rating",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.Integer,
                ReadOnly = true
            });

            schema.Add("last_changed", new MailChimpDataSchemaItem
            {
                FieldName = "last_changed",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.DateTime,
                ReadOnly = true
            });

            schema.Add("language", new MailChimpDataSchemaItem
            {
                FieldName = "language",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = false
            });

            schema.Add("vip", new MailChimpDataSchemaItem
            {
                FieldName = "vip",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.Boolean,
                ReadOnly = false
            });

            schema.Add("email_client", new MailChimpDataSchemaItem
            {
                FieldName = "email_client",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = true
            });

            schema.Add("source", new MailChimpDataSchemaItem
            {
                FieldName = "source",
                IsSubValue = false,
                DataType = MailChimpDataSchemaItemType.String,
                ReadOnly = true
            });

            schema.Add("tags", new MailChimpDataSchemaItem
            {
                FieldName = "name",
                ObjectName = "tags",
                IsSubValue = true,
                IsArray = true,                
                DataType = MailChimpDataSchemaItemType.StringArray
            });

            return schema;
        }

        public static MailChimpDataSchemaItemType GetSchemaType(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                switch (value)
                {
                    case "integer": return MailChimpDataSchemaItemType.Integer;
                    case "number": return MailChimpDataSchemaItemType.Number;
                    case "boolean": return MailChimpDataSchemaItemType.Boolean;
                }
            }

            return MailChimpDataSchemaItemType.String;
        }

        public static DataSchema MailChimpDataSyncSchema(Dictionary<string, MailChimpDataSchemaItem> mcSchema)
        {
            DataSchema schema = new DataSchema();
            
            foreach (var key in mcSchema.Keys)
            {
                var item = mcSchema[key];

                switch (item.DataType)
                {
                    case MailChimpDataSchemaItemType.Integer:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(Int32), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                    case MailChimpDataSchemaItemType.Number:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(double), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                    case MailChimpDataSchemaItemType.DateTime:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(DateTime), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                    case MailChimpDataSchemaItemType.Boolean:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(bool), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                    case MailChimpDataSchemaItemType.StringArray:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(string[]), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                    default:
                        {
                            schema.Map.Add(new DataSchemaItem(key, typeof(string), false, false, false, -1) { ReadOnly = item.ReadOnly });
                            break;
                        }
                }

            }

            return schema;
        }
    }
}
