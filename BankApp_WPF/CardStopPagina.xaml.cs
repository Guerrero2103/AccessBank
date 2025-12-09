using System;
using System.Windows;
using System.Windows.Input;

namespace BankApp_WPF
{
    public partial class CardStopPagina : Window
    {
        public CardStopPagina()
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Focusable = true;
            this.Focus();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                e.Handled = true;
                StartPagina startPagina = new StartPagina();
                startPagina.Show();
                this.Close();

            }
        }

       
        private void BtnTerug_Click(object sender, RoutedEventArgs e)
        {
            StartPagina startPagina = new StartPagina();
            startPagina.Show();
            this.Close();
        }
    }
}