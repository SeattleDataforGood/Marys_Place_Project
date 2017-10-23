﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Marys_Place_Uploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_Upload_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = null;
            SqlConnection cnn;
            connectionString = $"Server=tcp:{ServerName.Text},1433;Initial Catalog={DatabaseName.Text};Persist Security Info=False;User ID={AzureDBUserName.Text};Password={AzureDBPassword.Password}";
            cnn = new SqlConnection(connectionString);
            try {
                cnn.Open();
                string[] lines = System.IO.File.ReadAllLines((string) FileName.Content);

                foreach (string line in lines)
                {
                    char[] delimiterChars = { ',',';', '\t' };

                    string[] fields = line.Split(delimiterChars);

                    SqlCommand cmd = new SqlCommand("insert into mp_data(shelter, move_in, move_out, moved_from, homeless_status, moved_to, departure_type) value(@0, @1, @2, @3, @4, @5, @6)");

                    int i = 0;
                    foreach (string f in fields)
                    {
                        cmd.Parameters.AddWithValue("@"+i, f);

                        System.Console.WriteLine(f);
                        i++;
                    }

                    cmd.ExecuteNonQuery();
                }

                cnn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                FileName.Content = filename;
            }
        }
    }
}
