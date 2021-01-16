using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using WinSCP;

namespace MPVNetGUI {
    public abstract class NFB {
        private Regex re_isvideo;
        public List<string> filename;

        public NFB() {
            this.re_isvideo = new Regex(".(mkv|mp4|m2ts|avi|flv|wmv|mov|rmvb)$");
        }

        public abstract string getabsurl(int index);
        public abstract bool isdir(int index);
        public abstract bool cdindex(int index);

        public bool isvideo(int index) {
            var _fn = this.getabsurl(index);
            return re_isvideo.IsMatch(_fn);
        }

        public bool issub(int index) {
            var _fn = this.getabsurl(index);
            return _fn.EndsWith(".ass");
        }
    }

    class HC : NFB {
        private string http_root_url;
        private string http_cur_url;
        private HtmlWeb web;
        private HtmlNodeCollection nodes;

        public HC(string url) {
            this.filename = new List<string>(100);
            if (!url.EndsWith("/")) {
                url += "/";
            }
            this.http_root_url = url;
            this.http_cur_url = url;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            this.web = new HtmlWeb();
            httpget();
        }

        private void httpget() {
            this.filename.Clear();
            var htmlDoc = this.web.Load(this.http_cur_url);
            this.nodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (this.nodes[0].GetAttributeValue("href", "") != "../") {
                this.nodes.Insert(0, HtmlNode.CreateNode("<a href=\".. /\">../</a>"));
            }
            foreach (var node in this.nodes) {
                if (node.NodeType == HtmlNodeType.Element) {
                    this.filename.Add(node.InnerText);
                }
            }
        }

        public override string getabsurl(int index) {
            string _u = this.nodes[index].GetAttributeValue("href", "");
            if (_u.StartsWith("http://") || _u.StartsWith("https://")) {
                return _u;
            }
            else {
                return this.http_cur_url + _u;
            }
        }

        public override bool isdir(int index) {
            return getabsurl(index).EndsWith("/");
        }

        public override bool cdindex(int index) {
            if (index == 0) {
                if (this.http_cur_url != this.http_root_url) {
                    this.http_cur_url = this.http_cur_url.Substring(0, this.http_cur_url.LastIndexOf("/"));
                    this.http_cur_url = this.http_cur_url.Substring(0, this.http_cur_url.LastIndexOf("/") + 1);
                }
                httpget();
                return false;
            }
            if (isdir(index)) {
                this.http_cur_url = getabsurl(index);
                httpget();
                return false;
            }
            return true;
        }
    }

    class SC : NFB {
        private string sftp_host_url;
        private string sftp_root_path = "/";
        private string sftp_cur_path;
        private SessionOptions sop;
        private Session session;
        private RemoteDirectoryInfo rdi;

        public SC(string url) {
            filename = new List<string>(100);

            var re = new Regex(@"sftp://[^:]+:[^:]+@[^:]+/*[\s\S]*");
            var re_port = new Regex(@"sftp://[^:]+:[^:]+@[^:]+:[0-9]+/*[\s\S]*");
            var re_split = new Regex(@"[:@/]");

            int port_num = 0;
            if (!re_port.IsMatch(url)) {
                if (!re.IsMatch(url)) {
                    throw new ArgumentException("Wrong sftp url.");
                }
                port_num = 22;
            }

            var hl = re_split.Split(url.Substring(7));
            int i;
            if (port_num == 0) {
                port_num = Convert.ToInt32(hl[3]);
                i = 4;
            }
            else {
                i = 3;
            }
            for (; i < hl.Length; ++i) {
                if (hl[i].Length != 0) {
                    this.sftp_root_path += String.Format("{0}/", hl[i]);
                }
            }
            this.sftp_cur_path = this.sftp_root_path;
            this.sftp_host_url = String.Format("sftp://{0}:{1}@{2}:{3}", hl[0], hl[1], hl[2], port_num);

            this.sop = new SessionOptions {
                Protocol = Protocol.Sftp,
                HostName = hl[2],
                PortNumber = port_num,
                UserName = hl[0],
                Password = hl[1],
                GiveUpSecurityAndAcceptAnySshHostKey = true
            };
            this.session = new Session();
            this.session.Open(this.sop);
            sftp_listdir();
        }

        private void sftp_listdir() {
            this.filename.Clear();
            this.rdi = session.ListDirectory(sftp_cur_path);
            for (int i = 1; i < this.rdi.Files.Count; ++i) {
                this.filename.Add(this.rdi.Files[i].Name);
            }
        }

        public override string getabsurl(int index) {
            return this.sftp_host_url + this.rdi.Files[index + 1].FullName;
        }

        public override bool isdir(int index) {
            return this.rdi.Files[index + 1].IsDirectory;
        }

        public override bool cdindex(int index) {
            if(index == 0) {
                if (this.sftp_cur_path != this.sftp_root_path) {
                    this.sftp_cur_path = sftp_cur_path.Substring(0, sftp_cur_path.LastIndexOf("/"));
                    this.sftp_cur_path = sftp_cur_path.Substring(0, sftp_cur_path.LastIndexOf("/") + 1);
                }
                sftp_listdir();
                return false;
            }
            if (isdir(index)) {
                this.sftp_cur_path = this.rdi.Files[index + 1].FullName + "/";
                sftp_listdir();
                return false;
            }
            return true;
        }
    }

    class LF : NFB {
        private string fs_root_path;
        private string fs_cur_path;

        public LF(string path) {
            filename = new List<string>(100);
            if (Directory.Exists(path)) {
                if (!path.EndsWith(@"\")) {
                    path += @"\";
                }
                this.fs_root_path = path;
                this.fs_cur_path = path;
            }
            else {
                throw new ArgumentException("Not a directory.");
            }
            this.lsdir();
        }

        public override string getabsurl(int index) {
            return this.fs_cur_path + filename[index];
        }

        public override bool isdir(int index) {
            return filename[index].EndsWith(@"\");
        }

        public void lsdir() {
            this.filename.Clear();
            var dirinfo = new DirectoryInfo(this.fs_cur_path);
            var _dir = dirinfo.GetDirectories();
            var _file = dirinfo.GetFiles();

            filename.Add(@"..\");

            foreach (var i in _dir) {
                filename.Add(i.Name + @"\");
            }
            foreach (var i in _file) {
                filename.Add(i.Name);
            }
        }

        public override bool cdindex(int index) {
            if (index == 0) {
                if (this.fs_cur_path != this.fs_root_path) {
                    this.fs_cur_path = fs_cur_path.Substring(0, fs_cur_path.LastIndexOf(@"\"));
                    this.fs_cur_path = fs_cur_path.Substring(0, fs_cur_path.LastIndexOf(@"\") + 1);
                }
                lsdir();
                return false;
            }
            if (isdir(index)) {
                this.fs_cur_path += filename[index];
                lsdir();
                return false;
            }
            return true;
        }
    }
}
