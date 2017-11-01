using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Master.Master2XTypes;
using GOTSDK.Position;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ROSOnTrack_Console
{
    class GameOnTrack
    {
        public static string calibPath = AppDomain.CurrentDomain.BaseDirectory + "Calibration.xml";

        static public bool LoadCalib()
        {
            if(File.Exists(calibPath))
            {
                var doc = XDocument.Load(calibPath);
                var loadedScenarios = Scenario3DPersistence.Load(doc);

                foreach (var scenario in loadedScenarios)
                    Program.scenarios.Add(scenario);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static void Close()
        {
            // Close the Master USB connection
            if (Program.master != null)
                Program.master.Close();
        }

        internal static bool Connect()
        {
            // Try to connect. This will return false immediately if it fails to find the proper USB port.
            // Otherwise, it will begin connecting (async) and the "Master2X.OnMasterStatusChanged" event will be fired soon. 
            return Program.master.BeginConnect();
        }

        static public void Init()
        {
            Program.master = new Master2X(SynchronizationContext.Current);
            Program.master.OnMasterStatusChanged += OnMasterStatusChanged;
            Program.master.OnNewReceiverConnected += OnNewReceiverConnected;
            Program.master.OnNewTransmitterConnected += OnNewTransmitterConnected;
            Program.master.OnMasterInfoReceived += OnMasterInfoReceived;
            Program.master.OnMeasurementReceived += OnMeasurementReceived;
        }

        // Called when Master info has been received
        private static void OnMasterInfoReceived(IMaster master, string swVersion, string serialNumber)
        {
            Program.masterVersion = string.Format("{0}, Serial: {1}", swVersion, serialNumber);
        }

        // This is called whenever the master connection status has been changed.
        private static void OnMasterStatusChanged(IMaster master, MasterStatus newStatus)
        {
            Program.masterConnectionStatus = newStatus.ToString();

            if (newStatus == MasterStatus.Connected)
            {

                // Display the name of the virtual COM port
                Program.masterConnectionStatus += string.Format(" ({0})", master.CurrentPortName);

                // Request firmware version and serial.
                master.RequestMasterInfo();

                // The current air temperature
                int temperatureDegrees = 22;
                ushort speedOfSoundInMeters = (ushort)(Master2X.GetSpeedOfSoundInMeters(temperatureDegrees) + 0.5);
                ((Master2X)master).Setup(110, speedOfSoundInMeters, 16, 0, TPCLINK_ULTRASONIC_LEVEL.LEVEL_4);

                // The very last thing: Request all currently connected units. They will show up in the OnNewTransmitter/OnNewReceiver connected events.
                master.RequestUnits();
            }
        }

        // Called when a new transmitter has been connected to Master. This includes the ones used in the calibrator triangle.
        private static void OnNewTransmitterConnected(Transmitter transmitter)
        {
            if (!Program.connectedTransmitters.Any(t => t.GOTAddress == transmitter.GOTAddress))
            {
                Program.connectedTransmitters.Add(transmitter);
                Program.master.SetTransmitterState(transmitter.GOTAddress, GetTransmitterState(transmitter.GOTAddress), Transmitter.UltraSonicLevel.High);
            }
        }

        private static Transmitter.TransmitterState GetTransmitterState(GOTAddress gOTAddress)
        {
            if (CalibratorTriangle.IsCalibratorTriangleAddress(gOTAddress))
                return Transmitter.TransmitterState.Deactivated;

            return Transmitter.TransmitterState.ActiveHigh;
        }

        // Called when a new receiver has been connected to Master
        private static void OnNewReceiverConnected(Receiver receiver)
        {
            if (!Program.connectedReceivers.Any(r => r.GOTAddress == receiver.GOTAddress))
                Program.connectedReceivers.Add(receiver);
        }

        // Called when a measurement is received from the master
        private static void OnMeasurementReceived(Measurement measurement)
        {
            if (Program.scenarios.Count == 0)
            {
                Console.WriteLine("GameOnTrack: No scenario found. ");
            }
            else if (measurement.RSSI == 0)
            {
                Console.WriteLine("GameOnTrack: Transmitter radio lost. ");
            }
            else if (measurement.RxMeasurements.Count(dist => dist.Distance > 0) < 3)
            {
                Console.WriteLine("GameOnTrack: The number of receivers is less than 3. ");
            }
            else
            {
                CalculatedPosition pos;
                if (PositionCalculator.TryCalculatePosition(measurement, Program.scenarios.ToArray(), out pos))
                {
                    if( pos.TxAddress.ToString() == Program.sensor1.address.ToString() )
                    {
                        Program.sensor1.x = pos.Position.X;
                        Program.sensor1.y = pos.Position.Y;
                        Program.sensor1.z = pos.Position.Z;
                    }
                    if (pos.TxAddress.ToString() == Program.sensor2.address.ToString())
                    {
                        Program.sensor2.x = pos.Position.X;
                        Program.sensor2.y = pos.Position.Y;
                        Program.sensor2.z = pos.Position.Z;
                    }
                }
                else
                {
                    Console.WriteLine("GameOnTrack: Can not calculate the position. ");
                }
            }
        }       
    }
}
