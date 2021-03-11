using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MPVNetGUI {
    /// <summary>
    /// ConnectServer.xaml 的交互逻辑
    /// </summary>

    public enum protocolType {
        HTTP,
        SFTP,
        FILE_SYSTEM
    }

    public partial class ConnectServer : Window {
        public bool connected = false;
        public protocolType ptype;
        public string url;
        private List<string> histroy_list;

        public ConnectServer() {
            InitializeComponent();
            this.histroy_list = new List<string>(11);
            try {
                using (StreamReader sr = new StreamReader("./serv")) {
                    string _u = sr.ReadLine();
                    while(_u != null) {
                        comboBox.Items.Add(_u);
                        this.histroy_list.Add(_u);
                        _u = sr.ReadLine();
                    }
                }
            }
            catch (FileNotFoundException) {
                ;
            }
            comboBox.Items.Add("");
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
            this.Owner.Close();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e) {
            this.url = comboBox.Text;
            var re = new Regex(@"[a-zA-Z]:\\[\s\S]*");
            if (this.url.StartsWith("http://") || this.url.StartsWith("https://")) {
                this.ptype = protocolType.HTTP;
            }
            else if (this.url.StartsWith("sftp")) {
                this.ptype = protocolType.SFTP;
            }
            else if (re.IsMatch(this.url)) {
                this.ptype = protocolType.FILE_SYSTEM;
            }
            else {
                var _m = new Msg("Protocol not support.", this);
                this.connected = false;
                return;
            }
            this.connected = true;

            bool _rm = false;
            foreach (var s in histroy_list) {
                if (s == this.url) {
                    _rm = true;
                    break;
                }
            }
            if (!_rm) {
                histroy_list.Insert(0, this.url);
                using (StreamWriter sw = new StreamWriter("./serv")) {
                    int _es = (histroy_list.Count > 10)? 10 : histroy_list.Count;
                    for(int i = 0; i < _es; ++i) {
                        sw.WriteLine(histroy_list[i]);
                    }
                }
            }
            this.Close();
        }
    }
}
