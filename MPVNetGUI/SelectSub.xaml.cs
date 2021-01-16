using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MPVNetGUI {
    /// <summary>
    /// SelectSub.xaml 的交互逻辑
    /// </summary>
    public partial class SelectSub : Window {
        public bool havesub = false;
        public string suburl;
        private List<string> sub_url_list;

        public SelectSub(NFB client, Window Owner) {
            InitializeComponent();
            this.Owner = Owner;
            sub_url_list = new List<string>(100);
            for(int i = 0; i < client.filename.Count; ++i) {
                if (client.issub(i)) {
                    listBox.Items.Add(client.filename[i]);
                    sub_url_list.Add(client.getabsurl(i));
                }
            }
            if(sub_url_list.Count != 0) {
                this.ShowDialog();
            }
        }

        public void listBox_MouseDoubleClick(object sender, EventArgs e) {
            if(listBox.SelectedIndex != -1) {
                suburl = sub_url_list[listBox.SelectedIndex];
                havesub = true;
                this.Close();
            }
        }
    }
}
