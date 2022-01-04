using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KoderHack.Demo
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sliderText.Text = sampleText.Text;
            contentTicker.Rate = speedSlider.Value;
        }

        private void toggleDirection_Click(object sender, RoutedEventArgs e)
        {
            contentTicker.Direction = contentTicker.Direction == WPF.Controls.TickerDirection.East ? WPF.Controls.TickerDirection.West : WPF.Controls.TickerDirection.East;
            Restart();
        }

        private void updateTicker_Click(object sender, RoutedEventArgs e)
        {
            sliderText.Text = sampleText.Text;
            contentTicker.Rate = speedSlider.Value;

            Restart();
        }

        void Restart()
        {
            contentTicker.Stop();
            contentTicker.Start();
        }
    }
}
