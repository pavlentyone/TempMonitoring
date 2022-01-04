using System;
using System.Windows;
using System.Collections.Generic;
using System.Data;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for InputLocationWindow.xaml
    /// </summary>
    public partial class InputLocationWindow : Window
    {
        public DataRow selectedRow;
        public DataTable filteredRoles;
        private InitTableData rolesTableInfo;
        public InputLocationWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitRolesDataTable();

            if (CheckColumns(rolesTableInfo.DataTable.Columns, "hid", "name", "nod", "pch"))
            {
                int? userRoleHid = (Owner.Owner as MainWindow).user["role_hid"] as int?;
                int? roleHid = null;
                
                // инициализация UI
                if (selectedRow != null &&
                    CheckColumns(selectedRow.Table.Columns, "role_hid", "lochostname", "description", "cur_ip", "tkod", "t_zak"))
                {
                    roleHid = selectedRow["role_hid"] as int?;
                    LochostnameTextBox.Text = selectedRow["lochostname"] as string;
                    DescriptionTextBox.Text = selectedRow["description"] as string;
                    IpTextBox.Text = selectedRow["cur_ip"] as string;
                    TkodUpDown.Value = selectedRow["tkod"] as int?;
                    TZakUpDown.Value = selectedRow["t_zak"] as double?;
                }

                // подготовка и инициализация выпадающих меню
                List<string> rolesList = new List<string>();
                RoleComboBox.ItemsSource = rolesList;
                DataTable taaaa = new DataTable();
                taaaa =rolesTableInfo.DataTable.Clone();
                //var filteredRoles = taaaa.AsEnumerable().Where(t => userLochostname.Contains(t["lochostname"] as string));
                filteredRoles = new DataTable();
                foreach (DataColumn col in rolesTableInfo.DataTable.Columns)
                    filteredRoles.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                rolesTableInfo.DataTable.AsEnumerable().Where(t => t["hid"] as int? == userRoleHid).CopyToDataTable(filteredRoles, LoadOption.OverwriteChanges);

                foreach (DataRow row in filteredRoles.Rows)
                {
                    int? hid = row["hid"] as int?;
                    string name = row["name"] as string;
                    int? nod = row["nod"] as int?;
                    int? pch = row["pch"] as int?;

                    string newElem = String.Format("{0}, НОД-{1}, ПЧ-{2}", name, nod, pch);
                    rolesList.Add(newElem);

                    if (hid == roleHid)
                        RoleComboBox.SelectedItem = newElem;
                }
            }
        }

        private void InitRolesDataTable()
        {
            rolesTableInfo = new InitTableData();

            rolesTableInfo.SelectStr =
              @"
            select 
	            a.hid,
                a.name,
                a.description,
                a.deleted,
                a.country,
                a.nod,
                a.pch
            from temp.roles a
            where a.deleted is null";

            rolesTableInfo.SelectParams = null;

            rolesTableInfo.DataTable = InitTableData.FillDataTable(InitTableData.connStr, rolesTableInfo.SelectStr, rolesTableInfo.SelectParams);
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
            if (LochostnameTextBox.Text == "" || 
                DescriptionTextBox.Text == "" || 
                IpTextBox.Text == "" || 
                RoleComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("Не все поля заполнены или заполнены некорректно");
                return;
            }

            
            DialogResult = true;
            Close();
        }
    }
}
