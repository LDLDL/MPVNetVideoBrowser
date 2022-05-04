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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MPVNetGUI {
    /// <summary>
    /// FileListBox.xaml 的交互逻辑
    /// </summary>
    public partial class FileListBox : UserControl {
        public netFileCollection filelist;

        public FileListBox(netFileCollection filelist) {
            InitializeComponent();
            this.filelist = filelist;
            listBox.Items.Add("../");
            foreach(var nf in this.filelist) {
                listBox.Items.Add(nf.Name);
            }
        }
    }
}
