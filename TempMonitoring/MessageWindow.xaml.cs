using System;
using System.Windows;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public DataRow warningRow { get; set; }
        private string warningString { get; set; }
        private DateTime date { get; set; }
        private string locHostName { get; set; }

        private decimal hidAdded { get; set; }

        public MessageWindow(DataRow warningRow)
        {
            InitializeComponent();

            this.warningRow = warningRow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = "Внимание!";


            warningString = String.Format(" | {0}, {1}, {2}°C.",
                        warningRow["datec"], warningRow["lochostname"], warningRow["tempc"]);

            double? temp = warningRow["tempc"] as double?;
            double? t_zak = warningRow["t_zak"] as double?;
            
            if(temp == null || t_zak == null)
                Close();

            List<string> messages = (Owner as MainWindow).tempSettings.GetListOfWarningMessages((double)temp, (double)t_zak);
            if (messages.Count > 0)
                foreach (string message in messages)
                    warningString += String.Format(" {0}.", message);
            else
                warningString += String.Format(" Температура в пределах нормы.");

            MessageTextBlock.Text = warningString;

            string insertString = @"
                lock tables temp.exchange_cur write;
                insert into temp.exchange_cur(c_message, datem, lochostname) 
                    values (@1, @2, @3); 
                select LAST_INSERT_ID();
                unlock tables";

            date = DateTime.Now;
            locHostName = warningRow["lochostname"] as string;

            ObjAndDBType[] addArchiveParams = new ObjAndDBType[]{
                        new ObjAndDBType(){
                            obj = warningString,
                            type = MySqlDbType.String
                        },
                        new ObjAndDBType(){
                            obj = date,
                            type = MySqlDbType.DateTime
                        },
                        new ObjAndDBType(){
                            obj = locHostName,
                            type = MySqlDbType.String
                        }
                    };
            ulong? ret = (ulong?)InitTableData.ExecuteCommand(InitTableData.connStr, insertString, addArchiveParams);

            hidAdded = ret == null ? -1 : (decimal)(ulong?)ret;// (decimal)((decimal)(ulong?)ret ?? -1);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // удаление строки из таблицы текущих сообщений (которые не были прочитаны)
            string deleteString = @"delete from temp.exchange_cur where hid=@1";
            ObjAndDBType[] readParams = new ObjAndDBType[]{
                new ObjAndDBType(){
                    obj = hidAdded,
                    type = MySqlDbType.Decimal
                }
            };
            InitTableData.ExecuteCommand(InitTableData.connStr, deleteString, readParams);


            // добавление прочитанной записи в архив
            string insertString = @"insert into temp.exchange_arch(c_message, datem, lochostname, d_read) values (@1, @2, @3, @4)";

            ObjAndDBType[] addArchiveParams = new ObjAndDBType[]{
                        new ObjAndDBType(){
                            obj = warningString,
                            type = MySqlDbType.String
                        },
                        new ObjAndDBType(){
                            obj = date,
                            type = MySqlDbType.DateTime
                        },
                        new ObjAndDBType(){
                            obj = locHostName,
                            type = MySqlDbType.String
                        },
                        new ObjAndDBType(){
                            obj = DateTime.Now,
                            type = MySqlDbType.DateTime
                        }
                    };

            InitTableData.ExecuteCommand(InitTableData.connStr, insertString, addArchiveParams);
        }
    }
}