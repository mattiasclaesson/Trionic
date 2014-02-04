/*
    CanUsbComponent library for CAN-USB device
    Copyright (C) 2012 J Newcomb / http://www.jnewcomb.com/cv

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace CanUsbComponent
{
    public class CanStatusMessage
    {
        bool[] _aStatusFlag = new bool[8];
        public bool[] Flags
        {
            get { return _aStatusFlag; }
        }
        public CanStatusMessage()
        {

        }
        public void readFlags(Int16 num)
        {
            for (int i = 0; i < 8; i++)
            {
                if (((num >> i) & 0x01) == 0x01)
                    _aStatusFlag[i] = true;
                else _aStatusFlag[i] = false;
            }
        }
    }

    /// <summary>
    /// CAN2.0A for standard and 2.0B for extended (29bit) can frame
    /// </summary>
	public enum CanMode : int
	{
        /// <summary>
        /// Indicates standard can frame
        /// </summary>
		CAN2A = 1,
        /// <summary>
        /// Indicates extended can frame
        /// </summary>
		CAN2B = 2
	}

    // Can message. Also includes the code to construct and correct the timestamp   
    public class CanMessage
    {
        public CanMode Mode; // Standard or Extended 
        public bool RTR; // Standard or Extended 
        public UInt32 Id;			// 11/29 bit Identifier
        public int Length;
        public UInt64 Data;
        public UInt32 Timestamp;
        public bool TimestampSet;

        public byte[] DataBuf
        {
            get
            {
                byte[] returnBuffer = new byte[Length];
                for (int i = 0; i < Length; i++)
                {
                    returnBuffer[(Length-1) - i] = Convert.ToByte((Data >> (i * 8)) & 0x00000000000000FF);
                }
                return returnBuffer;
            }
            set
            {
                Length = value.Length;
                Data = BitConverter.ToUInt64(value, 0);
            }

        }

        // Constructor... Use to decode incoming tiiin[dd...dd]\r, Tiiiiiiiin[dd..dd]\r
        public CanMessage(string msg)
        {
            // Default values
            Mode = CanMode.CAN2A; // Standard or Extended 
            RTR = false;
            Id = 0x000;
            Length = 0;
            Data = 0x0000000000000000;
            Timestamp = 0x0;
            TimestampSet = false;
            // Set the values based on the incoming message
            if (msg.StartsWith("t"))
            {
                Mode = CanMode.CAN2A;
                RTR = false;
            }
            else if (msg.StartsWith("T"))
            {
                Mode = CanMode.CAN2B;
                RTR = false;
            }
            else if (msg.StartsWith("r"))
            {
                Mode = CanMode.CAN2A;
                RTR = true;
            }
            else if (msg.StartsWith("R"))
            {
                Mode = CanMode.CAN2B;
                RTR = true;
            }
            else
            {
                // This is not a recognised prefix. Throw an error
                throw new System.ArgumentException("Invalid prefix", "Cant decode message");
            }
            // ID
            string substr;
            int index = 1;
            try
            {
                if (Mode == CanMode.CAN2A)
                {
                    substr = msg.Substring(index, 3);
                    index += 3;
                }
                else
                {
                    substr = msg.Substring(index, 8);
                    index += 8;
                }
                Id = Convert.ToUInt32(substr, 16);
            }
            catch (Exception)
            {
                // Problem decoding the message.
                Console.WriteLine("Unable to parse incoming CAN frame - Mode=" + Mode.ToString() + ", Str=" + msg);
            }
            // Length
            Length = 0;
            try
            {
                substr = msg.Substring(index, 1);
                Length = Convert.ToByte(substr, 16);
                index += 1;
            }
            catch (Exception)
            {
                // Problem decoding the message.
                Console.WriteLine("Unable to parse length from incoming CAN frame - Mode=" + Mode.ToString() + ", Str=" + msg);
            }
            // Databytes
            Data = 0;
            if (Length > 8)
            {
                throw new System.ArgumentException("Invalid CAN data decode", "Length is over 8");
            }
            try
            {
                // RTR messages have a length, but no data..
                if ((Length > 0) && (RTR == false))
                {
                    substr = msg.Substring(index, Length * 2);
                    index += Length * 2;
                    // Convert the data to bytes buffer
                    Data = Convert.ToUInt64(substr, 16);
                }
            }
            catch (Exception)
            {
                // Problem decoding the message.
                Console.WriteLine("Unable to parse length from incoming RTR CAN frame - Mode=" + Mode.ToString() + ", Str=" + msg);
            }
            // Do we have a timestamp (-1 is the CR)
            if (msg.Length - 1 > index)
            {
                try
                {
                    // Yes. get the 4 bytes
                    substr = msg.Substring(index, 4);
                    index += 4;
                    Timestamp = Convert.ToUInt32(substr, 16);
                    TimestampSet = true;
                }
                catch (Exception)
                {
                    // Problem decoding the message.
                    Console.WriteLine("Unable to parse timestamp from incoming CAN frame - Mode=" + Mode.ToString() + ", Str=" + msg);
                }
            }
        }

        // Construct a CAN message
        public CanMessage(CanMode local_Mode, UInt32 local_Id, int local_length, UInt64 local_Data)
        {
            Mode = local_Mode;
            RTR = false;
            Id = local_Id;
            Length = local_length;
            Data = local_Data;
            Timestamp = 0x0;
        }

        // Construct a CAN message
        public CanMessage(CanMode local_Mode, UInt32 local_Id, byte[] local_Data)
        {
            Mode = local_Mode;
            RTR = false;
            Id = local_Id;
            Length = local_Data.Length;
            // Endian swap for the conversion to a UInt64
            if (Length > 0)
            {
                Array.Reverse(local_Data);
            }
            // For BitConverter we need 8 bytes otherwise the conversion will cause an exception
            if (local_Data.Length >= 8)
            {
                Data = BitConverter.ToUInt64(local_Data, 0);
            }
            else
            {
                // If we are less than 8 bytes resize
                Array.Resize<byte>(ref local_Data, local_Data.Length + (8 - local_Data.Length));
                Data = BitConverter.ToUInt64(local_Data, 0);
            }
            Timestamp = 0x0;
        }

        // Default constructor
        public CanMessage()
        {
            Mode = CanMode.CAN2A;
            RTR = false;
            Id = 0;
            Length = 0;
            Data = 0;
            Timestamp = 0x0;
        }

        // Print of the contents of the CAN message
        public override string ToString()
        {
            String ts, id, data;
            // Timestamp
            if (TimestampSet)
            {
                ts = String.Format("TS:{0:X4},", Timestamp);
            }
            else
            {
                ts = "";
            }
            // ID
            if (Mode == CanMode.CAN2A)
            {
                id = String.Format("ID:{0:X3}", Id);
            }
            else
            {
                id = String.Format("ID:{0:X8}", Id);
            }
            // Data
            if (RTR)
            {
                // This is and Resquest for data.. there is a length but no data. Print the length
                data = ",RTR " + Length.ToString() + " bytes";
            }
            else if (Length > 0)
            {
                // Not RTR and data present
                data = ",Data:" + Data.ToString("X" + (Length * 2).ToString());
            }
            else
            {
                // Not RTR and no data
                data = "";
            }
            return (ts + id + data);
        }

        // Print of the contents of the CAN message
        public string ToTransmitString()
        {
            String line;
            if (RTR == false)
            {
                // Setup formatter that specifies the number of data bytes to transmit
                string dataformat = "X" + (Length * 2).ToString();
                // This is a standard CAN frame
                if (Mode == CanMode.CAN2A)
                {
                    line = "t" + Id.ToString("X3") + Length.ToString() + Data.ToString(dataformat) + "\r";
                }
                else
                {
                    line = "T" + Id.ToString("X8") + Length.ToString() + Data.ToString(dataformat) + "\r";
                }
            }
            else
            {
                // This is a RTR frame
                if (Mode == CanMode.CAN2A)
                {
                    line = "r" + Id.ToString("X3") + Length.ToString() + "\r";
                }
                else
                {
                    line = "R" + Id.ToString("X8") + Length.ToString() + "\r";
                }
            }
            return (line);
        }
    }
}
