using BfevLibrary.Common;
using BfevLibrary.Parsers;
using System;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;

namespace BfevLibrary.Core;

public class SubflowSwitchEvent : Event, IBfevDataBlock
{
    public class SwitchCase
    {
        public uint Value { get; set; }
        public ushort EventIndex { get; set; }

        public SwitchCase(uint value, ushort eventIndex)
        {
            Value = value;
            EventIndex = eventIndex;
        }
    }

    public List<SwitchCase> SwitchCases { get; set; }
    public string EntryPointName { get; set; }

    [JsonConstructor]
    public SubflowSwitchEvent(string name) : base(name, EventType.SubflowSwitch)
    {
        SwitchCases = new List<SwitchCase>();
    }

    public SubflowSwitchEvent(BfevReader reader) : base(reader)
    {
        if (reader.Version[1] < 4) {
            throw new InvalidOperationException("SubflowSwitch events are only supported on version 4.0.0+");
        }
        ushort switchCaseCount = reader.ReadUInt16();
        reader.BaseStream.Position += 2 + 2;
        reader.BaseStream.Position += 8;
        SwitchCases = reader.ReadObjectsPtr(new SwitchCase[switchCaseCount], () => {
            SwitchCase switchCase = new(reader.ReadUInt32(), reader.ReadUInt16());
            reader.ReadInt16();
            reader.Align(8);
            return switchCase;
        }).ToList();
        reader.BaseStream.Position += 8;
        EntryPointName = reader.ReadStringPtr();
    }

    public new void Write(BfevWriter writer)
    {
        if (writer.Version[1] < 4) {
            throw new InvalidOperationException("SubflowSwitch events are only supported on version 4.0.0+");
        }
        base.Write(writer);
        writer.Write((ushort)SwitchCases.Count);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0L);
        Action insertSwitchCasesPtr = writer.ReservePtrIf(SwitchCases.Count > 0, register: true);
        writer.Write(0L);
        writer.WriteStringPtr(EntryPointName);
        writer.ReserveBlockWriter("EventArrayDataBlock", () => {
            if (SwitchCases.Count > 0) {
                writer.Align(8);
                insertSwitchCasesPtr();
                for (int i = 0; i < SwitchCases.Count; i++) {
                    SwitchCase switchCase = SwitchCases[i];
                    writer.Write(switchCase.Value);
                    writer.Write(switchCase.EventIndex);
                    writer.Write((ushort)0); // Padding
                    writer.Align(8);
                }
            }
        });
    }

    internal override bool GetIndices(List<int> indices, int index, int joinIndex, List<int>? ignoreIndices = null)
    {
        if (!base.GetIndices(indices, index, joinIndex, ignoreIndices)) {
            return false;
        }

        foreach (var switchCase in SwitchCases) {
            Flowchart?.Events[switchCase.EventIndex].GetIndices(indices, switchCase.EventIndex, joinIndex, ignoreIndices);
        }

        return true;
    }
}
