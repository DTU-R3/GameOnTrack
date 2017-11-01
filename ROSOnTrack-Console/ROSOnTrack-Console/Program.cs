using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Position;
using Ros_CSharp;
using Messages.geometry_msgs;
using Messages.std_msgs;
using System.Collections.ObjectModel;
using System.Threading;

namespace ROSOnTrack_Console
{
    class Program
    {
        #region Variables

        // Component configuration
        static public GOTData sensor1 = new GOTData();
        static public GOTData sensor2 = new GOTData();
        static public double x = 0, y = 0, theta = 0;
        static public double sensorDis = 100;
        static public int GOTAddress1 = 41269;
        static public int GOTAddress2 = 41272;

        // GameOnTrack configuration
        static public ObservableCollection<Transmitter> connectedTransmitters = new ObservableCollection<Transmitter>();
        static public ObservableCollection<Receiver> connectedReceivers = new ObservableCollection<Receiver>();
        static public ObservableCollection<Scenario3D> scenarios = new ObservableCollection<Scenario3D>();
        static public Master2X master;
        static public string masterConnectionStatus = "Offline";
        static public string masterVersion = "Unknown";

        // ROS .net variables
        static Publisher<Vector3> sensor1Pub;
        static Publisher<Vector3> sensor2Pub;
        static Publisher<Pose2D> robotPosPub;
        static Subscriber<Messages.std_msgs.Int32> addr1;
        static Subscriber<Messages.std_msgs.Int32> addr2;
        static NodeHandle nh;
        static Vector3 data1_msg = new Vector3();
        static Vector3 data2_msg = new Vector3();
        static Pose2D result_msg = new Pose2D();

        #endregion

        static void Main(string[] args)
        {
            ROS.Init(new string[0], "GameOnTrack");
            nh = new NodeHandle();
            sensor1Pub = nh.advertise<Vector3>("/got1_data", 10);
            sensor2Pub = nh.advertise<Vector3>("/got2_data", 10);
            robotPosPub = nh.advertise<Pose2D>("/got_result", 10);
            addr1 = nh.subscribe<Messages.std_msgs.Int32>("/GOT1Address", 1, Addr1CB);
            addr2 = nh.subscribe<Messages.std_msgs.Int32>("/GOT2Address", 1, Addr2CB);
            // Init component
            sensor1.address = GOTAddress1;
            sensor1.x = 0;
            sensor1.y = 0;
            sensor1.z = 0;
            sensor2.address = GOTAddress2;
            sensor2.x = 0;
            sensor2.y = 0;
            sensor2.z = 0;

            // Init GameOnTrack
            try
            {
                if (GameOnTrack.LoadCalib())
                {
                    Console.WriteLine("GameOnTrack calibration file found. ");
                    GameOnTrack.Init();
                    if (GameOnTrack.Connect())
                    {
                        Console.WriteLine("GameOnTrack master detected. ");
                    }
                    else
                    {
                        Console.WriteLine("Error: Master can not be detected. ");
                    }
                }
                else
                {
                    Console.WriteLine("Error: Can not find GameOnTrack Calibration file. ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message.ToString());
            }

            Console.WriteLine("Starts to update GameOnTrack measurements. ");
            while (true)
            {
                x = (sensor1.x + sensor2.x) / 2;
                y = (sensor1.y + sensor2.y) / 2;
                var theta_r = Math.Atan2(sensor2.y - sensor1.y, sensor2.x - sensor1.x) + Math.PI / 2;
                theta_r = FitInRadius(theta_r);
                theta = RadiusToDegree(theta_r);

                // Publish the data to ROS
                if (ROS.ok)
                {
                    data1_msg.x = Convert.ToInt32(sensor1.x);
                    data1_msg.y = Convert.ToInt32(sensor1.y);
                    data1_msg.z = Convert.ToInt32(sensor1.z);
                    Console.WriteLine("Sensor 1 data, Address {0}:", sensor1.address);
                    Console.WriteLine("\t X: {0}", data1_msg.x);
                    Console.WriteLine("\t Y: {0}", data1_msg.y);
                    Console.WriteLine("\t Z: {0}", data1_msg.z);

                    data2_msg.x = Convert.ToInt32(sensor2.x);
                    data2_msg.y = Convert.ToInt32(sensor2.y);
                    data2_msg.z = Convert.ToInt32(sensor2.z);
                    Console.WriteLine("Sensor 2 data, Address {0}:", sensor2.address);
                    Console.WriteLine("\t X: {0}", data2_msg.x);
                    Console.WriteLine("\t Y: {0}", data2_msg.y);
                    Console.WriteLine("\t Z: {0}", data2_msg.z);

                    result_msg.x = Convert.ToInt32(x);
                    result_msg.y = Convert.ToInt32(y);
                    result_msg.theta = theta_r;
                    Console.WriteLine("Robot position:");
                    Console.WriteLine("\t X: {0}", result_msg.x);
                    Console.WriteLine("\t Y: {0}", result_msg.y);
                    Console.WriteLine("\t Theta: {0}", result_msg.theta);

                    sensor1Pub.publish(data1_msg);
                    sensor2Pub.publish(data2_msg);
                    robotPosPub.publish(result_msg);
                }
                else
                {
                    Console.WriteLine("ROS is not ready");
                }

                Thread.Sleep(100);
            }          
        }

        private static void Addr1CB(Messages.std_msgs.Int32 argument)
        {
            GOTAddress1 = argument.data;
            sensor1.address = GOTAddress1;
        }

        private static void Addr2CB(Messages.std_msgs.Int32 argument)
        {
            GOTAddress2 = argument.data;
            sensor2.address = GOTAddress2;
        }     

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
