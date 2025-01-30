using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ANS.ViewModel
{
    public class VMmainWindow : INotifyPropertyChanged
    {
        private DateTime _currentTime;
        private DispatcherTimer _timer;

        public VMmainWindow()
        {

            _currentTime = DateTime.Now;

            _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now;
            };

            _timer.Start();

        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
