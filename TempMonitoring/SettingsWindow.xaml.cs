using System;
using System.Windows;

namespace TempMonitoring
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TickerSpeedSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(TickerSpeedSlider_ValueChanged);
            TickerSpeedTextBlock.Text = TickerSpeedSlider.Value.ToString();
        }

        private void TickerSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TickerSpeedTextBlock.Text = TickerSpeedSlider.Value.ToString();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
