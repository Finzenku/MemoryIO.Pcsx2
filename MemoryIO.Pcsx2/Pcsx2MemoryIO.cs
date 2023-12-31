﻿using MemoryIO.Factories;
using System.Diagnostics;
using System.Text;

namespace MemoryIO.Pcsx2
{
    public class Pcsx2MemoryIO : IMemoryIO
    {
        public static Process? Pcsx2Process => GetPcsx2Process();

        #region Fields
        private const string Pcsx2ProcessName = "pcsx2";
        private const int Pcsx2_32bit_Offset = 0x20000000;
        private const int Pcsx2_64bit_Offset = 0x40000000;

        private IProcessMemoryIO? memoryManager;
        private long Pcsx2MemoryOffset;
        #endregion

        #region Properties
        public bool Pcsx2Running => Pcsx2Process is not null; 
        public bool IsAttached => memoryManager is not null;
        public IntPtr BaseAddress => (IntPtr)Pcsx2MemoryOffset;

        public Encoding Encoding { get; init; }
        #endregion

        #region Constructor
        public Pcsx2MemoryIO()
        {
            Encoding = Encoding.UTF8;
            Update();
        }
        public Pcsx2MemoryIO(Encoding encoding)
        {
            Encoding = encoding;
            Update();
        }
        #endregion

        #region Methods

        private static Process? GetPcsx2Process()
        {
            // PCSX2 1.6.0 on Linux runs via some dynamic linker/loader, "ld-2.21.so", "ld-linux.so.2", "ld-2.31.so"
            // They seem to all have .so in their ProcessName and if we then check the MainModule.FileName we should see "pcsx2"
            // Checking for ".so" first avoids Windows yelling at us for looking at the MainModule of System processes
            // pcsx2.AppImage is another process that runs along-side it but doesn't have the memory we're looking for
            // So we have to avoid p.ProcessName.Contains("pcsx2") in this case
            Process? linux32 = Process.GetProcesses().Where(p => p.ProcessName.Contains(".so") && (p.MainModule?.FileName?.Contains(Pcsx2ProcessName)??false)).FirstOrDefault();
            if (linux32 is not null)
                return linux32;

            return Process.GetProcesses().Where(p => p.ProcessName.Contains(Pcsx2ProcessName)).FirstOrDefault();
        }

        #region Update
        /// <summary>
        /// Updates the API to check if PCSX2 is running, attaches a <see cref="MemoryManager"/> if it is.
        /// </summary>
        /// <returns><see cref="IsAttached"/></returns>
        public bool Update()
        {
            if (!Pcsx2Running)
                memoryManager = null;
            else if (!IsAttached)
            {
                memoryManager = MemoryIOFactory.CreateEnvironmentSpecificMemoryIO(Pcsx2Process!);

                // Double check that we're not using the Linux 1.6.0 process because that still returns Is64BitProcess as true
                if (memoryManager.Is64BitProcess && memoryManager.Process.ProcessName.Contains(Pcsx2ProcessName))
                {
                    // PCSX2 1.7 alligns its virtual memory to have a clean base address for "debugging pleasure"
                    // See pcsx2/System.cpp `makeMemoryManager` function if this ever breaks
                    Pcsx2MemoryOffset = (((long)memoryManager.Process.MainModule!.BaseAddress >> 28) << 28) + Pcsx2_64bit_Offset;
                }
                else
                {
                    // 32bit versions of PCSX2 always have EEmem at this offset
                    Pcsx2MemoryOffset = Pcsx2_32bit_Offset;
                }
            }

            return IsAttached;
        }
        #endregion

        #region ValidateAddress
        public IntPtr ValidateAddress(int address)
        {
            if (address <= 0)
                return IntPtr.Zero;
            if (address > 0 && address < Pcsx2MemoryOffset)
                return (IntPtr)(address + Pcsx2MemoryOffset);
            return (IntPtr)address;
        }
        public IntPtr ValidateAddress(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return IntPtr.Zero;
            long addLong = address.ToInt64();
            if (addLong < Pcsx2MemoryOffset)
                return new IntPtr(addLong + Pcsx2MemoryOffset);
            return address;
        }
        #endregion

        #region IMemoryIO
        public byte[] ReadData(IntPtr address, int dataLength) => IsAttached ? memoryManager!.ReadData(ValidateAddress(address), dataLength) : new byte[0];
        public byte[] ReadData(int address, int dataLength) => IsAttached ? memoryManager!.ReadData(ValidateAddress(address), dataLength) : new byte[0];

        public T Read<T>(IntPtr address) where T : unmanaged => IsAttached ? memoryManager!.Read<T>(ValidateAddress(address)) : default!;
        public T Read<T>(int address) where T : unmanaged => IsAttached ? memoryManager!.Read<T>(ValidateAddress(address)) : default!;

        public T[] ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged => IsAttached ? memoryManager!.ReadArray<T>(ValidateAddress(address), arrayLength) : default!;
        public T[] ReadArray<T>(int address, int arrayLength) where T : unmanaged => IsAttached ? memoryManager!.ReadArray<T>(ValidateAddress(address), arrayLength) : default!;

        public string ReadString(IntPtr address, Encoding encoding, int maxLength = 512) => IsAttached ? memoryManager!.ReadString(ValidateAddress(address), encoding) : string.Empty;
        public string ReadString(int address) => IsAttached ? memoryManager!.ReadString(ValidateAddress(address), Encoding) : string.Empty;

        public string[] ReadStringArray(IntPtr address, Encoding encoding, int maxLength = 512) => IsAttached ? memoryManager!.ReadStringArray(ValidateAddress(address), Encoding) : new string[0];
        public string[] ReadStringArray(int address) => IsAttached ? memoryManager!.ReadStringArray(ValidateAddress(address), Encoding) : new string[0];

        public void WriteData(IntPtr address, byte[] data)
        {
            if (IsAttached)
                memoryManager!.WriteData(ValidateAddress(address), data);
        }

        public void WriteData(int address, byte[] data)
        {
            if (IsAttached)
                memoryManager!.WriteData(ValidateAddress(address), data);
        }

        public void Write<T>(IntPtr address, T value) where T : unmanaged
        {
            if (IsAttached)
                memoryManager!.Write(ValidateAddress(address), value);
        }
        public void Write<T>(int address, T value) where T : unmanaged
        {
            if (IsAttached)
                memoryManager!.Write(ValidateAddress(address), value);
        }

        public void WriteArray<T>(IntPtr address, T[] value) where T : unmanaged
        {
            if (IsAttached)
                memoryManager!.WriteArray(ValidateAddress(address), value);
        }
        public void WriteArray<T>(int address, T[] value) where T : unmanaged
        {
            if (IsAttached)
                memoryManager!.WriteArray(ValidateAddress(address), value);
        }

        public void WriteString(IntPtr address, string text, Encoding encoding)
        {
            if (IsAttached)
                memoryManager!.WriteString(ValidateAddress(address), text, encoding);
        }
        public void WriteString(int address, string text)
        {
            if (IsAttached)
                memoryManager!.WriteString(ValidateAddress(address), text, Encoding);
        }

        public void WriteStringArray(IntPtr address, string[] text, Encoding encoding)
        {
            if (IsAttached)
                memoryManager!.WriteStringArray(ValidateAddress(address), text, encoding);
        }
        public void WriteStringArray(int address, string[] text)
        {
            if (IsAttached)
                memoryManager!.WriteStringArray(ValidateAddress(address), text, Encoding);
        }

        #endregion

        #region ReadPointerMethods
        public string[] ReadStringPointers(int[] addresses, int expectedLength = 16)
        {
            if (!IsAttached)
                return new string[0];

            string[] strings = new string[addresses.Length];
            Array.Sort(addresses);
            int first = addresses[0], last = addresses[^1];
            //Read from the first address to the last address
            byte[] data = ReadArray<byte>(first, last - first);
            int offset = 0;
            for (int i = 0; i < strings.Length - 1; i++)
            {
                offset = addresses[i] - first;

                //j is an expectedLength multiplier, keep incrementing until a string is set
                //Normally, this shouldn't be required but will prob'ly hit some edge cases
                for (int j = 0; strings[i] == null; j++)
                {
                    //Reading through the data array until hitting a zero should maybe be better than string.IndexOf('\0') and then string.Substring()
                    for (int o = expectedLength*j; o < expectedLength*(j+1) && o + offset < data.Length; o++)
                    {
                        if (data[offset + o] == 0)
                        {
                            strings[i] = Encoding.GetString(data[offset..(offset+o)]);
                            break;
                        }
                    }
                    if (offset + expectedLength*j >= data.Length)
                        strings[i] = Encoding.GetString(data[offset..]);
                }
            }

            //We can't know how long the last string is so call ReadString for just that one
            strings[strings.Length-1] = ReadString(last);
            return strings;
        }
        public string[][] ReadStringArrayPointers(int[] addresses, int expectedLength = 64)
        {
            if (!IsAttached)
                return new string[0][];

            string[][] strings = new string[addresses.Length][];
            int first = addresses[0], last = addresses[^1];
            byte[] data = ReadArray<byte>(first, last - first);
            int offset = 0;
            bool firstZero = false;
            for (int i = 0; i < strings.Length - 1; i++)
            {
                offset = addresses[i] - first;
                for (int j = 0; strings[i] == null; j++)
                {
                    firstZero = false;
                    for (int o = expectedLength*j; o < expectedLength*(j+1) && o + offset < data.Length; o++)
                    {
                        if (data[offset+o] == 0)
                        {
                            if (firstZero)
                            {
                                break;
                            }
                            else
                            {
                                strings[i] = Encoding.GetString(data[offset..(offset+o)]).Split('\0');
                                firstZero = true;
                            }
                        }
                        else
                            firstZero = false;
                    }
                }
            }
            strings[^1] = ReadStringArray(last);
            return strings;
        }
        public Dictionary<int, string> GetStringDictionary(int[] addresses, int subString = 0)
        {
            Dictionary<int, string> dict = new();

            if (!IsAttached || addresses.Length == 0)
                return dict;

                //Need to sort the pointers array to ensure that the returned strings line up properly
                Array.Sort(addresses);
            int nonZero = 0;
            while (addresses[nonZero] <= 0)
                nonZero++;
            int lastAdd = addresses[^1], lastUnique = 1;
            while (addresses[^(lastUnique+1)] == lastAdd)
                lastUnique++;
            lastUnique--;
            addresses = addresses[nonZero..^lastUnique];
            string[] strings = ReadStringPointers(addresses);

            if (strings.Length == addresses.Length)
                for (int i = 0; i < strings.Length; i++)
                    dict.TryAdd(addresses[i], strings[i].Substring(subString));
            dict.TryAdd(0, "");
            return dict;
        }
        public Dictionary<int, string[]> GetStringArrayDictionary(int[] addresses)
        {

            Dictionary<int, string[]> dict = new();

            if (!IsAttached || addresses.Length == 0)
                return dict;

            //Need to sort the pointers array to ensure that the returned strings line up properly
            Array.Sort(addresses);
            int nonZero = 0;
            while (addresses[nonZero] == 0)
                nonZero++;
            addresses = addresses[nonZero..];
            string[][] strings = ReadStringArrayPointers(addresses);

            if (strings.Length == addresses.Length)
                for (int i = 0; i < strings.Length; i++)
                    dict.TryAdd((int)addresses[i], strings[i]);
            dict.TryAdd(0, new string[] { "" });
            return dict;

        }
        #endregion

        #endregion
    }
}
