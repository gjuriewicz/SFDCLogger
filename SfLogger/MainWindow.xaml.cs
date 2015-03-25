using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
namespace SfLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<string> Logs { get; set; }
        public bool onTop { get; set; }
        private SFConnection sfConnection;
        public DateTime lastTime;
        System.Windows.Threading.DispatcherTimer refreshTimer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow(SFConnection connection)
        {
            InitializeComponent();
            this.DataContext = this;
            Logs = new ObservableCollection<string>();
            sfConnection = connection;
            sfConnection.Connected += delegate {
                ResetLoading();
            };
            sfConnection.Initialize();
        }


        private void fetchBtn_Click(object sender, RoutedEventArgs e)
        {
            GetLogs();
        }

        void TimerBtn_Click(object sender, EventArgs e)
        {


            if (TimerBtn.Content.ToString() == "Start Timer") 
            { TimerBtn.Content = "Stop Timer"; 
                TimerBtn.Background = Brushes.LightGreen;
                refreshTimer.Tick += new EventHandler(timer_Tick);
                refreshTimer.Interval = new TimeSpan(0, 0, int.Parse(refTime.Text));
                refreshTimer.Start();
            }
            else 
            { 
                TimerBtn.Content = "Start Timer";  var bc = new BrushConverter();
                TimerBtn.Background = (Brush)bc.ConvertFrom("#FF292929");
                refreshTimer.Stop();

            }

        }

        void timer_Tick(object sender, EventArgs e)
        {
            GetLogs();
        }

      
        private void registerBtn_Click(object sender, RoutedEventArgs e)
        {
            Loading();
            sfConnection.RegisterUser();
        }

        private void Loading()
        {
            ellipse.Visibility = System.Windows.Visibility.Visible;
            fetchBtn.IsEnabled = false;
            registerBtn.IsEnabled = false;
        }

        private void ResetLoading()
        {
            ellipse.Visibility = System.Windows.Visibility.Hidden;
            fetchBtn.IsEnabled = true;
            registerBtn.IsEnabled = true;
        }

        private void onTopBox_Checked(object sender, RoutedEventArgs e)
        {
            Window window = (Window)this;
            window.Topmost = onTop;
        }


        private async void GetLogs()
        {
            Loading();

            try
            {
                foreach (var log in await sfConnection.QueryLogs())
                {
                    if (log.Contains("***") == false)
                    {
                        String[] getDate = log.Split('(');
                        List<String> timeData = getDate[0].ToString().Split(':').ToList<String>();

                        DateTime nowTime = new DateTime(2015, 1, 1, int.Parse(timeData[0].ToString()) + 1,
                                                                    int.Parse(timeData[1].ToString()),
                                                                    int.Parse(timeData[2].ToString().Substring(0, 2)));

                        if (TimeSpan.Compare(nowTime.TimeOfDay, lastTime.TimeOfDay) > 0)
                        {
                            if (lastTime.TimeOfDay.ToString() == "00:00:00")
                            {
                                Logs.Add(log);
                            }
                            else
                            {
                                Logs.Insert(0, log);
                            }
                        }
                    }
                    else
                    {
                        //  Logs.Add(log);
                    }
                }
                lastTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nPlease check if your log list contains any records");
            }
        }


    }
}
