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

    public bool ValuesEqual(BustupParamAssistEntry other)
    {
        if (other == null) return false;

        return MajorID == other.MajorID &&
               MinorID == other.MinorID &&
               BasePos_X == other.BasePos_X &&
               BasePos_Y == other.BasePos_Y &&
               EyePos_X == other.EyePos_X &&
               EyePos_Y == other.EyePos_Y &&
               MouthPos_X == other.MouthPos_X &&
               MouthPos_Y == other.MouthPos_Y &&
               Flags == other.Flags;
    }
}

public static class BustupParamAssist
{
    private static List<BustupParamAssistEntry> _originalEntries = null;
    private static List<BustupParamAssistEntry> _finalEntries = null;

    public static void SetOriginalList(List<BustupParamAssistEntry> originalEntries)
    {
        _originalEntries = new List<BustupParamAssistEntry>(originalEntries);
        _finalEntries = new List<BustupParamAssistEntry>(originalEntries);
    }

    public static List<BustupParamAssistEntry> GetFinalList()
    {
        return _finalEntries?.ToList();
    }

    public static List<BustupParamAssistEntry> MergeIntoFinal(List<BustupParamAssistEntry> newList)
    {
        if (_finalEntries == null)
        {
            throw new InvalidOperationException("Original list must be set using SetOriginalList() first");
        }

        if (newList == null || newList.Count == 0)
            return _finalEntries.ToList();

        // Handle duplicates in finalEntries
        var finalDict = _finalEntries
            .GroupBy(e => (e.MajorID, e.MinorID))
            .ToDictionary(g => g.Key, g => g.Last());

        // Handle duplicates in originalEntries  
        var originalDict = _originalEntries?
            .GroupBy(e => (e.MajorID, e.MinorID))
            .ToDictionary(g => g.Key, g => g.Last())
            ?? new Dictionary<(ushort, ushort), BustupParamAssistEntry>();

        int modifiedCount = 0;
        int newCount = 0;
        var skippedEntries = new List<string>();

        // Group by key to handle duplicates in newList
        var groupedNewEntries = newList
            .GroupBy(e => (e.MajorID, e.MinorID))
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var kvp in groupedNewEntries)
        {
            var key = kvp.Key;
            var newEntry = kvp.Value;

            if (originalDict.TryGetValue(key, out var originalEntry))
            {
                if (newEntry.ValuesEqual(originalEntry))
                {
                    skippedEntries.Add($"({newEntry.MajorID}, {newEntry.MinorID})");
                    continue;
                }
            }

            bool isUpdate = finalDict.ContainsKey(key);

            if (isUpdate)
            {
                modifiedCount++;
            }
            else
            {
                newCount++;
            }

            finalDict[key] = newEntry;
        }

        _finalEntries = finalDict.Values
            .OrderBy(entry => entry.MajorID)
            .ThenBy(entry => entry.MinorID)
            .ToList();

        return _finalEntries.ToList();
    }

    public static List<BustupParamAssistEntry> ReadBustupParamAssistFile(string filePath, bool setAsOriginal = false)
    {
        var entries = ReadBustupParamAssistFileInternal(filePath);

        if (setAsOriginal && entries != null)
        {
            SetOriginalList(entries);
        }

        return entries;
    }

    private static List<BustupParamAssistEntry> ReadBustupParamAssistFileInternal(string filePath)
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