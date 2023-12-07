using Azure.ScannerEUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// ModuleInfo.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleInfo : Window
    {
        public ModuleInfo()
        {
            InitializeComponent();
            DataContext = Workspace.This.ModuleInfoViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ModuleInfoViewModel viewModel = DataContext as ModuleInfoViewModel;
            if (viewModel != null)
            {
                viewModel.Initialize();
            }
        }
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null)
                {
                    binding.UpdateSource();
                }

                float laserPower = Convert.ToSingle(tBox.Text);

            }
        }
    }
}
