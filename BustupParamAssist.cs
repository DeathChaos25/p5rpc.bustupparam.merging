using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BustupParamAssistEntry
{
    public ushort MajorID { get; set; }
    public ushort MinorID { get; set; }
    public float BasePos_X { get; set; }
    public float BasePos_Y { get; set; }
    public float EyePos_X { get; set; }
    public float EyePos_Y { get; set; }
    public float MouthPos_X { get; set; }
    public float MouthPos_Y { get; set; }
    public uint Flags { get; set; }

    public override string ToString()
    {
        return $"b_{MajorID:D4}_{MinorID:D3} -> type {Flags:D2} (Base:({BasePos_X}, {BasePos_Y}) Eye:({EyePos_X}, {EyePos_Y}) Mouth:({MouthPos_X}, {MouthPos_Y}))";
    }
}

public static class BustupParamAssist
{
    public static List<BustupParamAssistEntry> ReadBustupParamAssistFile(string filePath)
    {
        var entries = new List<BustupParamAssistEntry>();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            long fileSize = fs.Length;
            int entryCount = (int)(fileSize / 0x20);

            for (int i = 0; i < entryCount; i++)
            {
                var entry = new BustupParamAssistEntry();

                entry.MajorID = ReadUInt16BigEndian(reader);
                entry.MinorID = ReadUInt16BigEndian(reader);
                entry.BasePos_X = ReadFloatBigEndian(reader);
                entry.BasePos_Y = ReadFloatBigEndian(reader);
                entry.EyePos_X = ReadFloatBigEndian(reader);
                entry.EyePos_Y = ReadFloatBigEndian(reader);
                entry.MouthPos_X = ReadFloatBigEndian(reader);
                entry.MouthPos_Y = ReadFloatBigEndian(reader);
                entry.Flags = ReadUInt32BigEndian(reader);

                entries.Add(entry);
            }
        }

        return entries;
    }

    public static void WriteBustupParamAssistFile(string filePath, List<BustupParamAssistEntry> entries)
    {
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            foreach (var entry in entries)
            {
                // Write all values in Big Endian format
                WriteUInt16BigEndian(writer, entry.MajorID);
                WriteUInt16BigEndian(writer, entry.MinorID);
                WriteFloatBigEndian(writer, entry.BasePos_X);
                WriteFloatBigEndian(writer, entry.BasePos_Y);
                WriteFloatBigEndian(writer, entry.EyePos_X);
                WriteFloatBigEndian(writer, entry.EyePos_Y);
                WriteFloatBigEndian(writer, entry.MouthPos_X);
                WriteFloatBigEndian(writer, entry.MouthPos_Y);
                WriteUInt32BigEndian(writer, entry.Flags);
            }
        }
    }

    public static List<BustupParamAssistEntry> MergeEntries(List<BustupParamAssistEntry> baseList, List<BustupParamAssistEntry> overrideList)
    {
        var mergedDict = new Dictionary<(ushort, ushort), BustupParamAssistEntry>();
        var baseKeys = new HashSet<(ushort, ushort)>();

        foreach (var entry in baseList)
        {
            var key = (entry.MajorID, entry.MinorID);
            baseKeys.Add(key);
            mergedDict[key] = entry;
        }

        var newEntries = new List<string>();
        foreach (var overrideEntry in overrideList)
        {
            var key = (overrideEntry.MajorID, overrideEntry.MinorID);
            bool isNewEntry = !baseKeys.Contains(key);

            if (isNewEntry)
            {
                newEntries.Add($"({overrideEntry.MajorID}, {overrideEntry.MinorID})");
            }

            mergedDict[key] = overrideEntry; // Add or overwrite
        }

        // Print new entries that didn't exist in base list
        /*if (newEntries.Count > 0)
        {
            Console.WriteLine($"BustupParamAssist: {newEntries.Count} new entries added from override list:");
            foreach (var newEntry in newEntries)
            {
                Console.WriteLine($"  - {newEntry}");
            }
        }
        else
        {
            Console.WriteLine("BustupParamAssist: No new entries added from override list (only existing entries were updated)");
        }*/

        // Reorder entries: MajorID -> MinorID
        var mergedList = mergedDict.Values
            .OrderBy(entry => entry.MajorID)
            .ThenBy(entry => entry.MinorID)
            .ToList();

        return mergedList;
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static float ReadFloatBigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteFloatBigEndian(BinaryWriter writer, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    public static void PrintEntries(List<BustupParamAssistEntry> entries)
    {
        foreach (var entry in entries)
        {
            Console.WriteLine(entry.ToString());
        }
    }

    public static BustupParamAssistEntry FindEntry(List<BustupParamAssistEntry> entries, ushort majorID, ushort minorID)
    {
        return entries.Find(e => e.MajorID == majorID && e.MinorID == minorID);
    }

    public static List<BustupParamAssistEntry> FindEntriesByMajorID(List<BustupParamAssistEntry> entries, ushort majorID)
    {
        return entries.FindAll(e => e.MajorID == majorID);
    }

    public static List<BustupParamAssistEntry> FindEntriesByMinorID(List<BustupParamAssistEntry> entries, ushort minorID)
    {
        return entries.FindAll(e => e.MinorID == minorID);
    }
}