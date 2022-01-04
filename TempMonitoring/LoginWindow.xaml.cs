using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Data;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public InitTableData tableInfo;

        public List<DataRow> matchedUsers;
        public LoginWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoginTextBox.Focus();

            tableInfo = (Owner as MainWindow).usersTableData;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

            if (LoginTextBox.Text == "")
            {
                PasswordErrorLabel.Text = "";
                LoginErrorLabel.Text = "Введите логин. Без пароля нельзя войти в систему.";
                PasswordBox.Password = "";
                LoginTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                PasswordBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                return;
            }

            bool isPasswordMatch = false;
            bool isLoginMatch = false;
            bool isDeletedMatch = false;

            matchedUsers = new List<DataRow>();

            foreach (DataRow row in tableInfo.DataTable.Rows)
            {
                if (Convert.ToString(row["login"]).Equals(LoginTextBox.Text))
                {
                    isLoginMatch = true;
                    if (row["deleted"] == DBNull.Value)
                    {
                        isDeletedMatch = true;
                        if (Convert.ToString(row["password"]).Equals(PasswordBox.Password))
                        {
                            isPasswordMatch = true;
                            matchedUsers.Add(row);
                        }
                    }
                }
            }


            if (!isLoginMatch)
            {
                LoginErrorLabel.Text = "Пользователя с таким логином не существует! Попробуйте еще раз.";
                PasswordErrorLabel.Text = "";
                PasswordBox.Password = "";
                LoginTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                PasswordBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                return;
            }
            else if (isLoginMatch && !isDeletedMatch)
            {
                LoginErrorLabel.Text = "Пользователя с таким логином был удален! Введите логин другого пользователя.";
                PasswordErrorLabel.Text = "";
                PasswordBox.Password = "";
                LoginTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                PasswordBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                return;
            }
            else if (isLoginMatch && isDeletedMatch && !isPasswordMatch)
            {
                LoginErrorLabel.Text = "";
                PasswordErrorLabel.Text = "Пароль не подходит.";
                PasswordBox.Password = "";
                LoginTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                PasswordBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
