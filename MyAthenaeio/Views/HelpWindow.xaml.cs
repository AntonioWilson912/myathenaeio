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

namespace MyAthenaeio.Views
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        public void SelectScannerHelpTab()
        {
            HelpTabControl.SelectedItem = ScanningHelpTab;
        }

        public void SelectLoanHelpTab()
        {
            HelpTabControl.SelectedItem = LoanHelpTab;
        }

        public void SelectTroubleshootingTab()
        {
            HelpTabControl.SelectedItem = TroubleshootingTab;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
