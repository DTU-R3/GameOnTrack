using GOTSDK;
using GOTSDK.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GOTSDKSample.ExternalData
{
	abstract class ExternalDataConnection
	{
		public abstract void Start();
		public abstract void Stop();

		public abstract void SendError(ExternalDataConnection.ErrorCode error, GOTAddress transmitter);
		public abstract void SendPosition(CalculatedPosition position);
		public abstract void UpdateStatistics(Measurement measurement);

		/// <summary>
		/// This is a timestamp from when the program is initiated.
		/// </summary>
		private DateTime timeAtStart = DateTime.Now;

		/// <summary>
		/// Calculate the timespan since program was initialized.
		/// </summary>
		protected TimeSpan TimeSinceStart { get { return DateTime.Now - timeAtStart; } }

		/// <summary>
		/// List of statistics handlers.
		/// </summary>
		protected List<IStatistics> StatisticsHandler = new List<IStatistics>();

		/// <summary>
		/// Generate and send position messages.
		/// </summary>
		/// <remarks>
		/// Message structure: $GTPOS,TIME,TRANSMITTER,X,Y,Z\n
		/// </remarks>
		/// <param name="position">The scenario dependant positioning result</param>
		public string GetPosition(CalculatedPosition position)
		{
			// Send position
			return String.Format("$GTPOS,{0},{1},{2},{3},{4}\n", (int)TimeSinceStart.TotalMilliseconds, position.TxAddress, (int)position.Position.X, (int)position.Position.Y, (int)position.Position.Z);
		}

		/// <summary>
		/// Handle statistics interfacing. Update data and return if it is time to send.
		/// </summary>
		/// <param name="measurement"></param>
		protected IEnumerable<string> ProcessStatistics(Measurement measurement)
		{
			foreach (var item in StatisticsHandler)
			{
				item.UpdateStatisticsData(measurement);

				if (item.ReadyToSendStatistics())
				{
					yield return item.GetMessage();
				}
			}
		}

		/// <summary>
		/// Generate and send error messages.
		/// </summary>
		/// <remarks>
		/// Message structure: $GTERR,TIME,TRANSMITTER,ERROR\n
		/// </remarks>
		/// <param name="error">ErrorCode representing current error</param>
		/// <param name="transmitterAddress">The transmitter causing the error</param>
		public string GetError(ErrorCode error, GOTAddress transmitterAddress)
		{
			return String.Format("$GTERR,{0},{1},{2}\n", (int)(DateTime.Now - timeAtStart).TotalMilliseconds, transmitterAddress, (int)error);
		}

		public enum ErrorCode
		{
			NoCalibration = 1,
			TransmitterRadioLost = 2,
			NoUltraSound = 3,
			Other = 4
		}
	}

	static class MessageHandlerExtensions
	{
		public static void ForEach(this ExternalDataConnection[] input, Action<ExternalDataConnection> action)
		{
			for (int i = 0; i < input.Length; i++)
				action(input[i]);
		}
	}
}
