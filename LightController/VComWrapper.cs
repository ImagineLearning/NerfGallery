using System;
using System.Collections.Generic;
using System.Text;

namespace basic_light_board
{
	public enum DMXProMsgLabel
	{
		REPROGRAM_FIRMWARE_REQUEST = 1,
		PROGRAM_FLASH_PAGE_REQUEST = 2,
		PROGRAM_FLASH_PAGE_REPLY = 2,
		GET_WIDGET_PARAMETERS_REQUEST = 3,
		GET_WIDGET_PARAMETERS_REPLY = 3,
		SET_WIDGET_PARAMETERS_REQUEST = 4,
		SET_WIDGET_PARAMETERS_REPLY = 4,
		RECEIVED_DMX_PACKET = 5,
		OUTPUT_ONLY_SEND_DMX_PACKET_REQUEST = 6,
		SEND_RDM_PACKET_REQUEST = 7,
		RECEIVE_DMX_ON_CHANGE = 8,
		RECEIVED_DMX_CHANGE_OF_STATE_PACKET = 9,
		GET_WIDGET_SERIAL_NUMBER_REQUEST = 10,
		GET_WIDGET_SERIAL_NUMBER_REPLY = 10,
		SEND_RDM_DISCOVERY_REQUEST = 11
	}
	class DMXMessage : EventArgs
	{
		public DMXProMsgLabel type;
		public byte[] message;

		public DMXMessage(DMXProMsgLabel t, byte[] m)
		{
			message = m;
			type = t;
		}
	}

	class WidgetParameterArgs : EventArgs
	{
		public UInt16 Firmware;
		public double DMXOutBreakTime;
		public double DMXOutMarkTime;
		public int DMXOutRate;
		public byte[] UserConfigData;

		public WidgetParameterArgs(UInt16 dmxFirmware, double dmxOutBreakTime, double dmxOutMarkTime, int dmxOutRate, byte[] ConfigData)
		{
			Firmware = dmxFirmware;
			DMXOutBreakTime = dmxOutBreakTime;
			DMXOutMarkTime = dmxOutMarkTime;
			DMXOutRate = dmxOutRate;
			UserConfigData = ConfigData;
		}
	}
	class SerialNumberArgs : EventArgs
	{
		public UInt32 SerialNumber;
		public SerialNumberArgs(UInt32 serial)
		{
			SerialNumber = serial;
		}
	}
	class DMXLevelArgs : EventArgs
	{
		public bool valid;
		public byte[] levels;
		public DMXLevelArgs(bool isValid, byte[] lvls)
		{
			valid = isValid;
			levels = lvls;
		}
	}
	class FlashReplyArgs : EventArgs
	{
		public bool success;
		public FlashReplyArgs(bool s)
		{
			success = s;
		}
	}



	class VComWrapper
	{
		public System.IO.Ports.SerialPort m_port;
		public const byte msgStart = 0x7e;
		public const byte msgEnd = 0xe7;

		public event EventHandler<WidgetParameterArgs> WidgetParametersReceived;
		public event EventHandler<SerialNumberArgs> SerialNumberReceived;
		public event EventHandler<FlashReplyArgs> FlashReplyRecieved;
		public event EventHandler<DMXLevelArgs> DMXLevelsRecieved;


		public VComWrapper()
		{
			m_port = new System.IO.Ports.SerialPort();
			m_port.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(m_port_DataReceived);
			m_port.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(m_port_ErrorReceived);
			m_port.Encoding = Encoding.UTF8;
		}


		#region static methods
		public static string[] comList;
		public static void getWidgetList()
		{
			//setup the dummy Com Port
			System.IO.Ports.SerialPort temp = new System.IO.Ports.SerialPort();
			temp.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(temp_DataReceived);
			temp.Encoding = Encoding.UTF8;

			//try to send a get widget parameters to each port.
			string[] possiblePorts = System.IO.Ports.SerialPort.GetPortNames();
			foreach (string p in possiblePorts)
			{
				temp.PortName = p;
				temp.Open();
				VComWrapper.sendMsg(temp, DMXProMsgLabel.GET_WIDGET_PARAMETERS_REQUEST, new byte[2] { 0, 0 });

			}
		}

		static void temp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
		{

			throw new NotImplementedException();
		}





		#endregion

		public bool initPro(string portName)
		{
			if (!m_port.IsOpen)
			{
				m_port.Close();
			}
			try
			{
				m_port.PortName = portName;
				m_port.Open();
				return true;
			}
			catch //(Exception ex)
			{
				//throw new Exception(string.Format("Failed to open USB DMX Pro on comm port: {0} - Check Settings, Device",m_port.PortName),ex);
				return false;
			}
		}
		public void detatchPro()
		{
			if (m_port.IsOpen)
				m_port.Close();
		}
		public bool IsOpen { get { return m_port.IsOpen; } }

		void m_port_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
		{
			throw new System.NotImplementedException();
		}
		void m_port_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
		{
			byte[] header = new byte[4];
			byte[] message;
			byte footer;
			int length;
			m_port.Read(header, 0, 4);
			if (header[0] != VComWrapper.msgStart) return;
			length = header[2] | (header[3] << 8);
			message = new byte[length];
			m_port.Read(message, 0, length);
			//            footer = new byte[1];
			footer = (byte)m_port.ReadByte();
			if (footer != VComWrapper.msgEnd) return;
			decodeMessage((DMXProMsgLabel)header[1], message);
		}

		public void decodeMessage(DMXProMsgLabel label, byte[] msg)
		{
			int len;
			switch (label)
			{
				case DMXProMsgLabel.GET_WIDGET_PARAMETERS_REPLY: //3
					if (msg.Length < 5) return;
					UInt16 Firmware = (UInt16)(msg[0] | (msg[1] << 8));
					double DMXOutBreakTime = 10.67 * msg[2];
					double DMXOutMarkTime = 10.67 * msg[3];
					int DMXOutRate = msg[4];

					len = msg.Length - 5;
					byte[] UserConfigData = new byte[len];
					Array.Copy(msg, 5, UserConfigData, 0, len);
					if (WidgetParametersReceived != null) WidgetParametersReceived(this, new WidgetParameterArgs(Firmware, DMXOutBreakTime, DMXOutMarkTime, DMXOutRate, UserConfigData));
					break;
				case DMXProMsgLabel.GET_WIDGET_SERIAL_NUMBER_REPLY: //10
					UInt32 SerialNumber;
					SerialNumber = (UInt32)(msg[0] | (msg[1] << 8) | (msg[2] << 16) | (msg[3] << 24));
					if (SerialNumberReceived != null) SerialNumberReceived(this, new SerialNumberArgs(SerialNumber));
					break;
				case DMXProMsgLabel.PROGRAM_FLASH_PAGE_REPLY:
					bool success;
					string result = Encoding.UTF8.GetString(msg, 0, 4);
					if (result == "TRUE") success = true;
					else if (result == "FALSE") success = false;
					else throw new Exception(string.Format("Program Flash Page Responded with neither TRUE nor FALSE({0})", result));

					if (FlashReplyRecieved != null) FlashReplyRecieved(this, new FlashReplyArgs(success));
					break;
				case DMXProMsgLabel.RECEIVED_DMX_CHANGE_OF_STATE_PACKET:
					throw new NotImplementedException("Received DMX Change of State Packet is more effort than i want to put in at 12:26");
				//break;
				case DMXProMsgLabel.RECEIVED_DMX_PACKET:
					/*The Widget sends this message to the PC unsolicited, 
					 * whenever the Widget receives a DMX or
					 * RDM packet from the DMX port, 
					 * and the Receive DMX on Change mode is 'Send always'.*/
					bool valid = (bool)((msg[0] & 0x01) == 1);
					len = msg.Length - 1;
					byte[] levels = new byte[len];
					Array.Copy(msg, 1, levels, 0, len);
					if (DMXLevelsRecieved != null) DMXLevelsRecieved(this, new DMXLevelArgs(valid, levels));
					break;


			}
		}

		/// <summary>
		/// construct any message
		/// this is a helper function and should not be called directly unless implementing a new type of message
		/// </summary>
		/// <param name="label">the enumeration label of the message you are sending</param>
		/// <param name="data">and pertinent data the message type requires</param>
		public void sendMsg(DMXProMsgLabel label, byte[] data)
		{
			if (!m_port.IsOpen) return;

			List<byte> temp = new List<byte>();
			temp.Add(msgStart);
			temp.Add((byte)label);
			temp.Add((byte)(data.Length & 0xff));
			temp.Add((byte)(data.Length >> 8));
			temp.AddRange(data);
			temp.Add(msgEnd);
			m_port.Write(temp.ToArray(), 0, temp.Count);
		}
		public static void sendMsg(System.IO.Ports.SerialPort port, DMXProMsgLabel label, byte[] data)
		{
			if (!port.IsOpen) return;

			List<byte> temp = new List<byte>();
			temp.Add(msgStart);
			temp.Add((byte)label);
			temp.Add((byte)(data.Length & 0xff));
			temp.Add((byte)(data.Length >> 8));
			temp.AddRange(data);
			temp.Add(msgEnd);
			port.Write(temp.ToArray(), 0, temp.Count);
		}

		/// <summary>
		/// tells the Widget that we want to reprogram it's flash
		/// </summary>
		public void sendReprogramFirmwareRequest()
		{
			// not sure this makes sense
			sendMsg(DMXProMsgLabel.REPROGRAM_FIRMWARE_REQUEST, new byte[0]);
		}

		/// <summary>
		/// programs one page of the Widgets Flash
		/// </summary>
		/// <param name="page">an array of 64 bytes that corrisponds to a page of flash</param>
		public void sendProgramFlashPageRequest(byte[] page)
		{
			if (page.Length != 64) throw new Exception("page file must be 64 bytes");
			sendMsg(DMXProMsgLabel.PROGRAM_FLASH_PAGE_REQUEST, page);
		}

		/// <summary>
		/// gets data about the widget including DMX Timings,Firmware Version, and any user defined data
		/// </summary>
		/// <param name="configSize">the size of the user defined Data</param>
		public void sendGetWidgetParametersRequest(UInt16 configSize)
		{
			if (configSize > 508) throw new Exception("Config Size must be <= 508 bytes");
			byte[] size = new byte[2];
			size[0] = (byte)(configSize & 0xff);
			size[1] = (byte)((configSize >> 8) & 0xff);
			sendMsg(DMXProMsgLabel.GET_WIDGET_PARAMETERS_REQUEST, size);
		}

		/// <summary>
		/// sets the parameters of the widget
		/// </summary>
		/// <param name="DMXOutputBreakTime">the low time at the start of a DMX Packet (>88 uS) usually 100-120uS</param>
		/// <param name="DMXOutputMarkTime"></param>
		/// <param name="DMXOutRate"></param>
		/// <param name="configData"></param>
		public void SetWidgetParametersRequest(float DMXOutputBreakTime, float DMXOutputMarkTime, int DMXOutRate, byte[] configData)
		{
			byte breakTime = (byte)(DMXOutputBreakTime / 10.67);
			byte markTime = (byte)(DMXOutputMarkTime / 10.67);
			byte[] msg = new byte[5 + configData.Length];
			msg[0] = (byte)(configData.Length & 0xff);
			msg[1] = (byte)((configData.Length & 0xff00) >> 8);
			msg[2] = breakTime;
			msg[3] = markTime;
			msg[4] = (byte)DMXOutRate;
			if (configData.Length > 0)
				Array.Copy(configData, 0, msg, 5, configData.Length);
			sendMsg(DMXProMsgLabel.SET_WIDGET_PARAMETERS_REQUEST, msg);
		}

		/// <summary>
		/// this requests that the Widget sends DMX packets out, given the following levels
		/// universe size is set my the number of bytes in "Levels"
		/// Levels.Length must be [24, 512]
		/// </summary>
		/// <param name="Levels"></param>
		public void sendDMXPacketRequest(byte[] Levels)
		{
			if (Levels.Length < 24 || Levels.Length > 512) throw new Exception("The valid number of channel channels must be between 24 and 512.");
			byte[] msg = new byte[1 + Levels.Length];
			Array.Copy(Levels, 0, msg, 1, Levels.Length);
			msg[0] = (byte)0;
			sendMsg(DMXProMsgLabel.OUTPUT_ONLY_SEND_DMX_PACKET_REQUEST, msg);
		}

		/// <summary>
		/// tells the widget to send every dmx packet it receives
		/// </summary>
		public void setSendDMXalways()
		{
			sendMsg(DMXProMsgLabel.RECEIVE_DMX_ON_CHANGE, new byte[1] { 1 });
		}
		/// <summary>
		/// tells the widget to send the "DMX Changed packet" when the dmx signal changes
		/// </summary>
		public void setSendDMXOnChangeOnly()
		{
			sendMsg(DMXProMsgLabel.RECEIVE_DMX_ON_CHANGE, new byte[1] { 0 });
		}
		/// <summary>
		/// requests the serial number of the widget.
		/// this should match the number on the bottom of the widget.
		/// </summary>
		public void GetWidgetSerialNumber()
		{
			sendMsg(DMXProMsgLabel.GET_WIDGET_SERIAL_NUMBER_REQUEST, new byte[0]);
		}
	}
}