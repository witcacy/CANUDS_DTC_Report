using System.Collections.Generic;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{
    public class IsoTpDecoder
    {
        public static List<IsoTpMessage> Decode(List<CanFrame> frames)
        {
            var messages = new List<IsoTpMessage>();
            var currentMessages = new Dictionary<uint, IsoTpMessage>();

            foreach (var frame in frames)
            {
                if (frame.Data.Length == 0) continue;

                var pciType = frame.Data[0] >> 4;

                switch (pciType)
                {
                    case 0x0: // Single Frame
                        {
                            int length = frame.Data[0] & 0x0F;
                            var payload = new List<byte>();
                            for (int i = 1; i <= length; i++)
                                payload.Add(frame.Data[i]);

                            var msg = new IsoTpMessage(frame.Id) { Payload = payload, ExpectedLength = length };
                            msg.RawLines.Add(frame.RawLine);
                            msg.LineNumbers.Add(frame.LineNumber);
                            messages.Add(msg);
                            break;
                        }
                    case 0x1: // First Frame
                        {
                            int length = ((frame.Data[0] & 0x0F) << 8) + frame.Data[1];
                            var msg = new IsoTpMessage(frame.Id) { ExpectedLength = length };
                            for (int i = 2; i < frame.Data.Length; i++)
                                msg.Payload.Add(frame.Data[i]);
                            msg.RawLines.Add(frame.RawLine);
                            msg.LineNumbers.Add(frame.LineNumber);

                            currentMessages[frame.Id] = msg;
                            break;
                        }
                    case 0x2: // Consecutive Frame
                        {
                            if (!currentMessages.ContainsKey(frame.Id))
                                break;

                            var msg = currentMessages[frame.Id];
                            for (int i = 1; i < frame.Data.Length; i++)
                                msg.Payload.Add(frame.Data[i]);
                            msg.RawLines.Add(frame.RawLine);
                            msg.LineNumbers.Add(frame.LineNumber);

                            // if message complete, finalize it
                            if (msg.Payload.Count >= msg.ExpectedLength)
                            {
                                messages.Add(msg);
                                currentMessages.Remove(frame.Id);
                            }
                            break;
                        }
                }
            }

            return messages;
        }
    }
}
