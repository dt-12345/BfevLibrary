using BfevLibrary.Common;
using BfevLibrary.Parsers;
using System;
using System.Text.Json.Serialization;

namespace BfevLibrary.Core;

public class ReturnEvent : Event, IBfevDataBlock
{
    public uint ReturnValue { get; set; }

    [JsonConstructor]
    public ReturnEvent(string name) : base(name, EventType.Return)
    {
        ReturnValue = 0xffffffff;
    }

    public ReturnEvent(BfevReader reader) : base(reader)
    {
        if (reader.Version[1] < 4) {
            throw new InvalidOperationException("Return events are only supported on version 4.0.0+");
        }
        reader.BaseStream.Position += 2;
        ReturnValue = reader.ReadUInt32();
        reader.BaseStream.Position += 8 + 8 + 8 + 8;
    }

    public new void Write(BfevWriter writer)
    {
        if (writer.Version[1] < 4) {
            throw new InvalidOperationException("Return events are only supported on version 4.0.0+");
        }
        base.Write(writer);
        writer.Write((ushort)0);
        writer.Write(ReturnValue);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
    }
}
