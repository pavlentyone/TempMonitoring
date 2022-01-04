using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Data;
using System.Globalization;

using System.Data;
using MySql.Data.MySqlClient;

using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace TempMonitoring
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public double TickerRate = 60;
        public KoderHack.WPF.Controls.TickerDirection TickerDirection = KoderHack.WPF.Controls.TickerDirection.West;
        public DataRow user;


        public AdvInitTableData usersTableData;
        public AdvInitTableData rolesTableData;
        public AdvInitTableData locationsTableData;

        public InitTableData temparchTableData;

        DataTable currentMessages;

        DataTable currentEvents;

        public ObservableCollection<Node> roleTreeRoot;

        public string[] lochostnames { get; set; }


        DispatcherTimer lastEventsTimer;

        public TemperatureIntervalSettings tempSettings;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUser();

            UpdateAllButUser();

        }

        private void UpdateUser()
        {
            usersTableData = new AdvInitTableData();

            InitUsersDataTable();

            LoginWindow w = new LoginWindow();
            w.Owner = this;
            w.ShowDialog();
            if (w.DialogResult != true)
                Close();

            user = w.matchedUsers[0];

            ShowRoleTextBlock.Text += user["country"] == DBNull.Value ? "" : user["country"];
            ShowRoleTextBlock.Text += user["nod"] == DBNull.Value ? "" : " НОД-" + user["nod"];
            ShowRoleTextBlock.Text += user["pch"] == DBNull.Value ? "" : " ПЧ-" + user["pch"];
        }

        public void UpdateAllButUser()
        {
            temparchTableData = new InitTableData();
            rolesTableData = new AdvInitTableData();
            InitRolesDataTable();

            // инициализация дерева ролей (клиентов)
            try
            {
                roleTreeRoot = InitRoleTreeView(rolesTableData.DataTable);
                RolesTreeView.ItemsSource = roleTreeRoot;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }

            lochostnames = new string[roleTreeRoot[0].LocHostNames.Count];
            roleTreeRoot[0].LocHostNames.CopyTo(lochostnames);

            tempSettings = new TemperatureIntervalSettings();

            lastEventsTimer_Tick(this, null);
            TempGraphRadioButton_Checked(this, null);

            lastEventsTimer = new DispatcherTimer();
            lastEventsTimer.Tick += new EventHandler(lastEventsTimer_Tick);
            lastEventsTimer.Interval = new TimeSpan(0, 0, 2);
            lastEventsTimer.Start();
        }

        private void lastEventsTimer_Tick(object sender, EventArgs args)
        {
            string SelectStr = 
                @"select
                    a.hid,
                    a.c_message,
                    a.datem,
                    a.lochostname,
                    b.cur_ip,
                    b.description as loc_description,
                    b.role_hid
                from temp.exchange_cur a
                    left join temp.locations b on a.lochostname = b.lochostname
                where b.role_hid = @1";

            ObjAndDBType[] SelectParams = new ObjAndDBType[] { 
                new ObjAndDBType {
                obj = user["role_hid"],
                type = MySqlDbType.Int32
                }
            };

            currentMessages = InitTableData.FillDataTable(InitTableData.connStr,
                SelectStr, SelectParams);

            foreach (DataRow row in currentMessages.Rows)
            {
                string message = row["c_message"] as string;
                MessageBoxResult result = MessageBox.Show(message, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                

                if (result == MessageBoxResult.OK)
                {
                    string DeleteStr =
                        @"delete from temp.exchange_cur where hid = @1";
                    ObjAndDBType[] DeleteParams = new ObjAndDBType[]{
                        new ObjAndDBType(){
                            obj = row["hid"],
                            type = MySqlDbType.Decimal
                        }
                    };

                    InitTableData.ExecuteCommand(InitTableData.connStr,
                        DeleteStr, DeleteParams);

                    string InsertStr = 
                    @"insert into temp.exchange_arch (c_message, datem, lochostname, d_read) values (@1, @2, @3, @4)";

                    ObjAndDBType[] InsertParams = new ObjAndDBType[]{
                        new ObjAndDBType(){
                            obj = row["c_message"],
                            type = MySqlDbType.String
                        },
                        new ObjAndDBType(){
                            obj = row["datem"],
                            type = MySqlDbType.DateTime
                        },
                        new ObjAndDBType(){
                            obj = row["lochostname"],
                            type = MySqlDbType.String
                        },
                        new ObjAndDBType(){
                            obj = DateTime.Now,
                            type = MySqlDbType.DateTime
                        }
                    };

                    InitTableData.ExecuteCommand(InitTableData.connStr,
                        InsertStr, InsertParams);
                }

            }

            
            UpdateCurrentEvents();
            CancelTreeColors(null);
            UpdateTreeColors(null);
            RolesTreeView.UpdateLayout();
            UpdateTicker();
        }

        private void UpdateCurrentEvents()
        {
            string whereStr = "";

            Node node = roleTreeRoot.First();

            if (node.LocHostNames.Count > 0)
            {
                whereStr += " ( ";
                foreach (string str in node.LocHostNames)
                {
                    whereStr += String.Format(" a.lochostname = '{0}' or ", str);
                }
                whereStr += " 1=2 )";
            }
            else
            {
                whereStr += "1=2";
            }

            string selectStr = String.Format(
                @"select
                    a.hid,
                    a.lochostname,
                    a.tempc,
                    a.datec,
                    a.c_message,
                    b.t_zak
                from temp.tempwarning a
                    left join temp.locations b on a.lochostname = b.lochostname
                where {0} and is_warning=1", whereStr);

            ObjAndDBType[] selectParams = null;

            currentEvents = InitTableData.FillDataTable(InitTableData.connStr, selectStr, selectParams);
        }

        private void CancelTreeColors(Node node)
        {
            if (node == null)
                node = roleTreeRoot.First();

            node.BackColor = Brushes.Transparent;

            foreach (Node child in node.Children)
                CancelTreeColors(child);
        }

        private void UpdateTreeColors(Node node)
        {
            if (node == null)
                node = roleTreeRoot.First();

            Color newColor;

            if (node.BackColor == null)
                newColor = Colors.Transparent;
            else
                newColor = node.BackColor.Color;

            foreach (string nodeLochostname in node.LocHostNames)
                foreach (DataRow row in currentEvents.Rows)
                {
                    string rowLochostname = row["lochostname"] as string;
                    if (nodeLochostname == rowLochostname)
                    {
                        double? temp = row["tempc"] as double?;
                        double? t_zak = row["t_zak"] as double?;

                        if (temp == null || t_zak == null)
                            continue;


                        Color addColor = Colors.Transparent;

                        if (tempSettings.IsNormal((double)temp, (double)t_zak))
                            addColor = Colors.Transparent;
                        else if ((double)temp > (double)t_zak)
                            addColor = Colors.Red;
                        else if ((double)temp < (double)t_zak)
                            addColor = Colors.DeepSkyBlue;


                        newColor = Color.FromArgb(
                            (byte)((int)(newColor.A + addColor.A) / 2),
                            (byte)((int)(newColor.R + addColor.R) / 2),
                            (byte)((int)(newColor.G + addColor.G) / 2),
                            (byte)((int)(newColor.B + addColor.B) / 2));

                    }
                }

            node.BackColor = new SolidColorBrush(newColor);

            foreach (Node child in node.Children)
                UpdateTreeColors(child);
        }

        private void UpdateTicker()
        {
            if (currentEvents == null)
                UpdateCurrentEvents();

            Node node = RolesTreeView.SelectedValue as Node;

            if (node == null)
                node = roleTreeRoot.First();

            IEnumerable<DataRow> query = currentEvents.AsEnumerable().Where(row => node.LocHostNames.Contains(row["lochostname"] as string));


            if(query.Count() > 0) {
                contentTicker.Content = "";

                foreach (DataRow row in query)
                    contentTicker.Content += row["c_message"] as string;

                contentTicker.Background = new LinearGradientBrush(Colors.DarkRed, Colors.Gray, new Point(0, 0.8), new Point(1, 0.2));
            }
            else{
                contentTicker.Content = "Все спокойно на вашем участке";
                contentTicker.Background = new LinearGradientBrush(Colors.Black, Colors.Gray, new Point(0, 0.8), new Point(1, 0.2));
            }
            contentTicker.Foreground = Brushes.White;
        }

        private void InitUsersDataTable()
        {
            usersTableData.SelectStr =
              @"
            select 
                a.hid, 
                a.login, 
                a.password, 
                a.name,
                a.create_time,
                a.deleted,
	            b.hid as role_hid,
                b.name as roles_name,
                b.description as roles_description,
                b.deleted as roles_deleted,
                b.country,
                b.nod,
                b.pch
            from temp.user a 
                left join temp.roles			b on a.role_hid   	=b.hid
            where a.deleted is null and b.deleted is null";

            usersTableData.InsertStr = @"insert into temp.user (login, password, name, role_hid) values(@1, @2, @3, @4)";

            usersTableData.UpdateStr = @"update temp.user set login=@1, password=@2, name=@3, role_hid=@4 where hid=@5";

            usersTableData.DeleteStr = @"update temp.user set deleted=NOW() where hid=@1";

            usersTableData.DataTable = AdvInitTableData.FillDataTable(InitTableData.connStr, usersTableData.SelectStr, usersTableData.SelectParams);
        }

        private void InitRolesDataTable()
        {


            rolesTableData.SelectStr =
              @"
            select 
	            a.hid,
                a.name,
                a.description,
                a.deleted,
                a.country,
                a.nod,
                a.pch,
                d.lochostname as lochostname,
                d.t_zak as t_zak,
                d.description as loc_description
            from temp.roles a
	            left join temp.locations d on a.hid=d.role_hid
            where a.deleted is null and d.deleted is null";

            rolesTableData.InsertStr = @"insert into temp.roles (country, nod, pch, name, description) values (@1, @2, @3, @4, @5)";

            rolesTableData.UpdateStr = @"update temp.roles set country=@1, nod=@2, pch=@3, name=@4, description=@5 where hid=@6";

            rolesTableData.DeleteStr = @"update temp.roles set deleted=NOW() where hid=@1";

            rolesTableData.DataTable = AdvInitTableData.FillDataTable(InitTableData.connStr, rolesTableData.SelectStr, rolesTableData.SelectParams);
        }

        private ObservableCollection<Node> InitRoleTreeView(DataTable rolesDataTable)
        { 
            // проверка наличия элементов в таблице
            if (rolesDataTable.Rows.Count <= 0)
            {
                MessageBox.Show("Нет пользователей");
                return null;
            }

            // проверка наличия все необходимых столбцов для работы с таблицей
            string[] requiredColumns = new string[] { "nod", "pch", "lochostname" };

            foreach (string reqCol in requiredColumns)
                if (!rolesDataTable.Columns.Contains(reqCol))
                {
                    MessageBox.Show(String.Format("Таблица не имеет необходимого столбца под именем: \"{0}\"", reqCol));
                    return null;
                }

            ObservableCollection<Node> result = new ObservableCollection<Node>();
            // корневой узел, от которого будет создаваться все остальное дерево
            ObservableCollection<Node> currRootChildren = result;

            if (currRootChildren != null)
            {
                string userCountry = user["country"] == DBNull.Value ? null : user["country"] as string;
                string userNod = user["nod"] == DBNull.Value ? null : user["nod"].ToString();
                string userPch = user["pch"] == DBNull.Value ? null : user["pch"].ToString();

                // массив столбцов, которые будут являться уровнями дерева
                List<string> levels = new List<string>();
                if (userNod == null)
                    levels.Add("country");
                if (userPch == null)
                    levels.Add("nod");
                levels.Add("pch");
                levels.Add("loc_description");

                // проходим все строки в БД
                foreach (DataRow dr in rolesDataTable.Rows)
                {

                    // для прохождения дерева от корня используется текущий узел
                    ObservableCollection<Node> currNodeChildren = currRootChildren;
                    try
                    {
                        // прохождение всех уровней дерева в текущей строке
                        for (int i = 0; i < levels.Count; i++)
                            if (dr[levels[i]] != DBNull.Value)
                            {
                                // имя узла выбирается в  соответствии с текущим уровнем 
                                string nodeText = "";
                                string nodeName = "";
                                if (levels[i] == "country" &&
                                    (user["country"] as string == null ||
                                    user["country"] as string == dr["country"] as string))
                                    nodeName = nodeText = String.Format("{0}", dr["country"]);
                                else if (levels[i] == "nod" &&
                                    userNod == dr["nod"] as string)
                                    nodeName = nodeText = String.Format("НОД-{0}", dr["nod"]);
                                else if (levels[i] == "pch" &&
                                    (user["pch"] as string == null ||
                                    user["pch"] as string == dr["pch"] as string))
                                    nodeName = nodeText = String.Format("ПЧ-{0}", dr["pch"]);
                                else if (levels[i] == "loc_description" &&
                                    (user["role_hid"] as string == null ||
                                    user["role_hid"] as string == dr["role_hid"] as string))
                                {
                                    nodeText = String.Format("{0}", dr["loc_description"]);
                                    nodeName = string.Format("{0}", dr["lochostname"]);
                                }

                                else
                                    break;

                                // проверка наличия узла с текущим именем
                                int childIndex = IndexOfNodeByName(currNodeChildren, nodeName);
                                // если узел с текущим именем существует, 
                                // то переходим на данный узел и начинаем цикл с начала
                                if (childIndex > -1)
                                {
                                    if (dr["lochostname"] != DBNull.Value && currNodeChildren[childIndex].LocHostNames.IndexOf(dr["lochostname"].ToString()) < 0)
                                        currNodeChildren[childIndex].LocHostNames.Add(dr["lochostname"].ToString());
                                    currNodeChildren = currNodeChildren[childIndex].Children;
                                    continue;
                                }
                                // если же узла с текущим именем не было найдено,
                                // то следует его создать, а после перейти к нему
                                else
                                {
                                    if (currNodeChildren == null)
                                        currNodeChildren = new ObservableCollection<Node>();

                                    Node newNode = new Node()
                                    {
                                        Name = nodeName,
                                        Text = nodeText,
                                        NodCode = levels[i] == "country" || dr["nod"] == DBNull.Value ? null : dr["nod"] as int?,
                                        PchCode = levels[i] == "nod" || levels[i] == "country" || dr["pch"] == DBNull.Value ? null : dr["pch"] as int?,
                                        LocHostNames = new System.Collections.Generic.List<string>(),
                                        Children = new ObservableCollection<Node>(),
                                        ToolTip = String.Format("{0}, t(зак)={1}", dr["lochostname"], dr["t_zak"]),
                                        BackColor = Brushes.Transparent
                                    };
                                    if (dr["lochostname"] != DBNull.Value)
                                        newNode.LocHostNames.Add(dr["lochostname"].ToString());

                                    if((dr["country"].ToString() == user["country"].ToString() || user["country"] == DBNull.Value) &&
                                        (dr["nod"].ToString() == user["nod"].ToString() || user["nod"] == DBNull.Value) &&
                                        (dr["pch"].ToString() == user["pch"].ToString() || user["pch"] == DBNull.Value))
                                        currNodeChildren.Add(newNode);

                                    currNodeChildren = newNode.Children;
                                    continue;
                                }
                            }
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message);
                    }
                }
            }
            return result;
        }
        private int IndexOfNodeByName(ObservableCollection<Node> nodes, string name)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Name == name)
                    return i;
            return -1;
        }
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow w = new SettingsWindow();
            w.TickerSpeedSlider.Value = TickerRate;
            w.TickerDirectionComboBox.SelectedIndex = (int)TickerDirection;

            w.Owner = this;

            bool? result = w.ShowDialog();

            if (result != null && result != false)
            {
                TickerRate = w.TickerSpeedSlider.Value;
                TickerDirection = (KoderHack.WPF.Controls.TickerDirection)w.TickerDirectionComboBox.SelectedIndex;

                contentTicker.Rate = TickerRate;
                contentTicker.Direction = TickerDirection;
                contentTicker.Stop();
                contentTicker.Start();
            }
        }
        private void RolesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (InfoTabControl.SelectedItem == TempTabItem)
                UpdateOxyPlot();
            else if (InfoTabControl.SelectedItem == MessagesTabItem)
                UpdateWarningDataGrid();
        }

        private void UpdateOxyPlot()
        {
            if (!IsLoaded)
                return;

            GraphMessageTextBlock.Text = "";

            if (RolesTreeView.SelectedValue == null)
            {
                GraphMessageTextBlock.Text = "Выберите роль в дереве";

                OxyGraph.Visibility = System.Windows.Visibility.Collapsed;
                TempDataGrid.Visibility = System.Windows.Visibility.Collapsed;

                return;
            }

            if (InfoTabControl.SelectedItem != TempTabItem)
                return;

            // ограничение на отрисовку графика в случае, когда пользователь не имеет подобных прав
            if (!((user["nod"] == DBNull.Value || (RolesTreeView.SelectedValue as Node).NodCode == user["nod"] as int?) &&
                (user["pch"] == DBNull.Value || (RolesTreeView.SelectedValue as Node).PchCode == user["pch"] as int?)))
            {
                GraphMessageTextBlock.Text = "Не хватает прав для просмотра данного графика";

                OxyGraph.Visibility = System.Windows.Visibility.Collapsed;
                TempDataGrid.Visibility = System.Windows.Visibility.Collapsed;

                return;
            }


            List<string> locHostNames = (RolesTreeView.SelectedValue as Node).LocHostNames;


            string periodString = "where 1=1";
            if (PeriodComboBox.SelectedItem == DayComboBoxItem)
                periodString = " where datec >= CURDATE() ";
            else if (PeriodComboBox.SelectedItem == YeasterdayComboBoxItem)
                periodString = " where datec >= (CURDATE()-1) AND datec < CURDATE() ";
            else if (PeriodComboBox.SelectedItem == WeekComboBoxItem)
                periodString = " where datec >= DATE_SUB(CURRENT_DATE, INTERVAL 7 DAY) ";
            else if (PeriodComboBox.SelectedItem == MonthComboBoxItem)
                periodString = " where datec >= DATE_SUB(CURRENT_DATE, INTERVAL 1 MONTH) ";
            else if (PeriodComboBox.SelectedItem == YearComboBoxItem)
                periodString = " where datec >= DATE_SUB(CURRENT_DATE, INTERVAL 1 YEAR) ";

            string lochostnamesStr = "";

            if(locHostNames.Count > 0)
            {
                lochostnamesStr = " and ( ";
                foreach(string lochostname in locHostNames)
                    lochostnamesStr += String.Format("a.lochostname = '{0}' or ", lochostname);
                lochostnamesStr += " 1=2 ) ";
            }

            temparchTableData.SelectStr =
                String.Format(
                @"select 
                        a.lochostname,
                        a.tempc,
                        a.datec,
                        b.cur_ip
                    from temp.temparch a
                    left join temp.locations b on a.lochostname = b.lochostname
                    {0} {1}
                    order by datec desc limit 1000",
                periodString, lochostnamesStr);

            temparchTableData.DataTable = InitTableData.FillDataTable(InitTableData.connStr, temparchTableData.SelectStr, temparchTableData.SelectParams);

            if (temparchTableData.DataTable.Rows.Count <= 0)
            {
                GraphMessageTextBlock.Text = String.Format("Нет данных за {0}", (PeriodComboBox.SelectedItem as ComboBoxItem).Content);

                OxyGraph.Visibility = System.Windows.Visibility.Collapsed;
                TempDataGrid.Visibility = System.Windows.Visibility.Collapsed;

                return;
            }

            int graphSize = TempGraphRadioButton.IsChecked == true ? 1 : 0;
            int tableSize = TempTableRadioButton.IsChecked == true ? 1 : 0;

            if (graphSize == 0 && tableSize == 0)
            {
                OxyGraph.Visibility = System.Windows.Visibility.Collapsed;
                TempDataGrid.Visibility = System.Windows.Visibility.Collapsed;

                GraphMessageTextBlock.Text = "Выберите хоть один пункт";

                return;
            }

            GraphMessageTextBlock.Text = "";

            OxyGraph.Visibility = System.Windows.Visibility.Visible;
            TempDataGrid.Visibility = System.Windows.Visibility.Visible;

            OxyGraph.Model = GetOxyPlotModel(temparchTableData.DataTable, locHostNames);
            TempDataGrid.ItemsSource = temparchTableData.DataTable.DefaultView;
        }

        private PlotModel GetOxyPlotModel(DataTable table, List<string> locHostNames)
        {
            if (table == null || table.Rows.Count <= 0 || locHostNames == null || locHostNames.Count <= 0)
                return null;

            PlotModel model = new OxyPlot.PlotModel() { Title = "График" };
            DateTimeAxis xAxis = new DateTimeAxis()
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd/MM/yyyy",
                Title = "Время",
                MinorIntervalType = DateTimeIntervalType.Hours,
                IntervalType = DateTimeIntervalType.Minutes,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = OxyPlot.LineStyle.Dash,
            };
            LinearAxis yAxis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = "Температура",
                MajorGridlineStyle = OxyPlot.LineStyle.Solid,
                MajorStep = 10,
                MinorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorStep = 1,
                IntervalLength = 1
            };

            model.Axes.Add(xAxis);
            model.Axes.Add(yAxis);

            foreach (string s in locHostNames)
            {
                model.Series.Add(new FunctionSeries());
                model.Series.Last().Title = s;
            }

           // new OxyPlot.Series.FunctionSeries();
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < locHostNames.Count; i++)
                {
                    string rowLochostname = row["lochostname"] as string;

                    if (locHostNames[i] == rowLochostname)
                        (model.Series[i] as FunctionSeries).Points.Add(new DataPoint(DateTimeAxis.ToDouble((DateTime)row["datec"]), (double)row["tempc"]));
                }
            }

            return model;
        }
        private void UpdateWarningDataGrid()
        {
            if (!this.IsLoaded)
                return;

            string whereString = "";
            if (MessagesPeriodComboBox.SelectedItem == WarningDayComboBoxItem)
                whereString = " where datem >= CURDATE() ";
            else if (MessagesPeriodComboBox.SelectedItem == WarningYesterdayComboBoxItem)
                whereString = " where datem >= (CURDATE()-1) AND datem < CURDATE() ";
            else if (MessagesPeriodComboBox.SelectedItem == WarningWeekComboBoxItem)
                whereString = " where datem >= DATE_SUB(CURRENT_DATE, INTERVAL 7 DAY) ";
            else if (MessagesPeriodComboBox.SelectedItem == WarningMonthComboBoxItem)
                whereString = " where datem >= DATE_SUB(CURRENT_DATE, INTERVAL 1 MONTH) ";
            else if (MessagesPeriodComboBox.SelectedItem == WarningYearComboBoxItem)
                whereString = " where datem >= DATE_SUB(CURRENT_DATE, INTERVAL 1 YEAR) ";
            else
                whereString = " where 1=1 ";

            if(RolesTreeView.SelectedValue == null)
            {
                MessagesInformationTextBlock.Text = "Выберите роль в дереве!";
                MessagesDataGrid.ItemsSource = null;
                MessagesDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            Node node = RolesTreeView.SelectedValue as Node;
            if (node.LocHostNames.Count > 0)
            {
                whereString += " and (";
                foreach (string lochostname in (RolesTreeView.SelectedValue as Node).LocHostNames)
                    whereString += String.Format(" lochostname = '{0}' or ", lochostname);
                whereString += " 1=2)";
            }

            DataTable bothDataTable = new DataTable();


            InitTableData curTableData = new InitTableData();
            if (UnreadMessagesRadioButton.IsChecked == true)
            {
                curTableData.SelectStr = String.Format(@"
                select 
                    a.c_message, 
                    a.datem, 
                    a.lochostname 
                from temp.exchange_cur a {0} limit 1000", whereString);
                curTableData.DataTable = InitTableData.FillDataTable(InitTableData.connStr, curTableData.SelectStr, curTableData.SelectParams);

            }
            InitTableData archTableData = new InitTableData();
            if (ReadMessagesRadioButton.IsChecked == true)
            {
                archTableData.SelectStr = String.Format(@"
                select 
                    a.c_message, 
                    a.datem, 
                    a.lochostname 
                from temp.exchange_arch a {0} limit 1000", whereString);
                archTableData.DataTable = InitTableData.FillDataTable(InitTableData.connStr, archTableData.SelectStr, archTableData.SelectParams);

            }
            if (ReadMessagesRadioButton.IsChecked != true && UnreadMessagesRadioButton.IsChecked != true)
            {
                MessagesInformationTextBlock.Text = "Выберите хоть один пункт";
                MessagesDataGrid.ItemsSource = null;
                MessagesDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }


            if (curTableData.DataTable != null && curTableData.DataTable.Columns.Count > 0)
            {
                foreach (DataColumn col in curTableData.DataTable.Columns)
                    bothDataTable.Columns.Add(col.ColumnName);

                foreach (DataRow row in curTableData.DataTable.Rows)
                {
                    DataRow newRow = bothDataTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    bothDataTable.Rows.Add(newRow);
                }
            }
            if (archTableData.DataTable != null && archTableData.DataTable.Columns.Count > 0)
            {
                if(bothDataTable.Columns.Count <= 0)
                    foreach (DataColumn col in archTableData.DataTable.Columns)
                        bothDataTable.Columns.Add(col.ColumnName);

                foreach (DataRow row in archTableData.DataTable.Rows)
                {
                    DataRow newRow = bothDataTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    bothDataTable.Rows.Add(newRow);
                }
            }

            if(bothDataTable.Rows.Count <= 0)
            {
                MessagesInformationTextBlock.Text = String.Format("За {0} нет сообщений.", (MessagesPeriodComboBox.SelectedItem as ComboBoxItem).Content);
                MessagesDataGrid.ItemsSource = null;
                MessagesDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            MessagesInformationTextBlock.Text = "";
            MessagesDataGrid.Visibility = System.Windows.Visibility.Visible;
            MessagesDataGrid.ItemsSource = bothDataTable.DefaultView;
        }
        private void InfoTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InfoTabControl.SelectedItem == TempTabItem)
                UpdateOxyPlot();
            else if (InfoTabControl.SelectedItem == MessagesTabItem)
                UpdateWarningDataGrid();
        }
        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOxyPlot();
        }
        private void ShowUsersMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowUsersWindow w = new ShowUsersWindow();
            w.Owner = this;
            w.ShowDialog();
        }
        private void WarningRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateWarningDataGrid();
        }
        private void MessagesPeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateWarningDataGrid();
        }
        private void UpdateWarningButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateWarningDataGrid();
        }

        private void GraphUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateOxyPlot();
        }
        

        private void TempGraphRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;


            int graphSize = TempGraphRadioButton.IsChecked == true ? 1 : 0;
            int tableSize = TempTableRadioButton.IsChecked == true ? 1 : 0;

            if (graphSize == 0 && tableSize == 0)
            {
                TempGrahpColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
                TempTableColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
                OxyGraph.Visibility = System.Windows.Visibility.Collapsed;
                TempDataGrid.Visibility = System.Windows.Visibility.Collapsed;

                GraphMessageTextBlock.Text = "Выберите хоть один пункт";

                return;
            }

            GraphMessageTextBlock.Text = "";

            OxyGraph.Visibility = System.Windows.Visibility.Visible;
            TempDataGrid.Visibility = System.Windows.Visibility.Visible;
            TempGrahpColumnDefinition.Width = new GridLength(graphSize, GridUnitType.Star);
            TempTableColumnDefinition.Width = new GridLength(tableSize, GridUnitType.Star);
        }

        private void ShowLocationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowLocationsWindow w = new ShowLocationsWindow();
            w.Owner = this;
            w.Show();
            UpdateAllButUser();
        } 
    }
}
