using ANS.Model;
using MaterialDesignThemes.Wpf;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ANS
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        public MainWindow()
        {
            InitializeComponent();

            JobSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            ConfigurarRelojes();
        }
        private void ConfigurarRelojes()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) =>
            {
                ActualizarRelojDigital();
                ActualizarManecillas();
            };

            _timer.Start();

            ActualizarRelojDigital();
            ActualizarManecillas();
        }
        private void ActualizarRelojDigital()
        {
            DigitalClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        }
        private void ActualizarManecillas()
        {
            var now = DateTime.Now;

            double centerX = 150, centerY = 150;
            double hourLength = 50, minuteLength = 70, secondLength = 90;

            double hourAngle = (now.Hour % 12 + now.Minute / 60.0) * 30 * Math.PI / 180;
            double minuteAngle = (now.Minute + now.Second / 60.0) * 6 * Math.PI / 180;
            double secondAngle = now.Second * 6 * Math.PI / 180;

            HourHand.X1 = centerX;
            HourHand.Y1 = centerY;
            HourHand.X2 = centerX + hourLength * Math.Sin(hourAngle);
            HourHand.Y2 = centerY - hourLength * Math.Cos(hourAngle);

            MinuteHand.X1 = centerX;
            MinuteHand.Y1 = centerY;
            MinuteHand.X2 = centerX + minuteLength * Math.Sin(minuteAngle);
            MinuteHand.Y2 = centerY - minuteLength * Math.Cos(minuteAngle);

            SecondHand.X1 = centerX;
            SecondHand.Y1 = centerY;
            SecondHand.X2 = centerX + secondLength * Math.Sin(secondAngle);
            SecondHand.Y2 = centerY - secondLength * Math.Cos(secondAngle);
        }
        public void MostrarAviso(string msg, System.Windows.Media.Color color)
        {
            var model = new SnackbarMsg
            {
                Texto = msg,
                Fondo = new SolidColorBrush(color)
            };
            JobSnackbar.MessageQueue?.Enqueue(model);
        }

    }
}
