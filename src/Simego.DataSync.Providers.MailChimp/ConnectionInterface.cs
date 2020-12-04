using System.Windows.Forms;

namespace Simego.DataSync.Providers.MailChimp
{
    public partial class ConnectionInterface : UserControl
    {
        public PropertyGrid PropertyGrid { get { return propertyGrid1; } }
        
        public ConnectionInterface()
        {
            InitializeComponent();
        }
    }
}
