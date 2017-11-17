using System;
using System.Collections.Generic;
using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Position;
using Ros_CSharp;
using Messages.geometry_msgs;
using System.Collections.ObjectModel;
using System.Threading;

namespace ROSOnTrack_Console
{
    class Program
    {
        #region Variables

        // GameOnTrack configuration
        static public ObservableCollection<Transmitter> connectedTransmitters = new ObservableCollection<Transmitter>();
        static public ObservableCollection<Receiver> connectedReceivers = new ObservableCollection<Receiver>();
        static public ObservableCollection<Scenario3D> scenarios = new ObservableCollection<Scenario3D>();
        static public Master2X master;
        static public string masterConnectionStatus = "Offline";
        static public string masterVersion = "Unknown";
        static public List<GOTData> sensors = new List<GOTData>();

        // ROS .net variables
        static public NodeHandle nh;
        static public List<Publisher<Vector3>> publishers = new List<Publisher<Vector3>>();

        #endregion

        static void Main(string[] args)
        {
            ROS.Init(new string[0], "GameOnTrack");
            nh = new NodeHandle();

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

            for (int i=0; i<sensors.Count; i++)
            {
                publishers[i] = nh.advertise<Vector3>("/GameOnTrack/"+sensors[i].address, 10);
            }

            Console.WriteLine("Starts to update GameOnTrack measurements. ");
            while (true)
            {
                // Publish the data to ROS
                if (ROS.ok)
                {
                    for (int i = 0; i < sensors.Count; i++)
                    {
                        Vector3 sensorData = new Vector3();
                        sensorData.x = Convert.ToInt32(sensors[i].x);
                        sensorData.y = Convert.ToInt32(sensors[i].y);
                        sensorData.z = Convert.ToInt32(sensors[i].z);

                        publishers[i].publish(sensorData);
                    }
                }
                else
                {
                    Console.WriteLine("ROS is not ready");
                }

                Thread.Sleep(100);
            }          
        } 
    }
}
