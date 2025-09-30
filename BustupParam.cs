using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BustupEntry
{
    public ushort MajorID { get; set; }
    public ushort MinorID { get; set; }
    public ushort SubID { get; set; }
    public ushort Align1 { get; set; }
    public float BasePos_X { get; set; }
    public float BasePos_Y { get; set; }
    public float EyePos_X { get; set; }
    public float EyePos_Y { get; set; }
    public float MouthPos_X { get; set; }
    public float MouthPos_Y { get; set; }
    public uint Flags { get; set; }
    public uint Align3 { get; set; }

    public override string ToString()
    {
        return $"b_{MajorID:D4}_{MinorID:D3}_{SubID:D2} -> type {Flags:D2}";
    }
}

public static class BustupParam
{
    public static List<BustupEntry> ReadBustupFile(string filePath)
    {
        var entries = new List<BustupEntry>();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            long fileSize = fs.Length;
            int entryCount = (int)(fileSize / 0x28);

            for (int i = 0; i < entryCount; i++)
            {
                var entry = new BustupEntry();

                entry.MajorID = ReadUInt16BigEndian(reader);
                entry.MinorID = ReadUInt16BigEndian(reader);
                entry.SubID = ReadUInt16BigEndian(reader);
                entry.Align1 = ReadUInt16BigEndian(reader);
                entry.BasePos_X = ReadFloatBigEndian(reader);
                entry.BasePos_Y = ReadFloatBigEndian(reader);
                entry.EyePos_X = ReadFloatBigEndian(reader);
                entry.EyePos_Y = ReadFloatBigEndian(reader);
                entry.MouthPos_X = ReadFloatBigEndian(reader);
                entry.MouthPos_Y = ReadFloatBigEndian(reader);
                entry.Flags = ReadUInt32BigEndian(reader);
                entry.Align3 = ReadUInt32BigEndian(reader);

                entries.Add(entry);
            }
        }

        return entries;
    }

    public static void WriteBustupFile(string filePath, List<BustupEntry> entries)
    {
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            foreach (var entry in entries)
            {
                WriteUInt16BigEndian(writer, entry.MajorID);
                WriteUInt16BigEndian(writer, entry.MinorID);
                WriteUInt16BigEndian(writer, entry.SubID);
                WriteUInt16BigEndian(writer, entry.Align1);
                WriteFloatBigEndian(writer, entry.BasePos_X);
                WriteFloatBigEndian(writer, entry.BasePos_Y);
                WriteFloatBigEndian(writer, entry.EyePos_X);
                WriteFloatBigEndian(writer, entry.EyePos_Y);
                WriteFloatBigEndian(writer, entry.MouthPos_X);
                WriteFloatBigEndian(writer, entry.MouthPos_Y);
                WriteUInt32BigEndian(writer, entry.Flags);
                WriteUInt32BigEndian(writer, entry.Align3);
            }
        }
    }

    public static List<BustupEntry> MergeEntries(List<BustupEntry> baseList, List<BustupEntry> overrideList)
    {
        var mergedDict = new Dictionary<(ushort, ushort, ushort), BustupEntry>();
        var baseKeys = new HashSet<(ushort, ushort, ushort)>();

        foreach (var entry in baseList)
        {
            var key = (entry.MajorID, entry.MinorID, entry.SubID);
            baseKeys.Add(key);
            mergedDict[key] = entry;
        }

        var newEntries = new List<string>();
        foreach (var overrideEntry in overrideList)
        {
            var key = (overrideEntry.MajorID, overrideEntry.MinorID, overrideEntry.SubID);
            bool isNewEntry = !baseKeys.Contains(key);

            if (isNewEntry)
            {
                newEntries.Add($"({overrideEntry.MajorID}, {overrideEntry.MinorID}, {overrideEntry.SubID})");
            }

            mergedDict[key] = overrideEntry; // Add or overwrite
        }

        // Print new entries that didn't exist in base list
        /*if (newEntries.Count > 0)
        {
            Console.WriteLine($"BustupParam: {newEntries.Count} new entries added from override list:");
            foreach (var newEntry in newEntries)
            {
                Console.WriteLine($"  - {newEntry}");
            }
        }
        else
        {
            Console.WriteLine("BustupParam: No new entries added from override list (only existing entries were updated)");
        }*/

        var mergedList = mergedDict.Values
            .OrderBy(entry => entry.MajorID)
            .ThenBy(entry => entry.MinorID)
            .ThenBy(entry => entry.SubID)
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

    public static void PrintEntries(List<BustupEntry> entries)
    {
        foreach (var entry in entries)
        {
            Console.WriteLine(entry.ToString());
        }
    }

    public static BustupEntry FindEntry(List<BustupEntry> entries, ushort majorID, ushort minorID, ushort subID)
    {
        return entries.Find(e => e.MajorID == majorID &&
                                e.MinorID == minorID &&
                                e.SubID == subID);
    }

    public static List<BustupEntry> FindEntriesByMajorID(List<BustupEntry> entries, ushort majorID)
    {
        return entries.FindAll(e => e.MajorID == majorID);
    }

    public static List<BustupEntry> FindEntriesByMinorID(List<BustupEntry> entries, ushort minorID)
    {
        return entries.FindAll(e => e.MinorID == minorID);
    }

    public static List<BustupEntry> FindEntriesBySubID(List<BustupEntry> entries, ushort subID)
    {
        return entries.FindAll(e => e.SubID == subID);
    }
}