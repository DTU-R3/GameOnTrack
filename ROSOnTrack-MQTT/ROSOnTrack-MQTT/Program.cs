using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MQTTnet;
using MQTTnet.Core;
using MQTTnet.Core.Client;

namespace ROSOnTrack_MQTT
{
    sealed class Program
    {
        static IMqttClient mqttClient = null;
        static JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Coordinate transformation from XYZ to GPS
        /// </summary>
        /// <param name="args"></param>
        static async Task MainAsync(string[] args)
        {
            // Init GameOnTrack
            try
            {
                if (GamesOnTrack.LoadCalib())
                {
                    Console.Error.WriteLine("GameOnTrack calibration file found. ");
                    GamesOnTrack.Init();
                    if (GamesOnTrack.Connect())
                    {
                        Console.Error.WriteLine("GameOnTrack master detected. ");
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: Master can not be detected. ");
                        return;
                    }
                }
                else
                {
                    Console.Error.WriteLine("Error: Can not find GameOnTrack Calibration file. ");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message.ToString());
                return;
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
                        // Server = "localhost",
                        Server = "172.30.0.1",
                    },
                };

                var factory = new MqttFactory();

                mqttClient = factory.CreateMqttClient();

                mqttClient.Connected += (s, e) =>
                {
                    Console.Error.WriteLine("### CONNECTED WITH SERVER ###");
                };

                mqttClient.Disconnected += async (s, e) =>
                {
                    Console.Error.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await mqttClient.ConnectAsync(options);
                    }
                    catch
                    {
                        Console.Error.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await mqttClient.ConnectAsync(options);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }

            Console.Error.WriteLine("### START PUBLISHING GAMEONTRACK DATA ###");
            GamesOnTrack.OnPositionEvent += GamesOnTrack_OnPositionEvent;

            Console.ReadLine();
        }

        static void GamesOnTrack_OnPositionEvent(GOTSDK.Measurement gotMeasurement, GPSObservation gpsObservation)
        {
            string topic = "/GamesOnTrack/R232" + "/" + gotMeasurement.TxAddress;
            var data = javaScriptSerializer.Serialize(gpsObservation);
            var sensorMsg = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(data).Build();
            mqttClient.PublishAsync(sensorMsg);
            Console.WriteLine(data);
        }
    }
}
