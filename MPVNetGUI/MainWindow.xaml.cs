using System;
using System.Diagnostics;
using System.Windows;

namespace MPVNetGUI {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private ConnectServer connect;
        private NFB networkclient;
        private Process vidoprocess;

        public MainWindow() {
            InitializeComponent();
        }

        private void ConnectServer(object sender, System.EventArgs e) {
            connect = new ConnectServer {
                Owner = this
            };
            connect.Closed += new EventHandler(Connect);
            connect.ShowDialog();
        }

        public void Connect(object sender, EventArgs e) {
            if (connect.connected) {
                if(connect.ptype == connect.HTTP) {
                    try {
                        this.networkclient = new HC(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                else if(connect.ptype == connect.SFTP) {
                    try {
                        this.networkclient = new SC(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                else if (connect.ptype == connect.FILE_SYSTEM) {
                    try {
                        this.networkclient = new LF(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                foreach (var fn in this.networkclient.filename) {
                    listBox.Items.Add(fn);
                }
            }
            else {
                this.Close();
            }
        }

        private void listBox_MouseDoubleClick(object sender, EventArgs e) {
            if (listBox.SelectedIndex != -1) {
                bool isfile;
                try {
                    isfile = this.networkclient.cdindex(listBox.SelectedIndex);
                }
                catch (Exception ex) {
                    var _m = new Msg(ex.Message, this);
                    return;
                }
                if (!isfile) {
                    listBox.Items.Clear();
                    foreach (var fn in this.networkclient.filename) {
                        listBox.Items.Add(fn);
                    }
                    listBox.SelectedIndex = 0;
                    listBox.ScrollIntoView(listBox.Items[0]);
                }
                else {
                    if (this.networkclient.isvideo(listBox.SelectedIndex)) {
                        string com_par = '"' + networkclient.getabsurl(listBox.SelectedIndex) + '"';
                        var ss = new SelectSub(networkclient, this);
                        if (ss.havesub) {
                            com_par += " --sub-file=\"" + ss.suburl + '"';
                        }
                        this.vidoprocess = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo(".\\mpv.exe", com_par);
                        this.vidoprocess.StartInfo = startInfo;
                        this.vidoprocess.StartInfo.UseShellExecute = true;
                        /*                        this.vidoprocess.StartInfo.RedirectStandardInput = true;
                                                this.vidoprocess.StartInfo.RedirectStandardOutput = true;
                                                this.vidoprocess.StartInfo.RedirectStandardError = true;*/
                        this.vidoprocess.Start();
                    }
                    else {
                        var _m = new Msg("Not a video.", this);
                    }
                }
            }
        }
    }
}
