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
        public List<string> suburl = new List<string>();
        private netFileCollection sub_file_list;

        public SelectSub(netFileCollection filelist, string filename, Window Owner) {
            InitializeComponent();
            this.Owner = Owner;
            this.sub_file_list = new netFileCollection();
            foreach(var nf in filelist) {
                if (nf.Type == fileType.Subtitle) {
                    int l = (filename.Length > nf.Name.Length) ? nf.Name.Length : filename.Length;
                    for (int i = 0; i < l; ++i) {
                        if(filename[i] != nf.Name[i]) {
                            break;
                        }
                        else if (filename[i] == '.' && nf.Name[i] == '.'){
                            suburl.Add(nf.Url);
                            havesub = true;
                            break;
                        }
                    }
                    this.sub_file_list.Add(nf);
                    listBox.Items.Add(nf.Name);
                }
            }
            if ( (sub_file_list.Count > 0) && (suburl.Count == 0) ){
                this.ShowDialog();
            }
        }

        public void listBox_MouseDoubleClick(object sender, EventArgs e) {
            if(listBox.SelectedIndex != -1) {
                this.suburl.Add(sub_file_list[listBox.SelectedIndex].Url);
                this.havesub = true;
                this.Close();
            }
        }
    }
}
