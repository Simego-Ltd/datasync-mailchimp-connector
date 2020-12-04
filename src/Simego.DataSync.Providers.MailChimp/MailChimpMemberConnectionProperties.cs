using Simego.DataSync.Providers.MailChimp.TypeConverters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Simego.DataSync.Providers.MailChimp
{
    class MailChimpMemberConnectionProperties
    {
        private readonly MailChimpMemberDatasourceReader _reader;

        [Category("Settings")]
        public string APIKey
        {

            get { return _reader.APIKey; }
            set { _reader.APIKey = value; }
        }


        [Category("List")]
        [Description("The Mailchimp List Connect")]
        [TypeConverter(typeof(MailChimpListTypeConverter))]
        public string ListName
        {

            get { return _reader.ListName; }
            set { _reader.ListName = value; }
        }

        public MailChimpMemberConnectionProperties(MailChimpMemberDatasourceReader reader)
        {
            _reader = reader;
        }

        public Dictionary<string, string> GetMailChimpLists()
        {
            return _reader.GetMailChimpLists();
        }
    }
}
