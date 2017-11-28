using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GOTSDK;
using GOTSDK.Master;
using GOTSDK.Position;
using System.Collections.ObjectModel;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet;
using System.Globalization;

namespace ROSOnTrack_MQTT
{
    class Program
    {
        // GameOnTrack configuration
        static public ObservableCollection<Transmitter> connectedTransmitters = new ObservableCollection<Transmitter>();
        static public ObservableCollection<Receiver> connectedReceivers = new ObservableCollection<Receiver>();
        static public ObservableCollection<Scenario3D> scenarios = new ObservableCollection<Scenario3D>();
        static public Master2X master;
        static public string masterConnectionStatus = "Offline";
        static public string masterVersion = "Unknown";
        static public List<GOTData> sensors = new List<GOTData>();

        static void Main(string[] args)
        {
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

            // Init MQTT
            try
            {
                var options = new MqttClientOptions
                {
                    ClientId = "/GamesOnTrack/R232",
                    CleanSession = true,
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        // Server = "localhost"
                        Server = "172.30.0.1"
                    },
                };

                var factory = new MqttFactory();

                var client = factory.CreateMqttClient();

                client.Connected += (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");
                };

                client.Disconnected += async (s, e) =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await client.ConnectAsync(options);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    client.ConnectAsync(options);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### START PUBLISHING GAMEONTRACK DATA ###");

                while (true)
                {
                    for (int i = 0; i < sensors.Count; i++)
                    {
                        string t = options.ClientId + "/" + sensors[i].address;

                        var d = String.Format(CultureInfo.InvariantCulture,
                                "{{\"x\":{0},\"y\":{1},\"z\":{2}}}",
                                Math.Round(sensors[i].x),
                                Math.Round(sensors[i].y),
                                Math.Round(sensors[i].z));

                        var sensorMsg = new MqttApplicationMessageBuilder()
                            .WithTopic(t).WithPayload(d).WithAtLeastOnceQoS().Build();

                        client.PublishAsync(sensorMsg);
                        Console.WriteLine(t+" "+d);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

    }
}
