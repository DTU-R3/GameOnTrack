using GOTSDK;
using GOTSDK.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GOTSDKSample.ExternalData
{
	class TCPDataConnection : ExternalDataConnection
	{
		/// <summary>
		/// This is true from the moment Start is run, until the program is terminated.
		/// </summary>
		private bool isRunning = false;

		/// <summary>
		/// TCP listener listening for new clients signing up for data transmissions.
		/// </summary>
		private TcpListener listener;

		/// <summary>
		/// The thread handler where listener listens for incoming connections
		/// </summary>
		private Thread listenerThread;

		private object connectionsLockObj = new object();
		private HashSet<TcpClient> connections = new HashSet<TcpClient>();

		/// <summary>
		/// Initializes a new instance of the <see cref="TCPDataConnection"/> class.
		/// </summary>
		/// <param name="port">The port.</param>
		public TCPDataConnection(int portNumber)
		{
			listener = new TcpListener(IPAddress.Any, portNumber);
			listenerThread = new Thread(StartListening) { Name = "NetworkInterface listener", IsBackground = true };

			StatisticsHandler.Add(new ReceiverPercentages());
		}

		/// <summary>
		/// Initialize interface.
		/// </summary>
		/// <param name="port">port to start sending on. Default = 26517</param>
		public override void Start()
		{
			listenerThread.Start();
			isRunning = true;
		}

		public override void Stop()
		{
			isRunning = false;
		}

		public override void SendError(ExternalDataConnection.ErrorCode error, GOTAddress transmitter)
		{
			SendData(Encoding.ASCII.GetBytes(GetError(error, transmitter)));
		}

		public override void SendPosition(CalculatedPosition position)
		{
			SendData(Encoding.ASCII.GetBytes(GetPosition(position)));
		}

		public override void UpdateStatistics(Measurement measurement)
		{
			var stats = ProcessStatistics(measurement);
			foreach (var item in stats)
			{
				SendData(Encoding.ASCII.GetBytes(item));
			}
		}

		private void StartListening(object notUsed)
		{
			try
			{
				listener.Start();

				while (isRunning)
				{
					while (!listener.Pending())
					{
						Thread.Sleep(20);

						if (!isRunning)
							return;
						else
							continue;
					}

					// Block and wait for a new connection
					var connection = listener.AcceptTcpClient();

					lock (connectionsLockObj)
					{
						connections.Add(connection);
					}
				}
			}
			catch
			{
			}
			finally
			{
				try
				{
					listener.Stop();
				}
				catch
				{

				}
				finally
				{
					lock (connectionsLockObj)
					{
						foreach (var client in connections)
						{
							try
							{
								client.Close();
							}
							catch { }
						}

						connections.Clear();
					}
				}
			}
		}

		private void SendData(byte[] data)
		{
			foreach (var client in connections)
			{
				try
				{
					var stream = client.GetStream();
					stream.Write(data, 0, data.Length);
				}
				catch
				{
					try
					{
						client.Close();
					}
					catch { }
				}
			}

			// Remove broken connections
			if (connections.Any(c => !c.Connected))
			{
				lock (connectionsLockObj)
				{
					var aliveConnections = connections.Where(c => c.Connected).ToArray();
					connections.Clear();

					foreach (var item in aliveConnections)
						connections.Add(item);
				}
			}
		}
	}
}
