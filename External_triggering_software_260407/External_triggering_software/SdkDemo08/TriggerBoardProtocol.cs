using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace SdkDemo08
{
    internal sealed class TriggerBoardException : Exception
    {
        public TriggerBoardException(string message) : base(message) { }
        public TriggerBoardException(string message, Exception inner) : base(message, inner) { }
    }

    internal sealed class TriggerBoardFrame
    {
        public byte Version;
        public byte Command;
        public byte[] Payload;
    }

    internal sealed class TriggerBoardInfo
    {
        public string FirmwareVersion;
        public byte ProtocolVersion;
        public UInt32 ClockHz;
        public UInt32 Capabilities;
    }

    internal sealed class TriggerBoardStatus
    {
        public bool Running;
        public bool Configured;
        public bool PendingUpdate;
        public bool Precharge;
        public bool LoopbackBusy;
        public bool LoopbackPass;
        public bool LoopbackFail;
        public bool InputTimeout;
        public bool CounterOverflow;
        public UInt32 PeriodTicks;
        public UInt32 WidthTicks;
        public bool OutputActiveLow;
        public bool InputActiveLow;
        public UInt32 Remaining;
        public byte LastError;
    }

    internal sealed class TriggerBoardStats
    {
        public UInt32 EventCount;
        public UInt32 LastWidthTicks;
        public UInt32 LastPeriodTicks;
        public UInt32 TooNarrowCount;
        public bool Timeout;
        public bool Overflow;
    }

    internal sealed class TriggerBoardParameters
    {
        public UInt32 PeriodTicks;
        public UInt32 WidthTicks;
        public UInt32 Count;
        public bool OutputActiveLow;
        public bool InputActiveLow;

        public static TriggerBoardParameters FromNanoseconds(
            Int64 periodNs, Int64 widthNs, UInt32 count,
            bool outputActiveLow, bool inputActiveLow)
        {
            if (periodNs <= 0 || widthNs <= 0)
                throw new TriggerBoardException("周期和脉宽必须大于 0 ns。");
            if ((periodNs % 10) != 0 || (widthNs % 10) != 0)
                throw new TriggerBoardException("周期和脉宽必须是 10 ns 的整数倍。");
            if (widthNs >= periodNs)
                throw new TriggerBoardException("有效脉宽必须小于周期。");

            Int64 periodTicks = periodNs / 10;
            Int64 widthTicks = widthNs / 10;
            if (periodTicks < 2 || periodTicks > UInt32.MaxValue ||
                widthTicks < 1 || widthTicks > UInt32.MaxValue)
                throw new TriggerBoardException("周期或脉宽超出 FPGA uint32 tick 范围。");

            TriggerBoardParameters result = new TriggerBoardParameters();
            result.PeriodTicks = (UInt32)periodTicks;
            result.WidthTicks = (UInt32)widthTicks;
            result.Count = count;
            result.OutputActiveLow = outputActiveLow;
            result.InputActiveLow = inputActiveLow;
            return result;
        }
    }

    internal sealed class TriggerBoardClient
    {
        internal const byte Version = 0x01;
        internal const byte CmdPing = 0x01;
        internal const byte CmdSetPeriod = 0x10;
        internal const byte CmdSetWidth = 0x11;
        internal const byte CmdSetPolarity = 0x12;
        internal const byte CmdStart = 0x13;
        internal const byte CmdStop = 0x14;
        internal const byte CmdPulseOnce = 0x15;
        internal const byte CmdReadStatus = 0x20;
        internal const byte CmdReadStats = 0x21;
        internal const byte CmdClearStats = 0x22;

        private readonly SerialPort port;

        public TriggerBoardClient(SerialPort port)
        {
            if (port == null)
                throw new ArgumentNullException("port");
            this.port = port;
        }

        public static UInt16 Crc16(byte[] data, int offset, int count)
        {
            UInt16 crc = 0xffff;
            for (int i = 0; i < count; i++)
            {
                crc ^= (UInt16)(data[offset + i] << 8);
                for (int bit = 0; bit < 8; bit++)
                    crc = (UInt16)(((crc & 0x8000) != 0) ? ((crc << 1) ^ 0x1021) : (crc << 1));
            }
            return crc;
        }

        public static byte[] BuildFrame(byte command, byte[] payload)
        {
            if (payload == null)
                payload = new byte[0];
            if (payload.Length > 64)
                throw new TriggerBoardException("协议 payload 不能超过 64 字节。");

            byte[] frame = new byte[7 + payload.Length];
            frame[0] = 0x55;
            frame[1] = 0xaa;
            frame[2] = Version;
            frame[3] = command;
            frame[4] = (byte)payload.Length;
            Array.Copy(payload, 0, frame, 5, payload.Length);
            UInt16 crc = Crc16(frame, 2, 3 + payload.Length);
            frame[5 + payload.Length] = (byte)(crc & 0xff);
            frame[6 + payload.Length] = (byte)(crc >> 8);
            return frame;
        }

        public static TriggerBoardFrame ParseFrame(byte[] frame)
        {
            if (frame == null || frame.Length < 7)
                throw new TriggerBoardException("响应帧被截断。");
            if (frame[0] != 0x55 || frame[1] != 0xaa)
                throw new TriggerBoardException("响应帧起始标志错误。");
            int payloadLength = frame[4];
            if (payloadLength > 64 || frame.Length != payloadLength + 7)
                throw new TriggerBoardException("响应帧长度错误。");

            UInt16 expected = Crc16(frame, 2, 3 + payloadLength);
            UInt16 actual = (UInt16)(frame[5 + payloadLength] | (frame[6 + payloadLength] << 8));
            if (expected != actual)
                throw new TriggerBoardException("响应帧 CRC 错误。");

            TriggerBoardFrame result = new TriggerBoardFrame();
            result.Version = frame[2];
            result.Command = frame[3];
            result.Payload = new byte[payloadLength];
            Array.Copy(frame, 5, result.Payload, 0, payloadLength);
            return result;
        }

        public static TriggerBoardFrame ParseFirstFrame(byte[] data)
        {
            if (data == null)
                throw new TriggerBoardException("没有响应数据。");
            for (int i = 0; i + 1 < data.Length; i++)
            {
                if (data[i] != 0x55 || data[i + 1] != 0xaa)
                    continue;
                if (i + 5 > data.Length)
                    break;
                int total = 7 + data[i + 4];
                if (i + total > data.Length)
                    throw new TriggerBoardException("响应帧被截断。");
                byte[] frame = new byte[total];
                Array.Copy(data, i, frame, 0, total);
                return ParseFrame(frame);
            }
            throw new TriggerBoardException("未找到响应帧起始标志。");
        }

        public static byte[] ValidateResponse(TriggerBoardFrame frame, byte requestCommand)
        {
            if (frame.Version != Version)
                throw new TriggerBoardException("响应协议版本不匹配。");
            byte expectedCommand = (byte)(requestCommand | 0x80);
            if (frame.Command != expectedCommand)
                throw new TriggerBoardException("响应命令不匹配。");
            if (frame.Payload == null || frame.Payload.Length == 0)
                throw new TriggerBoardException("响应缺少状态码。");
            if (frame.Payload[0] != 0)
                throw new TriggerBoardException("小板返回错误：" + StatusText(frame.Payload[0]));

            byte[] payload = new byte[frame.Payload.Length - 1];
            Array.Copy(frame.Payload, 1, payload, 0, payload.Length);
            return payload;
        }

        public static string StatusText(byte status)
        {
            switch (status)
            {
                case 0: return "OK";
                case 1: return "BAD_VERSION";
                case 2: return "UNKNOWN_CMD";
                case 3: return "BAD_LENGTH";
                case 4: return "BAD_CRC";
                case 5: return "INVALID_PARAMETER";
                case 6: return "NOT_CONFIGURED";
                case 7: return "BUSY";
                case 8: return "FRAME_TIMEOUT";
                case 9: return "UART_FRAME_ERROR";
                default: return "ERROR_" + status.ToString();
            }
        }

        private byte[] Request(byte command, byte[] payload)
        {
            if (!port.IsOpen)
                throw new TriggerBoardException("外触发小板串口未连接。");
            try
            {
                byte[] request = BuildFrame(command, payload);
                port.Write(request, 0, request.Length);
                TriggerBoardFrame response = ReadFrame();
                return ValidateResponse(response, command);
            }
            catch (TriggerBoardException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TriggerBoardException("串口通信失败：" + ex.Message, ex);
            }
        }

        private TriggerBoardFrame ReadFrame()
        {
            int matched = 0;
            while (matched < 2)
            {
                int value = port.ReadByte();
                if (matched == 0)
                    matched = value == 0x55 ? 1 : 0;
                else if (value == 0xaa)
                    matched = 2;
                else
                    matched = value == 0x55 ? 1 : 0;
            }

            byte[] header = ReadExact(3);
            int length = header[2];
            if (length > 64)
                throw new TriggerBoardException("响应 payload 超过 64 字节。");
            byte[] tail = ReadExact(length + 2);
            byte[] frame = new byte[7 + length];
            frame[0] = 0x55;
            frame[1] = 0xaa;
            Array.Copy(header, 0, frame, 2, header.Length);
            Array.Copy(tail, 0, frame, 5, tail.Length);
            return ParseFrame(frame);
        }

        private byte[] ReadExact(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;
            while (offset < count)
                offset += port.Read(result, offset, count - offset);
            return result;
        }

        private static byte[] U32(UInt32 value)
        {
            return new byte[] {
                (byte)(value & 0xff), (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff), (byte)((value >> 24) & 0xff)
            };
        }

        private static UInt32 ReadU32(byte[] data, int offset)
        {
            return (UInt32)(data[offset] | (data[offset + 1] << 8) |
                (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        public TriggerBoardInfo Ping()
        {
            byte[] data = Request(CmdPing, null);
            if (data.Length != 12)
                throw new TriggerBoardException("PING 响应长度错误。");
            TriggerBoardInfo info = new TriggerBoardInfo();
            info.FirmwareVersion = data[0] + "." + data[1] + "." + data[2];
            info.ProtocolVersion = data[3];
            info.ClockHz = ReadU32(data, 4);
            info.Capabilities = ReadU32(data, 8);
            return info;
        }

        public void Configure(TriggerBoardParameters parameters)
        {
            Request(CmdSetPeriod, U32(parameters.PeriodTicks));
            Request(CmdSetWidth, U32(parameters.WidthTicks));
            byte polarity = (byte)((parameters.OutputActiveLow ? 1 : 0) |
                (parameters.InputActiveLow ? 2 : 0));
            Request(CmdSetPolarity, new byte[] { polarity });
        }

        public void Start(UInt32 count) { Request(CmdStart, U32(count)); }
        public void Stop() { Request(CmdStop, null); }
        public void PulseOnce() { Request(CmdPulseOnce, null); }
        public void ClearStats() { Request(CmdClearStats, null); }

        public TriggerBoardStatus ReadStatus()
        {
            byte[] data = Request(CmdReadStatus, null);
            if (data.Length != 16)
                throw new TriggerBoardException("READ_STATUS 响应长度错误。");
            UInt16 flags = (UInt16)(data[0] | (data[1] << 8));
            TriggerBoardStatus status = new TriggerBoardStatus();
            status.Running = (flags & (1 << 0)) != 0;
            status.Configured = (flags & (1 << 1)) != 0;
            status.PendingUpdate = (flags & (1 << 2)) != 0;
            status.Precharge = (flags & (1 << 3)) != 0;
            status.LoopbackBusy = (flags & (1 << 4)) != 0;
            status.LoopbackPass = (flags & (1 << 5)) != 0;
            status.LoopbackFail = (flags & (1 << 6)) != 0;
            status.InputTimeout = (flags & (1 << 7)) != 0;
            status.CounterOverflow = (flags & (1 << 8)) != 0;
            status.PeriodTicks = ReadU32(data, 2);
            status.WidthTicks = ReadU32(data, 6);
            status.OutputActiveLow = (data[10] & 1) != 0;
            status.InputActiveLow = (data[10] & 2) != 0;
            status.Remaining = ReadU32(data, 11);
            status.LastError = data[15];
            return status;
        }

        public TriggerBoardStats ReadStats()
        {
            byte[] data = Request(CmdReadStats, null);
            if (data.Length != 18)
                throw new TriggerBoardException("READ_INPUT_STATS 响应长度错误。");
            UInt16 flags = (UInt16)(data[16] | (data[17] << 8));
            TriggerBoardStats stats = new TriggerBoardStats();
            stats.EventCount = ReadU32(data, 0);
            stats.LastWidthTicks = ReadU32(data, 4);
            stats.LastPeriodTicks = ReadU32(data, 8);
            stats.TooNarrowCount = ReadU32(data, 12);
            stats.Timeout = (flags & 1) != 0;
            stats.Overflow = (flags & 2) != 0;
            return stats;
        }
    }
}
