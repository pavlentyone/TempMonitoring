using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows;

namespace TempMonitoring
{
    public class InitTableData
    {
        public static string connStr = String.Format(
                "Server={0};Database={1};port={2};User Id={3};password={4}",
                Properties.Settings.Default.Server,
                Properties.Settings.Default.Database, 
                Properties.Settings.Default.Port,
                Properties.Settings.Default.UserId,
                Properties.Settings.Default.Password);

        //DataTable, который будет получен в результате работы команды select
        public DataTable DataTable { get; set; }

        //строка команды select
        public string SelectStr { get; set; }

        //объект параметра и его тип при работе команды select
        public ObjAndDBType[] SelectParams { get; set; }

        //конструктор по умолчанию
        public InitTableData()
        {
            DataTable = null;
            SelectStr = null;
            SelectParams = null;
        }

        public InitTableData(DataTable showDataTable, HeaderInfo[] showColumnInfo,
            string selectStr, string insertStr, string updateStr, string deleteStr,
            ObjAndDBType[] selectParams, ObjAndDBType[] insertParams, ObjAndDBType[] updateParams, ObjAndDBType[] deleteParams)
        {
            this.DataTable = showDataTable;
            this.SelectStr = selectStr;
            this.SelectParams = selectParams;
        }

        public InitTableData(InitTableData copy)
        {
            this.DataTable = copy.DataTable;
            this.SelectStr = copy.SelectStr;
            this.SelectParams = copy.SelectParams;
        }

        //метод для заполнения showDataTable через команду в строчной переменной selectStr
        static public DataTable FillDataTable(string connStr, string selectStr, ObjAndDBType[] selectParams)
        {
            DataTable dataTable = null;
            if (selectStr != null && selectStr != "")
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = selectStr;
                    cmd.CommandType = CommandType.Text;
                    if (selectParams != null)
                        for (int i = 0; i < selectParams.Length; i++)
                        {
                            cmd.Parameters.Add("@" + (i + 1).ToString(), selectParams[i].type, 255);
                            cmd.Parameters["@" + (i + 1)].Value = selectParams[i].obj;
                        }
                    MySqlDataAdapter dataAdaptor = new MySqlDataAdapter(cmd);

                    dataTable = new DataTable();
                    dataAdaptor.Fill(dataTable);
                    conn.Close();
                }
            return dataTable;
        }

        static public object ExecuteCommand(string connStr, string sqlStr, ObjAndDBType[] cmdParams)
        {
            object result = null;

            if (sqlStr != null && sqlStr != "")
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = sqlStr;
                    cmd.CommandType = CommandType.Text;
                    if (cmdParams != null)
                        for (int i = 0; i < cmdParams.Length; i++)
                        {
                            cmd.Parameters.Add("@" + (i + 1).ToString(), cmdParams[i].type, 255);
                            cmd.Parameters["@" + (i + 1).ToString()].Value = cmdParams[i].obj;
                        }
                    try
                    {
                        result = cmd.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        result = false;
                    }
                    conn.Close();
                }
            return result;
        }
    }

    public class AdvInitTableData : InitTableData
    {
        //звголовки и типы столбцов, которые будут показаны пользователю
        public HeaderInfo[] ColumnInfo { get; set; }

        //строка команды insert
        public string InsertStr { get; set; }
        //строка команды update
        public string UpdateStr { get; set; }
        //строка команды delete
        public string DeleteStr { get; set; }

        //объект параметра и его тип при работе команды insert
        public ObjAndDBType[] InsertParams { get; set; }
        //объект параметра и его тип при работе команды update
        public ObjAndDBType[] UpdateParams { get; set; }
        //объект параметра и его тип при работе команды delete
        public ObjAndDBType[] DeleteParams { get; set; }

        public AdvInitTableData()
        {
            DataTable = null;
            ColumnInfo = null;
            SelectStr = null;
            InsertStr = null;
            UpdateStr = null;
            DeleteStr = null;
            SelectParams = null;
            InsertParams = null;
            UpdateParams = null;
            DeleteParams = null;
        }

        public AdvInitTableData(DataTable showDataTable, HeaderInfo[] showColumnInfo,
            string selectStr, string insertStr, string updateStr, string deleteStr,
            ObjAndDBType[] selectParams, ObjAndDBType[] insertParams, ObjAndDBType[] updateParams, ObjAndDBType[] deleteParams)
        {
            this.DataTable = showDataTable;
            this.ColumnInfo = showColumnInfo;
            this.SelectStr = selectStr;
            this.InsertStr = insertStr;
            this.UpdateStr = updateStr;
            this.DeleteStr = deleteStr;
            this.SelectParams = selectParams;
            this.InsertParams = insertParams;
            this.UpdateParams = updateParams;
            this.DeleteParams = deleteParams;
        }

        public AdvInitTableData(AdvInitTableData copy)
        {
            this.DataTable = copy.DataTable;
            this.ColumnInfo = copy.ColumnInfo;
            this.SelectStr = copy.SelectStr;
            this.InsertStr = copy.InsertStr;
            this.UpdateStr = copy.UpdateStr;
            this.DeleteStr = copy.DeleteStr;
            this.SelectParams = copy.SelectParams;
            this.InsertParams = copy.InsertParams;
            this.UpdateParams = copy.UpdateParams;
            this.DeleteParams = copy.DeleteParams;
        }
    }

    //класс для хранения объекта параметра и типа параметра
    public class ObjAndDBType
    {
        public object obj { get; set; }
        public MySqlDbType type { get; set; }
    }

    public class HeaderInfo : ObjAndDBType
    {
        public bool show { get; set; }
    }
}
