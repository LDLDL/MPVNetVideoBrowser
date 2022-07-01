using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MPVNetGUI {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private ConnectServer connect;
        private NFB networkclient;
        private FileListBox cur_flb;
        private Stack<FileListBox> flb_stack;

        public MainWindow() {
            InitializeComponent();
            flb_stack = new Stack<FileListBox>();
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
                if(connect.ptype == protocolType.HTTP) {
                    try {
                        this.networkclient = new HC(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                else if(connect.ptype == protocolType.SFTP) {
                    try {
                        this.networkclient = new SC(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                else if (connect.ptype == protocolType.WEBDAV) {
                    try {
                        this.networkclient = new DAV(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                else if (connect.ptype == protocolType.FILE_SYSTEM) {
                    try {
                        this.networkclient = new LF(connect.url);
                    }
                    catch (Exception ex) {
                        var _m = new Msg(ex.Message, this);
                        this.Close();
                        return;
                    }
                }
                this.cur_flb = new FileListBox(networkclient.filelist);
                this.cur_flb.MouseDoubleClick += new MouseButtonEventHandler(listBox_MouseDoubleClick);
                flb_stack.Push(this.cur_flb);
                this.cur_flb.SetValue(Grid.RowProperty, 1);
                grid.Children.Add(this.cur_flb);
            }
            else {
                this.Close();
            }
        }

        private void listBox_MouseDoubleClick(object sender, EventArgs e) {
            var _i = this.cur_flb.listBox.SelectedIndex;
            if (_i >= 0) {
                if(_i == 0) {
                    if(flb_stack.Count > 1) {
                        grid.Children.Remove(flb_stack.Pop());
                        this.cur_flb = flb_stack.Peek();
                        grid.Children.Add(this.cur_flb);
                        //this.cur_flb.SetValue(Grid.RowProperty, 1);
                        var listboxitem = (ListBoxItem)this.cur_flb.listBox.ItemContainerGenerator.ContainerFromItem(this.cur_flb.listBox.SelectedItem);
                        if (listboxitem != null) {
                            listboxitem.Focus();
                        }
                    }
                }
                else {
                    _i--;
                    var selectd_file = this.cur_flb.filelist[_i];
                    if (selectd_file.Isdir) {
                        try {
                            networkclient.cdurl(this.cur_flb.filelist[_i].Url);
                        }
                        catch (Exception ex) {
                            var _m = new Msg(ex.Message, this);
                            return;
                        }
                        grid.Children.Remove(this.cur_flb);
                        this.cur_flb = new FileListBox(networkclient.filelist);
                        this.cur_flb.MouseDoubleClick += new MouseButtonEventHandler(listBox_MouseDoubleClick);
                        flb_stack.Push(this.cur_flb);
                        this.cur_flb.SetValue(Grid.RowProperty, 1);
                        grid.Children.Add(this.cur_flb);
                    }
                    else {
                        if (selectd_file.playable()) {
                            string command_param = string.Format("\"{0}\"", selectd_file.Url);
                            if(selectd_file.Type == fileType.Video) {
                                var ss = new SelectSub(this.cur_flb.filelist, selectd_file.Name, this);
                                if (ss.havesub) {
                                    foreach (var su in ss.suburl) {
                                        command_param += string.Format(" --sub-file=\"{0}\"", su);
                                    }
                                }
                            }
                            else if(selectd_file.Type == fileType.Pciture) {
                                command_param += " --keep-open=yes";
                            }
                            var vidoprocess = new Process();
                            ProcessStartInfo startInfo = new ProcessStartInfo(".\\mpv.exe", command_param);
                            vidoprocess.StartInfo = startInfo;
                            vidoprocess.StartInfo.UseShellExecute = true;
                            vidoprocess.Start();
                        }
                        else {
                            var _m = new Msg("Can not play.", this);
                        }
                    }
                }
            }
        }

        private void search_button_Click(object sender, RoutedEventArgs e) {
            var search_text = search_textbox.Text;
            if(search_text == "") {
                return;
            }
            var search_result = new netFileCollection();
            foreach (var nf in this.cur_flb.filelist) {
                if (nf.Name.IndexOf(search_text, StringComparison.OrdinalIgnoreCase) > -1) {
                    search_result.Add(nf);
                }
            }
            grid.Children.Remove(this.cur_flb);
            this.cur_flb = new FileListBox(search_result);
            this.cur_flb.MouseDoubleClick += new MouseButtonEventHandler(listBox_MouseDoubleClick);
            flb_stack.Push(this.cur_flb);
            this.cur_flb.SetValue(Grid.RowProperty, 1);
            grid.Children.Add(this.cur_flb);
            search_textbox.Text = "";
        }

        private void textbox_enter(object sender, KeyEventArgs e) {
            if(e.Key == Key.Return) {
                search_button_Click(sender, e);
            }
        }
    }
}
