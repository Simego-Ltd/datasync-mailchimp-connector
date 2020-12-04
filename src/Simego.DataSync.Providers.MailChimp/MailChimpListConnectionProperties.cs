using System.ComponentModel;

namespace Simego.DataSync.Providers.MailChimp
{
    class MailChimpListConnectionProperties
    {
        private readonly MailChimpListDatasourceReader _reader;
        
        [Category("Settings")]
        public string APIKey {

            get { return _reader.APIKey; }
            set { _reader.APIKey = value; }
        }

        public MailChimpListConnectionProperties(MailChimpListDatasourceReader reader)
        {
            _reader = reader;
        }        
    }
}
