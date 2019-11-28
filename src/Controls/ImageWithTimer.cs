using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace spiw.Controls
{
    internal class ImageWithTimer : Image
    {
        private DispatcherTimer CommandTriggerTimer { get; set; }

        public ImageWithTimer()
        {
            CommandTriggerTimer = new DispatcherTimer(DispatcherPriority.Normal);
            CommandTriggerTimer.Tick += CommandTriggerTimerCallback;
        }

        private void CommandTriggerTimerCallback(object sender, EventArgs e)
        {
            TimerCommand?.Execute(null);
        }

        public static readonly DependencyProperty TimerIntervalProperty = 
            DependencyProperty.Register(
                "TimerInterval",
                typeof(int),
                typeof(ImageWithTimer),
                new PropertyMetadata(0));

        public int TimerInterval
        {
            get { return (int)GetValue(TimerIntervalProperty); }
            set { SetValue(TimerIntervalProperty, value); }
        }

        public static readonly DependencyProperty IsTimerEnabledProperty = 
            DependencyProperty.Register(
                "IsTimerEnabled",
                typeof(bool),
                typeof(ImageWithTimer),
                new PropertyMetadata(false, new PropertyChangedCallback(IsTimerEnabledPropertyPropertyChanged)));

        public bool IsTimerEnabled
        {
            get { return (bool)GetValue(IsTimerEnabledProperty); }
            set { SetValue(IsTimerEnabledProperty, value); }
        }

        private static void IsTimerEnabledPropertyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ImageWithTimer image)) return;

            if (e.NewValue != null)
            {
                if ((bool)e.NewValue)
                {
                    image.StartTimer();
                }
                else
                {
                    image.StopTimer();
                }
            }
        }

        public static readonly DependencyProperty TimerCommandProperty = 
            DependencyProperty.Register(
                "TimerCommand",
                typeof(ICommand),
                typeof(ImageWithTimer),
                new PropertyMetadata(null));

        public ICommand TimerCommand
        {
            get { return (ICommand)GetValue(TimerCommandProperty); }
            set { SetValue(TimerCommandProperty, value); }
        }

        public void StartTimer()
        {
            CommandTriggerTimer.Interval = new TimeSpan(0, 0, 0, 0, TimerInterval);
            CommandTriggerTimer.Start();
        }

        public void StopTimer()
        {
            CommandTriggerTimer.Stop();
        }
    }
}
