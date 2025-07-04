﻿using ANS.Model;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ANS
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private VMmainWindow _viewModel;
        public MainWindow()
        {

            InitializeComponent();

            _viewModel = new VMmainWindow();

            DataContext = _viewModel;

            JobSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

        }
        public void MostrarAviso(string msg, System.Windows.Media.Color color)
        {
            if (this.WindowState == WindowState.Minimized)
                return;


            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => MostrarAviso(msg, color));
                return;
            }

            var model = new SnackbarMsg

            {
                Texto = msg,

                Fondo = new SolidColorBrush(color)
            };

            Console.WriteLine("aqui se encoló el msg: " + msg);

            JobSnackbar.MessageQueue?.Enqueue(model);

        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var seleccionDeBanco = new SeleccionDeBanco();

            seleccionDeBanco.ShowDialog();
        }
        // Implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Mostrar diálogo de confirmación
            var result = MessageBox.Show(
                "¿Está seguro de que desea cerrar? Se dejará de acreditar a los bancos.",
                "Confirmar cierre",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // Si el usuario elige No, cancelar el cierre
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            // Si elige Yes, e.Cancel queda en false y la ventana se cierra
        }


    }
}
