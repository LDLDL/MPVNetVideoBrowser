using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using WinSCP;

namespace MPVNetGUI {
    
    public enum fileType {
        None,
        Video,
        Audio,
        Pciture,
        Subtitle
    }

    public class netFile {
        public string Name { get; }
        public string Url { get; }
        public bool Isdir { get; }
        public fileType Type { get; } = fileType.None;

        private static Regex re_video = new Regex(
            ".(mkv|mka|mp4|m2ts|avi|flv|wmv|mov|rmvb|vob)$",
            RegexOptions.IgnoreCase
        );
        private static Regex re_audio = new Regex(
            ".(cue|mp3|flac|wav|wma|ape|aac|dsf|dff)$",
            RegexOptions.IgnoreCase
        );
        private static Regex re_pic = new Regex(
            ".(jpg|png|gif|bmp|webp|tif)$",
            RegexOptions.IgnoreCase
        );

        public netFile(string Name, string Url, bool Isdir = false) {
            this.Name = Name;
            this.Url = Url;
            this.Isdir = Isdir;
            if (!Isdir) {
                if (re_video.IsMatch(this.Url)) {
                    this.Type = fileType.Video;
                }
                else if (re_audio.IsMatch(this.Url)) {
                    this.Type = fileType.Audio;
                }
                else if (re_pic.IsMatch(this.Url)) {
                    this.Type = fileType.Pciture;
                }
                else if (this.Url.EndsWith(".ass")) {
                    this.Type = fileType.Subtitle;
                }
                else {
                    this.Type = fileType.None;
                }
            }
            else {
                this.Type = fileType.None;
            }
        }

        public bool playable() {
            return (Type != fileType.None) && (Type != fileType.Subtitle);
        }
    }

    public class netFileCollection : List<netFile> {

    }

    public abstract class NFB {
        public netFileCollection filelist;

        public abstract void cdurl(string url);

        public void sortFile() {
            this.filelist.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }

    class HC : NFB {
        private string http_url;
        private HtmlWeb web;

        public HC(string url) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            web = new HtmlWeb();
            this.cdurl(url);
        }

        private string getabsurl(HtmlNode node) {
            string _u = node.GetAttributeValue("href", "");
            if (_u.StartsWith("http://") || _u.StartsWith("https://")) {
                return _u;
            }
            else {
                return this.http_url + _u;
            }
        }

        private void httpget() {
            this.filelist = new netFileCollection();
            var htmlDoc = this.web.Load(this.http_url);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if(nodes[0].GetAttributeValue("href", "") != "../") {
                this.filelist.Add(new netFile(
                    Name: "../",
                    Url: "",
                    Isdir: true
                ));
            }
            foreach (var node in nodes) {
                if (node.NodeType == HtmlNodeType.Element) {
                    var _u = this.getabsurl(node);
                    this.filelist.Add(new netFile(
                        Name: node.InnerText,
                        Url: _u,
                        Isdir: _u.EndsWith("/")
                    ));
                }
            }
        }

        public override void cdurl(string url) {
            if (!url.EndsWith("/")) {
                url += "/";
            }
            this.http_url = url;
            this.httpget();
        }
    }

    class SC : NFB {
        private string sftp_url;
        private string sftp_host_url;
        private string sftp_cur_path;
        private int spliti;
        private static Regex re_split = new Regex(@"[:@/]");
        private static Regex re_sftp = new Regex(@"sftp://[^:]+:[^:]+@[^:]+/*[\s\S]*");
        private static Regex re_sftp_port = new Regex(@"sftp://[^:]+:[^:]+@[^:]+:[0-9]+/*[\s\S]*");
        private SessionOptions sop;
        private Session session = new Session();

        public SC(string url) {
            this.sftp_url = url;
            this.sftp_cur_path = "/";
            int port_num = 0;
            if (!re_sftp_port.IsMatch(this.sftp_url)) {
                if (!re_sftp.IsMatch(this.sftp_url)) {
                    throw new ArgumentException("Wrong sftp url.");
                }
                port_num = 22;
            }

            var hl = re_split.Split(this.sftp_url.Substring(7));
            if (port_num == 0) {
                port_num = Convert.ToInt32(hl[3]);
                this.spliti = 4;
            }
            else {
                this.spliti = 3;
            }
            for (int i = this.spliti; i < hl.Length; ++i) {
                if (hl[i].Length != 0) {
                    this.sftp_cur_path += String.Format("{0}/", hl[i]);
                }
            }
            this.sftp_host_url = String.Format("sftp://{0}:{1}@{2}:{3}", hl[0], hl[1], hl[2], port_num);

            this.sop = new SessionOptions {
                Protocol = Protocol.Sftp,
                HostName = hl[2],
                PortNumber = port_num,
                UserName = hl[0],
                Password = hl[1],
                GiveUpSecurityAndAcceptAnySshHostKey = true
            };

            this.session.Open(sop);
            this.sftp_listdir();
        }

        private void sftp_listdir() {
            this.filelist = new netFileCollection();
            var rdi = this.session.ListDirectory(this.sftp_cur_path);
            for (int i = 1; i < rdi.Files.Count; ++i) {
                filelist.Add(new netFile(
                    Name: rdi.Files[i].Name,
                    Url: this.sftp_host_url + rdi.Files[i].FullName,
                    Isdir: rdi.Files[i].IsDirectory
                )); 
            }
            this.sortFile();
        }

        public override void cdurl(string url) {
            this.sftp_url = url;
            this.sftp_cur_path = "/";
            var hl = re_split.Split(this.sftp_url.Substring(7));
            for (int i = this.spliti; i < hl.Length; ++i) {
                if (hl[i].Length != 0) {
                    this.sftp_cur_path += String.Format("{0}/", hl[i]);
                }
            }
            this.sftp_listdir();
        }
    }

    class DAV : NFB
    {
        private string webdav_url;
        private string webdav_file_base_url;
        private string webdav_cur_path;
        private bool ssl;
        private int spliti;
        private static Regex re_webdav_split = new Regex(@"[:@/]");
        private static Regex re_webdav = new Regex(@"(dav|davs)://[^:]+:[^:]+@[^:]+/*[\s\S]*");
        private static Regex re_webdav_port = new Regex(@"(dav|davs)://[^:]+:[^:]+@[^:]+:[0-9]+/*[\s\S]*");
        private SessionOptions sop;
        private Session session = new Session();

        public DAV(string url) {
            this.webdav_url = url;
            this.webdav_cur_path = "/";
            this.ssl = this.webdav_url.StartsWith("davs");
            int port_num = 0;

            if (!re_webdav_port.IsMatch(this.webdav_url))
            {
                if (!re_webdav.IsMatch(this.webdav_url))
                {
                    throw new ArgumentException("Wrong webdav url.");
                }
                port_num = this.ssl ? 443 : 80;
            }

            var hl = re_webdav_split.Split(this.webdav_url.Substring(ssl ? 7 : 6));
            if (port_num == 0)
            {
                port_num = Convert.ToInt32(hl[3]);
                this.spliti = 4;
            }
            else
            {
                this.spliti = 3;
            }

            for (int i = this.spliti; i < hl.Length; ++i)
            {
                if (hl[i].Length != 0)
                {
                    this.webdav_cur_path += String.Format("{0}/", hl[i]);
                }
            }
            this.webdav_file_base_url = ssl ? String.Format("https://{0}:{1}@{2}:{3}", hl[0], hl[1], hl[2], port_num) :
                                              String.Format("http://{0}:{1}@{2}:{3}", hl[0], hl[1], hl[2], port_num);

            this.sop = new SessionOptions
            {
                Protocol = Protocol.Webdav,
                HostName = hl[2],
                PortNumber = port_num,
                UserName = hl[0],
                Password = hl[1],
                RootPath = this.webdav_cur_path,
                WebdavSecure = ssl
            };

            this.session.Open(sop);
            this.webdav_lsdir();
        }

        private void webdav_lsdir() {
            this.filelist = new netFileCollection();
            var rdi = this.session.ListDirectory(this.webdav_cur_path);

            this.filelist.Add(new netFile(
                Name: "../",
                Url: "",
                Isdir: true
            ));

            for (int i = 1; i < rdi.Files.Count; ++i)
            {
                filelist.Add(new netFile(
                    Name: rdi.Files[i].Name,
                    Url: this.webdav_file_base_url + rdi.Files[i].FullName,
                    Isdir: rdi.Files[i].IsDirectory
                ));
            }
            this.sortFile();
        }

        public override void cdurl(string url) {
            this.webdav_url = url;
            this.webdav_cur_path = "/";
            var hl = re_webdav_split.Split(this.webdav_url.Substring(this.ssl ? 8 : 7));
            for (int i = this.spliti; i < hl.Length; ++i)
            {
                if (hl[i].Length != 0)
                {
                    this.webdav_cur_path += String.Format("{0}/", hl[i]);
                }
            }
            this.webdav_lsdir();
        }
    }

    class LF : NFB {
        private string fs_path;

        public LF(string path) {
            this.cdurl(path);
        }

        public override void cdurl(string url) {
            if (Directory.Exists(url)) {
                if (!url.EndsWith(@"\")) {
                    url += @"\";
                }
                this.fs_path = url;
            }
            else {
                throw new ArgumentException("Not a directory.");
            }
            this.lsdir();
        }

        public void lsdir() {
            this.filelist = new netFileCollection();
            var dirinfo = new DirectoryInfo(this.fs_path);
            var _dir = dirinfo.GetDirectories();
            var _file = dirinfo.GetFiles();

            this.filelist.Add(new netFile(
                Name: @"..\",
                Url: "",
                Isdir: true
            ));

            foreach (var i in _dir) {
                this.filelist.Add(new netFile(
                    Name: i.Name,
                    Url: this.fs_path + i.Name,
                    Isdir: true
                ));
            }
            foreach (var i in _file) {
                this.filelist.Add(new netFile(
                    Name: i.Name,
                    Url: this.fs_path + i.Name,
                    Isdir: false
                ));
            }
        }

    }
}
