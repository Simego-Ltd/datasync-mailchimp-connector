using Newtonsoft.Json.Linq;
using Simego.DataSync.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Simego.DataSync.Providers.MailChimp
{
    [ProviderInfo(Name = "Mailchimp List Provider", Group ="Mailchimp", Description = "Mailchimp Lists Datasource.")]
    public class MailChimpListDatasourceReader : DataReaderProviderBase, IDataSourceSetup
    {
        private ConnectionInterface _connectionIf;
        private HttpWebRequestHelper helper = new HttpWebRequestHelper();

        [Category("Settings")]
        [Description("Your Mailchimp API Key.")]
        [ProviderCacheSetting(Name = "MailChimpDatasourceReader.APIKey")]
        public string APIKey { get; set; }

        public override DataTableStore GetDataTable(DataTableStore dt)
        {
            dt.AddIdentifierColumn(typeof(string));

            var uriHelper = new MailChimpUriHelper(APIKey);
            var mapping = new DataSchemaMapping(SchemaMap, Side);
            var schema = MailChimpDataSchema.MailChimpListSchema();

            helper.SetAuthorizationHeader(APIKey);
            var result = helper.GetRequestAsJson(uriHelper.ListServiceUrl);

            if (result["lists"] != null)
            {
                foreach (var item_row in result["lists"])
                {
                    var newRow = dt.NewRow();

                    foreach (DataSchemaItem item in SchemaMap.GetIncludedColumns())
                    {
                        string columnName = mapping.MapColumnToDestination(item);

                        var schemaItem = schema[columnName];

                        if (schemaItem.IsSubValue)
                        {
                            foreach (var six in item_row[schemaItem.ObjectName])
                            {
                                foreach (var sub_item in item_row[schemaItem.ObjectName].Children<JProperty>())
                                {
                                    if (sub_item.Name.Equals(schemaItem.FieldName))
                                    {
                                        newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(sub_item.Value, item.DataType);
                                    }
                                }
                            }
                        }
                        else
                        {
                            newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(item_row[schemaItem.FieldName], item.DataType);
                        }
                    }

                    if (dt.Rows.AddWithIdentifier(newRow, DataSchemaTypeConverter.ConvertTo<string>(item_row["id"])) == DataTableStore.ABORT)
                    {
                        break;
                    }
                }
            }

            return dt;
        }

        public override DataSchema GetDefaultDataSchema()
        {
            return MailChimpDataSchema.MailChimpDataSyncSchema(MailChimpDataSchema.MailChimpListSchema());
        }
       
        public override List<ProviderParameter> GetInitializationParameters()
        {
            //Return the Provider Settings so we can save the Project File.
            return new List<ProviderParameter>
                       {
                            new ProviderParameter("APIKey", SecurityService.EncryptValue(APIKey), GetConfigKey("APIKey"))
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
                    default:
                        {
                            break;
                        }
                }
            }
        }

        public override IDataSourceWriter GetWriter()
        {
            //if your provider is read-only return null here.
            return new MailChimpListDataSourceWriter { SchemaMap = SchemaMap };
        }

        #region IDataSourceSetup - Render Custom Configuration UI
        
        public void DisplayConfigurationUI(Control parent)
        {
            if (_connectionIf == null)
            {
                _connectionIf = new ConnectionInterface();
                _connectionIf.PropertyGrid.SelectedObject = new MailChimpListConnectionProperties(this);
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
             
        public override LicenseValidatorResponse Validate(LicenseValidatorRequest request)
        {
            if (request.Mode == LicenseValidatorRequestMode.Design)
                return new LicenseValidatorResponse(true);

            var claims = request.GetClaims("MailChimp");

            return claims.Count == 0 ? new LicenseValidatorResponse(false, "MailChimp", "A Mailchimp Runtime License is required.") : new LicenseValidatorResponse(true);
        }
    }
}
