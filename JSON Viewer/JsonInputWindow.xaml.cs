using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JSON_Viewer
{
    /// <summary>
    /// Interaction logic for JsonInputWindow.xaml
    /// </summary>
    public partial class JsonInputWindow : Window, INotifyPropertyChanged
    {
        public string Text { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public JsonInputWindow()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        public static string ShowAsDialog()
        {
            var win = new JsonInputWindow();
            
            if (win.ShowDialog() == true)
            {
                return win.Text;
            }

            return null;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
