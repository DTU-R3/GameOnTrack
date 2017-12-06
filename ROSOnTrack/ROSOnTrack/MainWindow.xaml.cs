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
using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Position;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Ros_CSharp;
using Messages.geometry_msgs;


namespace ROSOnTrack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables

        // Component configuration
        static public GOTData sensor1 = new GOTData();
        static public GOTData sensor2 = new GOTData();
        static public double x = 0, y = 0, theta = 0;
        static public double sensorDis = 100;
        static public int GOTAddress1 = 41267;
        static public int GOTAddress2 = 41272;

        // GameOnTrack configuration
        static public ObservableCollection<Transmitter> connectedTransmitters = new ObservableCollection<Transmitter>();
        static public ObservableCollection<Receiver> connectedReceivers = new ObservableCollection<Receiver>();
        static public ObservableCollection<Scenario3D> scenarios = new ObservableCollection<Scenario3D>();
        static public Master2X master;
        static public string masterConnectionStatus = "Offline";
        static public string masterVersion = "Unknown";

        // Timer
        DispatcherTimer timer = new DispatcherTimer();

        #endregion

        #region Constructior

        public MainWindow()
        {
            InitializeComponent();
            Log.Init();
        }

        #endregion

        #region Window Event

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Init component
            sensor1.address = GOTAddress1;
            sensor1.x = 0;
            sensor1.y = 50;
            sensor1.z = 0;
            sensor2.address = GOTAddress2;
            sensor2.x = 0;
            sensor2.y = -50;
            sensor2.z = 0;
            SensorDisTxt.Text = sensorDis.ToString();

            // Init GameOnTrack
            try
            {
                if (GameOnTrack.LoadCalib())
                {
                    ShowInfo("GameOnTrack calibration file found. ");
                    GameOnTrack.Init();
                    if (GameOnTrack.Connect())
                    {
                        ShowInfo("GameOnTrack master detected. ");
                        MasterTxt.Text = "Connected";
                    }
                    else
                    {
                        ShowInfo("GameOnTrack master can not be detected. ");
                        Log.SetLog("Error: Master can not be detected. ");
                    }
                }
                else
                {
                    ShowInfo("Can not find GameOnTrack Calibration file. ");
                    Log.SetLog("Error: Can not find GameOnTrack Calibration file. ");
                }
            }
            catch (Exception ex)
            {
                Log.SetLog("Exception: " + ex.Message.ToString());
                ShowInfo("Error: " + ex.Message.ToString());
            }

            // Start the timer, every 100 ms.
            try
            {
                timer.Tick += new EventHandler(Timer_tick);
                timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                timer.Start();
                ShowInfo("Timer starts to update GameOnTrack measure. ");
            }
            catch (Exception ex)
            {
                Log.SetLog("Exception: " + ex.Message.ToString());
                ShowInfo("Error: " + ex.Message.ToString());
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                GameOnTrack.Close();
            }
            catch (Exception ex)
            {
                Log.SetLog("Exception: " + ex.Message.ToString());
            }
        }

        #endregion

        #region Show information

        public void ShowInfo(string text)
        {
            sysInfoTxt.AppendText(DateTime.Now.ToString() + ": " + text + "\r\n");
            sysInfoTxt.ScrollToEnd();
        }

        #endregion

        #region Timer Click

        private void Timer_tick(object sender, EventArgs e)
        {
            // Display measurement to the interface and caculcate robot pose
            Sender1_xTxt.Text = Convert.ToInt32(sensor1.x).ToString();
            Sender1_yTxt.Text = Convert.ToInt32(sensor1.y).ToString();
            Sender1_zTxt.Text = Convert.ToInt32(sensor1.z).ToString();

            Sender2_xTxt.Text = Convert.ToInt32(sensor2.x).ToString();
            Sender2_yTxt.Text = Convert.ToInt32(sensor2.y).ToString();
            Sender2_zTxt.Text = Convert.ToInt32(sensor2.z).ToString();

            x = (sensor1.x + sensor2.x) / 2;
            y = (sensor1.y + sensor2.y) / 2;
            var theta_r = Math.Atan2(sensor2.y - sensor1.y, sensor2.x - sensor1.x) + Math.PI / 2;
            theta_r = FitInRadius(theta_r);
            theta = RadiusToDegree(theta_r);

            Rob_xTxt.Text = Convert.ToInt32(x).ToString();
            Rob_yTxt.Text = Convert.ToInt32(y).ToString();
            Rob_thTxt.Text = Convert.ToInt32(theta).ToString();
            Rob_thrTxt.Text = theta_r.ToString("0.00");
        }

        private void Sender1Btn_Click(object sender, RoutedEventArgs e)
        {
            GOTAddress1 = Int32.Parse(Sender1_addrTxt.Text);
            sensor1.address = GOTAddress1;
        }

        private void Sender2Btn_Click(object sender, RoutedEventArgs e)
        {
            GOTAddress2 = Int32.Parse(Sender2_addrTxt.Text);
            sensor2.address = GOTAddress2;
        }

        private void RobotBtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            sensorDis = Int32.Parse(SensorDisTxt.Text);
        }

        #endregion

        #region Math function

        private static double FitInRadius(double r)
        {
            while (r < -Math.PI)
            {
                r = r + 2 * Math.PI;
            }
            while (r > Math.PI)
            {
                r = r - 2 * Math.PI;
            }
            return r;
        }

        private static double RadiusToDegree(double r)
        {
            var d = (r / Math.PI) * 180;
            return d;
        }

        #endregion
    }
}
