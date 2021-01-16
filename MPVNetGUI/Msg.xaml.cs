using System.Windows;

namespace MPVNetGUI {
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Msg : Window {
        public Msg(string msg, Window Owner) {
            InitializeComponent();
            this.Owner = Owner;
            textBlock.Text = msg;
            this.ShowDialog();
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
