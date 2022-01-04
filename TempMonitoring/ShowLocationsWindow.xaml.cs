using System;
using System.Windows;
using System.Data;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for ShowLocationsWindow.xaml
    /// </summary>
    public partial class ShowLocationsWindow : Window
    {
        public AdvInitTableData tableInfo;

        public bool isCurrentUserChanged = false;

        public ShowLocationsWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                //инициализация значений для работы с текущей таблицей
                InitDataGrid();

                tableInfo.ColumnInfo = new HeaderInfo[0];

                //работа с отображаемой таблицей
                TableWork(tableInfo);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void InitDataGrid()
        {
            tableInfo = new AdvInitTableData();

            string[] lochostnames = (Owner as MainWindow).roleTreeRoot[0].LocHostNames.ToArray();

            string whereStr = String.Format(" and ( ");
            foreach (string lochostname in (Owner as MainWindow).roleTreeRoot[0].LocHostNames)
                whereStr += String.Format(" lochostname = '{0}' or ", lochostname);
            whereStr += " 1=2 ) ";

            tableInfo.SelectStr = String.Format(@"
                select 
                    a.hid,
	                a.lochostname, 
                    a.description, 
                    a.t_zak, 
                    a.tkod,
                    a.cur_ip, 
                    a.role_hid, 
                    b.hid, 
                    b.country, 
                    b.nod, 
                    b.pch, 
                    b.name, 
                    b.description as role_description 
                from temp.locations a 
	                left join temp.roles b on a.role_hid = b.hid
                where a.deleted is null {0}", whereStr);

            tableInfo.SelectParams = null;

            tableInfo.InsertStr =
                @"insert into temp.locations (lochostname, description, cur_ip, tkod, t_zak, role_hid) values (@1, @2, @3, @4, @5, @6)";

            tableInfo.UpdateStr =
                @"update temp.locations set lochostname=@1, description=@2, cur_ip=@3, tkod=@4, t_zak=@5, role_hid=@6 where hid=@7";

            tableInfo.DeleteStr =
                @"update temp.locations set deleted=CURDATE() where hid=@1";

            tableInfo.DataTable = InitTableData.FillDataTable(InitTableData.connStr, tableInfo.SelectStr, tableInfo.SelectParams);

        }

        //работа с отображаемой таблицей
        private void TableWork(AdvInitTableData tableInfo)
        {
            //указываем, что источником таблицы является наш datatable
            LocationsDataGrid.ItemsSource = tableInfo.DataTable.DefaultView;

            //скрываем столбцы, которые видеть не надо
            for (int i = 0; i < tableInfo.ColumnInfo.Length; i++)
                if (tableInfo.ColumnInfo[i].show == false)
                    LocationsDataGrid.Columns[i].Visibility = System.Windows.Visibility.Hidden;

            //обработка столбцов
            for (int i = 0; i < LocationsDataGrid.Columns.Count && i < tableInfo.ColumnInfo.Length; i++)
            {
                if (tableInfo.ColumnInfo[i].type == MySqlDbType.Date)
                    LocationsDataGrid.Columns[i].ClipboardContentBinding.StringFormat = "MM.yyyy";
            }

            //переприсваивание заголовков таблицы
            for (int i = 0; i < LocationsDataGrid.Columns.Count && i < tableInfo.ColumnInfo.Length; i++)
                LocationsDataGrid.Columns[i].Header = tableInfo.ColumnInfo[i].obj;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            int? userRoleHid = (Owner as MainWindow).user["role_hid"] as int?;
            foreach (DataRow row in tableInfo.DataTable.Rows)
            {
                int? rowRoleHid = row["role_hid"] as int?;
                
                if (userRoleHid != rowRoleHid)
                {
                    MessageBox.Show("Нет прав на добавления новой строки");
                    return;
                }
            }

            string lochostname, description, ip;
            int? tkod, role_hid;
            double? t_zak;
            if (!GetInputWindowResult(out lochostname, out description, out ip, out tkod, out t_zak, out role_hid))
                return;


            tableInfo.InsertParams = new ObjAndDBType[] {
                new ObjAndDBType {obj = lochostname, type = MySqlDbType.String},
                new ObjAndDBType {obj = description, type = MySqlDbType.String},
                new ObjAndDBType {obj = ip, type = MySqlDbType.String},
                new ObjAndDBType {obj = tkod, type = MySqlDbType.Int32},
                new ObjAndDBType {obj = t_zak, type = MySqlDbType.Double},
                new ObjAndDBType {obj = role_hid, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.InsertStr, tableInfo.InsertParams);

            ResetButton_Click(this, e);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocationsDataGrid.SelectedItem == null)
                return;

            DataRow row = LocationsDataGrid.SelectedItem != null ?
                (LocationsDataGrid.SelectedItem as DataRowView).Row :
                null;

            int? rowRoleHid = row["role_hid"] as int?;
            int? userRoleHid = (Owner as MainWindow).user["role_hid"] as int?;
            if (rowRoleHid != userRoleHid)
            {
                MessageBox.Show("Нет прав на изменение текущей строки");
                return;
            }

            string lochostname, description, ip;
            int? tkod, role_hid;
            double? t_zak;
            if (!GetInputWindowResult(out lochostname, out description, out ip, out tkod, out t_zak, out role_hid))
                return;

            tableInfo.UpdateParams = new ObjAndDBType[]{
                new ObjAndDBType {obj = lochostname, type = MySqlDbType.String},
                new ObjAndDBType {obj = description, type = MySqlDbType.String},
                new ObjAndDBType {obj = ip, type = MySqlDbType.String},
                new ObjAndDBType {obj = tkod, type = MySqlDbType.Int32},
                new ObjAndDBType {obj = t_zak, type = MySqlDbType.Double},
                new ObjAndDBType {obj = role_hid, type = MySqlDbType.Int32},
                new ObjAndDBType {obj = 
                    (LocationsDataGrid.SelectedItem as DataRowView).Row.Table.Columns.Contains("hid") ?
                    Convert.ToInt32((LocationsDataGrid.SelectedItem as DataRowView).Row["hid"]) :
                    -1, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.UpdateStr, tableInfo.UpdateParams);

            ResetButton_Click(this, e);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocationsDataGrid.SelectedItem == null)
                return;

            DataRow row = LocationsDataGrid.SelectedItem != null ?
                (LocationsDataGrid.SelectedItem as DataRowView).Row :
                null;

            int? rowRoleHid = row["role_hid"] as int?;
            int? userRoleHid = (Owner as MainWindow).user["role_hid"] as int?;

            if (rowRoleHid != userRoleHid)
            {
                MessageBox.Show("Нет прав на изменение текущей строки");
                return;
            }

            tableInfo.DeleteParams = new ObjAndDBType[]{
                new ObjAndDBType {obj = 
                    (LocationsDataGrid.SelectedItem as DataRowView).Row.Table.Columns.Contains("hid") ?
                    Convert.ToInt32((LocationsDataGrid.SelectedItem as DataRowView).Row["hid"]) :
                    -1, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.DeleteStr, tableInfo.DeleteParams);

            ResetButton_Click(this, e);
        }

        private bool GetInputWindowResult(out string lochostname, out string description, out string ip, out int? tkod, out double? t_zak, out int? role_hid)
        {
            DataRow row = LocationsDataGrid.SelectedItem != null ?
                (LocationsDataGrid.SelectedItem as DataRowView).Row :
                null;

            //создание окна
            InputLocationWindow w = new InputLocationWindow();
            w.selectedRow = row;
            w.Owner = this;
            //отображение окна
            w.ShowDialog();

            //получение результатов
            lochostname = w.LochostnameTextBox.Text;
            description = w.DescriptionTextBox.Text;
            ip = w.IpTextBox.Text;

            tkod = w.TkodUpDown.Value as int?;
            t_zak = w.TZakUpDown.Value as double?;

            role_hid =
                w.RoleComboBox.SelectedItem != null &&
                w.filteredRoles.Columns.Contains("hid") ?
                w.filteredRoles.Rows[w.RoleComboBox.SelectedIndex]["hid"] as int? :
                -1;

            //возврат результата
            if (w.DialogResult == null || w.DialogResult == false)
                return false;

            return true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            (Owner as MainWindow).rolesTableData.DataTable = InitTableData.FillDataTable(InitTableData.connStr, (Owner as MainWindow)  .rolesTableData.SelectStr, (Owner as MainWindow).rolesTableData.SelectParams);
            tableInfo.DataTable = InitTableData.FillDataTable(InitTableData.connStr, tableInfo.SelectStr, tableInfo.SelectParams);
            TableWork(tableInfo);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as MainWindow).UpdateAllButUser();
        }
    }
}
