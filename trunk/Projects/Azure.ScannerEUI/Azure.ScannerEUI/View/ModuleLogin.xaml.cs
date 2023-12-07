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

namespace Azure.ScannerEUI.View
{
    /// <summary>
    /// ModuleLogin.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleLogin : Window
    {
        public ModuleLogin()
        {
            InitializeComponent();
        }

        private void Signin_Click(object sender, RoutedEventArgs e)
        {
            string txtPsw = pwd.Password;
            if (string.Empty == txtPsw)
            {
                return;
            }
            if (txtPsw != "admin")
            {
                MessageBox.Show("Password error!");
                return;
            }
            this.Hide();
            ModuleInfo lmcs = new ModuleInfo();
            lmcs.ShowDialog();
        }
    }
}
