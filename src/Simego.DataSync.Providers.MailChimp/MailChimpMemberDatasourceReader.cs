using Newtonsoft.Json.Linq;
using Simego.DataSync.Helpers;
using Simego.DataSync.Interfaces;
using Simego.DataSync.Providers.MailChimp.TypeConverters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Simego.DataSync.Providers.MailChimp
{
    [ProviderInfo(Name = "Mailchimp List Member Provider", Group = "Mailchimp", Description = "Manage Members of a Mailchimp List.")]
    public class MailChimpMemberDatasourceReader : DataReaderProviderBase, IDataSourceSetup
    {
        private ConnectionInterface _connectionIf;
        private HttpWebRequestHelper helper = new HttpWebRequestHelper();

        private string listName;

        [Category("Settings")]
        [Description("Your Mailchimp API Key.")]
        [ProviderCacheSetting(Name = "MailChimpDatasourceReader.APIKey")]
        public string APIKey { get; set; }
        

        [Category("List")]
        [Description("The Mailchimp List Connect")]
        [TypeConverter(typeof(MailChimpListTypeConverter))]
        public string ListName
        {
            get
            {
                return listName;
            }

            set
            {
                listName = value;

                var entities = Cache.GetCacheItem((string)string.Format("MailChimp.MailChimpListTypeConverter.Lists.{0}", APIKey), () => GetMailChimpLists());

                foreach(var k in entities.Keys)
                {
                    if(entities[k] == value)
                    {
                        ListId = k;
                        break;
                    }
                }
            }
        }

        [Category("List")]
        [Description("The Mailchimp List Connect")]
        [ReadOnly(true)]
        public string ListId { get; set; }

        [Category("List")]
        [Description("The number or records to return per request.")]
        public int PageSize { get; set; }

        [Description("Enable HTTP Request Tracing")]
        [Category("Debug")]
        public bool TraceEnabled { get { return helper.TraceEnabled; } set { helper.TraceEnabled = true; }  }

        public MailChimpMemberDatasourceReader()
        {
            SupportsIncrementalReconciliation = true;
            PageSize = 50;
        }

        public override DataTableStore GetDataTable(DataTableStore dt)
        {
            dt.AddIdentifierColumn(typeof(string));

            var uriHelper = new MailChimpUriHelper(APIKey);
            var mapping = new DataSchemaMapping(SchemaMap, Side);
            var schema = MailChimpDataSchema.MailChimpMemberSchema();
            var included_columns = SchemaMap.GetIncludedColumns();

            int total_items = 0;
            int count = 0;
            bool abort = false;

            helper.SetAuthorizationHeader(APIKey);

            do
            {                
                var result = helper.GetRequestAsJson($"{uriHelper.ListServiceUrl}/{ListId}/members?count={PageSize}&offset={count}");

                total_items = result["total_items"].ToObject<int>();

                if (result["members"] != null)
                {
                    foreach (var item_row in result["members"])
                    {
                        count++;

                        var newRow = dt.NewRow();
                        var id = ProcessRow(mapping, schema, included_columns, item_row, newRow);

                        if (dt.Rows.AddWithIdentifier(newRow, id) == DataTableStore.ABORT)
                        {
                            abort = true;
                            break;
                        }
                    }
                }

            } while (!abort && count < total_items);

            return dt;
        }

        public override DataTableStore GetDataTable(DataTableStore dt, DataTableKeySet keyset)
        {
            dt.AddIdentifierColumn(typeof(string));

            var uriHelper = new MailChimpUriHelper(APIKey);
            var mapping = new DataSchemaMapping(SchemaMap, Side);
            var schema = MailChimpDataSchema.MailChimpMemberSchema();
            var hash_helper = new HashHelper(HashHelper.HashType.MD5);
            var included_columns = SchemaMap.GetIncludedColumns();

            helper.SetAuthorizationHeader(APIKey);

            var target_index = mapping.MapColumnToDestination(keyset.KeyColumn);

            foreach (var key in keyset.KeyValues)
            {
                var index = key;
                if (target_index.Equals("email_address"))
                {
                    index = hash_helper.GetHashAsString(DataSchemaTypeConverter.ConvertTo<string>(key)).ToLower();
                }

                try
                {
                    var result = helper.GetRequestAsJson($"{uriHelper.ListServiceUrl}/{ListId}/members/{index}");

                    var newRow = dt.NewRow();
                    var id = ProcessRow(mapping, schema, included_columns, result, newRow);

                    if (dt.Rows.AddWithIdentifier(newRow, id) == DataTableStore.ABORT)
                    {
                        break;
                    }
                }
                catch (WebException e)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        continue;

                    throw;
                }
            }


            return dt;
        }

        private static string ProcessRow(DataSchemaMapping mapping, Dictionary<string, MailChimpDataSchemaItem> schema, List<DataSchemaItem> included_columns, JToken result, DataTableStoreRow newRow)
        {
            foreach (DataSchemaItem item in included_columns)
            {
                string columnName = mapping.MapColumnToDestination(item);

                var schemaItem = schema[columnName];

                if (schemaItem.IsSubValue)
                {
                    if (result[schemaItem.ObjectName] != null)
                    {
                        if (schemaItem.IsArray)
                        {
                            var array = result[schemaItem.ObjectName] as JArray;
                            if (array != null)
                            {
                                var list = new List<string>();
                                foreach (var i in array)
                                {
                                    var o = i[schemaItem.FieldName]?.ToObject<string>();
                                    if (o != null)
                                    {
                                        list.Add(o);
                                    }
                                }

                                list.Sort();
                                newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(list.ToArray(), item.DataType);
                            }
                        }
                        else
                        {
                            foreach (var sub_item in result[schemaItem.ObjectName].Children<JProperty>())
                            {
                                if (sub_item.Name.Equals(schemaItem.FieldName))
                                {
                                    newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(sub_item.Value, item.DataType);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (result[schemaItem.FieldName] != null)
                    {
                        newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(result[schemaItem.FieldName].ToObject<object>(), item.DataType);
                    }
                }
            }

            return DataSchemaTypeConverter.ConvertTo<string>(result["id"].ToObject<object>());
        }

        public override DataSchema GetDefaultDataSchema()
        {
            return MailChimpDataSchema.MailChimpDataSyncSchema(MailChimpDataSchema.MailChimpMemberSchema());            
        }

        public override List<ProviderParameter> GetInitializationParameters()
        {
            //Return the Provider Settings so we can save the Project File.
            return new List<ProviderParameter>
                       {
                            new ProviderParameter("APIKey", SecurityService.EncryptValue(APIKey), GetConfigKey("APIKey")),
                            new ProviderParameter("ListId", ListId, GetConfigKey("ListId")),
                            new ProviderParameter("ListName", ListName, GetConfigKey("ListName")),
                            new ProviderParameter("PageSize", PageSize.ToString(), GetConfigKey("PageSize"))
                       };
        }

        public override void Initialize(List<ProviderParameter> parameters)
        {
            //Load the Provider Settings from the File.
            foreach (ProviderParameter p in parameters)
            {
                AddConfigKey(p.Name, p.ConfigKey);

                switch (p.Name)
                {
                    case "APIKey":
                        {
                            APIKey = SecurityService.DecyptValue(p.Value);
                            break;
                        }
                    case "ListId":
                        {
                            ListId = p.Value;
                            break;
                        }
                    case "ListName":
                        {
                            ListName = p.Value;
                            break;
                        }
                    case "PageSize":
                        {
                            if (int.TryParse(p.Value, out int value))
                            {
                                PageSize = value;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        public override IDataSourceWriter GetWriter()
        {            
            return new MailChimpMemberDatasourceWriter { SchemaMap = SchemaMap,  };
        }

        #region IDataSourceSetup - Render Custom Configuration UI

        public void DisplayConfigurationUI(Control parent)
        {
            if (_connectionIf == null)
            {
                _connectionIf = new ConnectionInterface();
                _connectionIf.PropertyGrid.SelectedObject = new MailChimpMemberConnectionProperties(this);
            }

            _connectionIf.Font = parent.Font;
            _connectionIf.Size = new Size(parent.Width, parent.Height);
            _connectionIf.Location = new Point(0, 0);

            parent.Controls.Add(_connectionIf);
        }

        public bool Validate()
        {
            try
            {
                if (string.IsNullOrEmpty(APIKey))
                {
                    throw new ArgumentException("You must specify a valid APIKey.");
                }

                if (string.IsNullOrEmpty(ListName))
                {
                    throw new ArgumentException("You must specify a valid Mailchimp Audience List.");
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Mailchimp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false;

        }

        public IDataSourceReader GetReader()
        {
            return this;
        }

        #endregion        

        public Dictionary<string, string> GetMailChimpLists()
        {
            var lists = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var uriHelper = new MailChimpUriHelper(APIKey);

            helper.SetAuthorizationHeader(APIKey);

            var result = helper.GetRequestAsJson(uriHelper.ListServiceUrl);

            if (result["lists"] != null)
            {
                foreach (var item_row in result["lists"])
                {
                    lists[item_row["id"].ToObject<string>()] = item_row["name"].ToObject<string>();
                }
            }

            return lists;
        }        

        public HttpWebRequestHelper GetWebRequestHelper()
        {
            var requestHelper = helper.Copy();
            requestHelper.SetAuthorizationHeader(SecurityService.DecyptValue(APIKey));
            return requestHelper;
        }

        public MailChimpUriHelper GetUriHelper()
        {
            var apiKey = SecurityService.DecyptValue(APIKey);
            return new MailChimpUriHelper(apiKey);
        }

        public override LicenseValidatorResponse Validate(LicenseValidatorRequest request)
        {
            if (request.Mode == LicenseValidatorRequestMode.Design)
                return new LicenseValidatorResponse(true);

            var claims = request.GetClaims("MailChimp");

            return claims.Count == 0 ? new LicenseValidatorResponse(false, "MailChimp", "A Mailchimp Runtime License is required.") : new LicenseValidatorResponse(true);
        }


    }
}
