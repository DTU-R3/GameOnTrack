using GOTSDK;
using GOTSDK.Position;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace GOTSDKSample.ExternalData
{
	class SerialDataConnection : ExternalDataConnection
	{
		SerialPort serialPort;

		public SerialDataConnection(SerialPort port)
		{
			serialPort = port;

			StatisticsHandler.Add(new ReceiverPercentages());
		}

		/// <summary>
		/// Initialize interface.
		/// </summary>
		public override void Start()
		{
			try
			{
				// Only attempt to open the serial connection if something is connected to the port.
				if (SerialPort.GetPortNames().Any(p => string.Compare(p, serialPort.PortName, true) == 0))
				{
					serialPort.Open();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
		}

		public override void Stop()
		{
			serialPort.Close();
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

		private void SendData(byte[] data)
		{
			try
			{
				serialPort.BaseStream.BeginWrite(data, 0, data.Length, null, this);
			}
			catch
			{ } // There is apparently no good way to detect if something is connected on the other side.			
		}
	}
}
