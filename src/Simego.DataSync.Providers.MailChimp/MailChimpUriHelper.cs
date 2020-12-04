using System;

namespace Simego.DataSync.Providers.MailChimp
{
    public class MailChimpUriHelper
    {
        public string Datacenter { get; private set;  }

        public string ListServiceUrl => $"https://{Datacenter}.api.mailchimp.com/3.0/lists";
        
        public MailChimpUriHelper(string apiKey)
        {
            var sp = new StringSplit(apiKey, '-');
            
            if (string.IsNullOrEmpty(sp.Value2))
                throw new ArgumentException("Invalid API Key cannot determine Mailchimp Datacenter", apiKey);

            Datacenter = sp.Value2;
        }
    }
}
