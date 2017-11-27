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
                    ClientId = "GameOnTrack/R371",
                    CleanSession = true,
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "localhost"
                        // Server = "172.30.0.1"
                    },
                };

                var factory = new MqttFactory();

                var client = factory.CreateMqttClient();

                client.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                };

                client.Connected += async (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());

                    Console.WriteLine("### SUBSCRIBED ###");
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

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                while (true)
                {
                    for (int i = 0; i < sensors.Count; i++)
                    {
                        string t = options.ClientId + "/" + sensors[i].address;

                        string d = "{x:" + Convert.ToInt32(sensors[i].x).ToString() + 
                            ",y:" + Convert.ToInt32(sensors[i].y).ToString() + 
                            ",z" + Convert.ToInt32(sensors[i].z).ToString() + "}";

                        var sensorMsg = new MqttApplicationMessageBuilder()
                            .WithTopic(t).WithPayload(d).WithAtLeastOnceQoS().Build();

                        client.PublishAsync(sensorMsg);
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
