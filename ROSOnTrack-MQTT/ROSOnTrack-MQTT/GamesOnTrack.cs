using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Master.Master2XTypes;
using GOTSDK.Position;

namespace ROSOnTrack_MQTT
{
    public struct GOTObservation
    {
        public double x;
        public double y;
        public double z;
    }

    public static class GamesOnTrack
    {
        // GameOnTrack configuration
        static ObservableCollection<Transmitter> connectedTransmitters = new ObservableCollection<Transmitter>();
        static ObservableCollection<Receiver> connectedReceivers = new ObservableCollection<Receiver>();
        static ObservableCollection<Scenario3D> scenarios = new ObservableCollection<Scenario3D>();
        static Master2X master;

        static readonly string gotCalibPath = AppDomain.CurrentDomain.BaseDirectory + "Calibration.xml";

        static JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();

        public delegate void PositionEventHandler(Measurement gotMeasurement, GOTObservation gpsObservation);
        public static event PositionEventHandler OnPositionEvent;

        public static bool LoadCalib()
        {
            try
            {
                var doc = XDocument.Load(gotCalibPath);
                var loadedScenarios = Scenario3DPersistence.Load(doc);
                foreach (var scenario in loadedScenarios)
                {
                    scenarios.Add(scenario);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message.ToString());
                return false;
            }

            return true;
        }

        public static void Init()
        {
            master = new Master2X(SynchronizationContext.Current);
            master.OnMasterStatusChanged += OnMasterStatusChanged;
            master.OnMasterInfoReceived += OnMasterInfoReceived;
            master.OnMeasurementReceived += OnMeasurementReceived;
        }

        public static bool Connect()
        {
            // Try to connect. This will return false immediately if it fails to find the proper USB port.
            // Otherwise, it will begin connecting (async) and the "Master2X.OnMasterStatusChanged" event will be fired soon. 
            return master.BeginConnect();
        }

        public static void Close()
        {
            // Close the Master USB connection
            if (master != null)
            {
                master.Close();
                master = null;
            }
        }

        // Called when Master info has been received
        static void OnMasterInfoReceived(IMaster master, string swVersion, string serialNumber)
        {
            Console.Error.WriteLine("Master info: {0}, Serial: {1}", swVersion, serialNumber);
        }

        // This is called whenever the master connection status has been changed.
        static void OnMasterStatusChanged(IMaster master, MasterStatus newStatus)
        {
            Console.Error.WriteLine("Master status: {0}", newStatus);

            if (newStatus == MasterStatus.Connected)
            {
                // Display the name of the virtual COM port
                Console.Error.WriteLine("Master port: {0}", master.CurrentPortName);

                // Request firmware version and serial.
                master.RequestMasterInfo();

                // The current air temperature
                double temperatureDegrees = 22.0;
                ushort speedOfSoundInMeters = (ushort)(Master2X.GetSpeedOfSoundInMeters(temperatureDegrees) + 0.5);
                ((Master2X)master).Setup(110, speedOfSoundInMeters, 16, 0, TPCLINK_ULTRASONIC_LEVEL.LEVEL_4);
            }
        }

        // Called when a measurement is received from the master
        static void OnMeasurementReceived(Measurement measurement)
        {
            if (scenarios.Count == 0)
            {
                Console.Error.WriteLine("GameOnTrack: No scenario found.");
            }
            else if (measurement.RSSI == 0)
            {
                Console.Error.WriteLine("GameOnTrack: Transmitter radio lost for " + measurement.TxAddress);
            }
            else if (measurement.RxMeasurements.Count(dist => dist.Distance > 0) < 3)
            {
                Console.Error.WriteLine("GameOnTrack: The number of receivers is less than 3.");
            }
            else
            {
                Console.Error.WriteLine("Address: {0}, RSSI: {1}", measurement.TxAddress, measurement.RSSI);
                CalculatedPosition pos;
                if (PositionCalculator.TryCalculatePosition(measurement, scenarios.ToArray(), out pos))
                {
                    GOTObservation got_pos = new GOTObservation();
                    got_pos.x = pos.Position.X / 1000.0;
                    got_pos.y = pos.Position.Y / 1000.0;
                    got_pos.z = pos.Position.Z / 1000.0;
                    OnPositionEvent?.Invoke(measurement, got_pos);
                }
                else
                {
                    Console.Error.WriteLine("GameOnTrack: Can not calculate the position.");
                }
            }
        }       
    }
}
