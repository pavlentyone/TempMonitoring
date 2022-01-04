using System;
using System.Windows;
using System.Collections.Generic;
using System.Data;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for InputUserWindow.xaml
    /// </summary>
    public partial class InputUserWindow : Window
    {
        public DataRow selectedRow;
        public InitTableData rolesTableInfo;
        public InputUserWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rolesTableInfo = (Owner.Owner as MainWindow).rolesTableData;

            //подготовка выпадающих меню

            if (CheckColumns(rolesTableInfo.DataTable.Columns, "hid", "name", "nod", "pch", "lochostname"))
            {
                int? role_hid = null;

                if (selectedRow != null &&
                    CheckColumns(selectedRow.Table.Columns, "role_hid", "login", "password", "name"))
                {
                    LoginTextBox.Text = selectedRow["login"] as string;
                    UserPasswordBox.Password = selectedRow["password"] as string;
                    NameTextBox.Text = selectedRow["name"] as string;
                    role_hid = selectedRow["role_hid"] as int?;
                }

                List<string> rolesList = new List<string>(); 
                RoleComboBox.ItemsSource = rolesList;
                EnumerableRowCollection filteredRoles = 
                    rolesTableInfo.DataTable.AsEnumerable();

                foreach (DataRow row in filteredRoles)
                {
                    int? hid = row["hid"] as int?;
                    string lochostname = row["lochostname"] as string;
                    string name = row["name"] as string;
                    int? nod = row["nod"] as int?;
                    int? pch = row["pch"] as int?;

                    string newItem = String.Format("{0}, НОД-{1}, ПЧ-{2}, {3}", name, nod, pch, lochostname);
                    rolesList.Add(newItem);

                    if (hid == role_hid)
                        RoleComboBox.SelectedItem = newItem;
                }
            }
        }

        private bool CheckColumns(DataColumnCollection cols, params string[] names)
        {
            foreach (string name in names)
                if (!cols.Contains(name))
                    return false;
            return true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoginTextBox.Text == "" || UserPasswordBox.Password == "" || NameTextBox.Text == "" || RoleComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("Не все поля заполнены или заполнены некорректно");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
