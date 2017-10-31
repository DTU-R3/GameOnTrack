using GOTSDK;
using GOTSDK.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GOTSDKSample.ExternalData
{
	class ReceiverPercentages : IStatistics
	{
		private int sentMessages = 0; // Progress towards being ready to send.
		private int statisticsInterval = 50; // Every 50 messages we send statistics data.

		private Dictionary<GOTAddress, double> measurementsPerReceiver = new Dictionary<GOTAddress, double>();

		/// <summary>
		/// Process incoming Measurement
		/// </summary>
		/// <param name="measurement">Incoming Measurement</param>
		public virtual void UpdateStatisticsData(Measurement measurement)
		{
			// Foreach receiver, add a mark. If unknown
			foreach (var item in measurement.RxMeasurements)
			{
				if (measurementsPerReceiver.ContainsKey(item.Address))
				{
					if (item.Level > 0 && item.Distance > 0) // count if valid result.
						measurementsPerReceiver[item.Address]++;
				}
				else
				{
					if (item.Level > 0 && item.Distance > 0)
						measurementsPerReceiver.Add(item.Address, 1);
					else
						measurementsPerReceiver.Add(item.Address, 0);
				}
			}

			sentMessages++;
		}

		/// <summary>
		/// We wait until enough messages have been received.
		/// </summary>
		/// <returns></returns>
		public virtual bool ReadyToSendStatistics()
		{
			return sentMessages >= statisticsInterval;
		}

		/// <summary>
		/// Generate message and reset counters.
		/// </summary>
		/// <remarks>
		/// Mesage structure: $GTRRS,MESSAGES SENT,RECEIVER COUNT,RECEIVERID|RECEIVED%,...
		/// </remarks>
		/// <returns>Statistics info ready to be sent.</returns>
		public string GetMessage()
		{
			// Send statistics message.
			string message = String.Format("$GTRRS,{0},{1},", sentMessages, measurementsPerReceiver.Count);
			message += string.Join(",", measurementsPerReceiver.Select(receiver => String.Format("{0}|{1}", receiver.Key, (int)(receiver.Value * 100 / statisticsInterval)))) + '\n';

			measurementsPerReceiver.Clear();
			sentMessages = 0;
			return message;
		}
	}

	/// <summary>
	/// This interface is used by the Network interface for statistics handling. 
	/// </summary>
	/// <remarks>
	/// Proper use:
	/// UpdateStatisticsData on each measurement
	/// IF ReadyToSendStatistics == TRUE: Send Message
	/// </remarks>
	interface IStatistics
	{
		/// <summary>
		/// Gets message string to send. Only called when ready to send, and thus can be used to reset counters
		/// </summary>
		string GetMessage();

		/// <summary>
		/// Is the implementation ready to send statistics data?
		/// </summary>
		/// <returns>True if ready to send.</returns>
		bool ReadyToSendStatistics();
		
		/// <summary>
		/// Updates the internal data with new measurement data.
		/// </summary>
		/// <param name="measurement">Newest measurement</param>
		void UpdateStatisticsData(Measurement measurement);
	}
}
