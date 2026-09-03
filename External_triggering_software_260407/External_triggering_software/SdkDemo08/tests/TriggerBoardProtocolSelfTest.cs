using System;

namespace SdkDemo08
{
    internal static class TriggerBoardProtocolSelfTest
    {
        private static int passed;

        private static void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("FAILED: " + name);
            passed++;
        }

        private static void ExpectFailure(Action action, string name)
        {
            bool failed = false;
            try { action(); }
            catch (TriggerBoardException) { failed = true; }
            Check(failed, name);
        }

        public static int Main()
        {
            byte[] standard = System.Text.Encoding.ASCII.GetBytes("123456789");
            Check(TriggerBoardClient.Crc16(standard, 0, standard.Length) == 0x29b1, "CRC standard vector");

            byte[] responsePayload = new byte[] { 0, 1, 2, 3 };
            byte[] response = TriggerBoardClient.BuildFrame(0x81, responsePayload);
            TriggerBoardFrame parsed = TriggerBoardClient.ParseFrame(response);
            Check(parsed.Version == 1 && parsed.Command == 0x81, "frame header");
            byte[] accepted = TriggerBoardClient.ValidateResponse(parsed, TriggerBoardClient.CmdPing);
            Check(accepted.Length == 3 && accepted[2] == 3, "response status stripping");

            byte[] prefixed = new byte[response.Length + 4];
            prefixed[0] = 0x00;
            prefixed[1] = 0x55;
            prefixed[2] = 0x01;
            prefixed[3] = 0x7f;
            Array.Copy(response, 0, prefixed, 4, response.Length);
            Check(TriggerBoardClient.ParseFirstFrame(prefixed).Command == 0x81, "SOF resynchronization");

            byte[] damaged = (byte[])response.Clone();
            damaged[5] ^= 0x20;
            ExpectFailure(delegate { TriggerBoardClient.ParseFrame(damaged); }, "bad CRC");
            byte[] truncated = new byte[response.Length - 1];
            Array.Copy(response, truncated, truncated.Length);
            ExpectFailure(delegate { TriggerBoardClient.ParseFrame(truncated); }, "truncated frame");
            ExpectFailure(delegate { TriggerBoardClient.ValidateResponse(parsed, TriggerBoardClient.CmdStop); }, "wrong response command");

            TriggerBoardFrame error = TriggerBoardClient.ParseFrame(
                TriggerBoardClient.BuildFrame(0x81, new byte[] { 5 }));
            ExpectFailure(delegate { TriggerBoardClient.ValidateResponse(error, TriggerBoardClient.CmdPing); }, "device error status");

            TriggerBoardParameters parameters = TriggerBoardParameters.FromNanoseconds(1000000, 200, 1000, false, true);
            Check(parameters.PeriodTicks == 100000 && parameters.WidthTicks == 20, "nanosecond conversion");
            Check(TriggerBoardParameters.FromNanoseconds(20, 10, 0, false, true).PeriodTicks == 2, "minimum values");
            Check(TriggerBoardParameters.FromNanoseconds(42949672950L, 10, UInt32.MaxValue, false, true).PeriodTicks == UInt32.MaxValue, "uint32 boundary");
            ExpectFailure(delegate { TriggerBoardParameters.FromNanoseconds(100, 100, 1, false, true); }, "width equals period");
            ExpectFailure(delegate { TriggerBoardParameters.FromNanoseconds(101, 10, 1, false, true); }, "10 ns resolution");
            ExpectFailure(delegate { TriggerBoardParameters.FromNanoseconds(10, 1, 1, false, true); }, "minimum period");

            Console.WriteLine("TriggerBoardProtocolSelfTest passed: " + passed);
            return 0;
        }
    }
}
