using System;
using System.Windows;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for ShowUsersWindow.xaml
    /// </summary>
    public partial class ShowUsersWindow : Window
    {
        public AdvInitTableData tableInfo;

        public bool isCurrentUserChanged = false;

        public ShowUsersWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //инициализация значений для работы с текущей таблицей
            InitDataGrid();

            tableInfo.ColumnInfo = new HeaderInfo[0];

            //работа с отображаемой таблицей
            TableWork(tableInfo);
        }

        private void InitDataGrid()
        {
            tableInfo = (Owner as MainWindow).usersTableData;
        }

        //работа с отображаемой таблицей
        private void TableWork(AdvInitTableData tableInfo)
        {
            //указываем, что источником таблицы является наш datatable
            UsersDataGrid.ItemsSource = tableInfo.DataTable.DefaultView;

            //скрываем столбцы, которые видеть не надо
            for (int i = 0; i < tableInfo.ColumnInfo.Length; i++)
                if (tableInfo.ColumnInfo[i].show == false)
                    UsersDataGrid.Columns[i].Visibility = System.Windows.Visibility.Hidden;

            //обработка столбцов
            for (int i = 0; i < UsersDataGrid.Columns.Count && i < tableInfo.ColumnInfo.Length; i++)
            {
                if (tableInfo.ColumnInfo[i].type == MySqlDbType.Date)
                    UsersDataGrid.Columns[i].ClipboardContentBinding.StringFormat = "MM.yyyy";
            }

            //переприсваивание заголовков таблицы
            for (int i = 0; i < UsersDataGrid.Columns.Count && i < tableInfo.ColumnInfo.Length; i++)
                UsersDataGrid.Columns[i].Header = tableInfo.ColumnInfo[i].obj;
        }
        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            string login, password, name;
            int role_hid;
            if (!GetInputWindowResult(out login, out password, out name, out role_hid))
                return;

            tableInfo.InsertParams = new ObjAndDBType[] {
                new ObjAndDBType {obj = login, type = MySqlDbType.String},
                new ObjAndDBType {obj = password, type = MySqlDbType.String},
                new ObjAndDBType {obj = name, type = MySqlDbType.String},
                new ObjAndDBType {obj = role_hid, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.InsertStr, tableInfo.InsertParams);

            ResetButton_Click(this, e);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
                return;

            string login, password, name;
            int role_id;
            if (!GetInputWindowResult(out login, out password, out name, out role_id))
                return;

            tableInfo.UpdateParams = new ObjAndDBType[]{
                new ObjAndDBType {obj = login,  type = MySqlDbType.String},
                new ObjAndDBType {obj = password,  type = MySqlDbType.String},
                new ObjAndDBType {obj = name,  type = MySqlDbType.String},
                new ObjAndDBType {obj = role_id, type = MySqlDbType.Int32},
                new ObjAndDBType {obj = 
                    (UsersDataGrid.SelectedItem as DataRowView).Row.Table.Columns.Contains("hid") ?
                    Convert.ToInt32((UsersDataGrid.SelectedItem as DataRowView).Row["hid"]) :
                    -1, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.UpdateStr, tableInfo.UpdateParams);

            ResetButton_Click(this, e);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
                return;
            UsersDataGrid.CommitEdit();

            tableInfo.DeleteParams = new ObjAndDBType[]{
                new ObjAndDBType {obj = 
                    (UsersDataGrid.SelectedItem as DataRowView).Row.Table.Columns.Contains("hid") ?
                    Convert.ToInt32((UsersDataGrid.SelectedItem as DataRowView).Row["hid"]) :
                    -1, type = MySqlDbType.Int32}
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, tableInfo.DeleteStr, tableInfo.DeleteParams);

            ResetButton_Click(this, e);
        }

        private bool GetInputWindowResult(out string login, out string password, out string name, out int role_id)
        {
            //создание окна
            InputUserWindow w = new InputUserWindow();
            w.selectedRow =
                UsersDataGrid.SelectedItem != null ?
                (UsersDataGrid.SelectedItem as DataRowView).Row :
                null;
            w.Owner = this;
            //отображение окна
            w.ShowDialog();

            //получение результатов
            login = w.LoginTextBox.Text;

            password = w.UserPasswordBox.Password;

            name = w.NameTextBox.Text;

            role_id =
                w.RoleComboBox.SelectedItem != null &&
                w.rolesTableInfo.DataTable.Columns.Contains("hid") ?
                Convert.ToInt32(w.rolesTableInfo.DataTable.Rows[w.RoleComboBox.SelectedIndex]["hid"]) :
                -1;

            //возврат результата
            if (w.DialogResult == null || w.DialogResult == false)
                return false;

            return true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            tableInfo.DataTable = InitTableData.FillDataTable(InitTableData.connStr, tableInfo.SelectStr, tableInfo.SelectParams);
            TableWork(tableInfo);
        }
    }
}
