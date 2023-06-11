# MemoryIO.Pcsx2
An implemention of [MemoryIO](https://github.com/Finzenku/MemoryIO) specifically for the [PCSX2 emulator](https://github.com/PCSX2/pcsx2). 
Designed along-side MemoryIO to provide an easy-to-use, cross platform PCSX2 memory manipulation experience.

## Usage
`Pcsx2MemoryIO` automatically attempts to detect and attach to a PCSX2 process. 
Because different PS2 games use different string encodings, it is recommended that you provide an Encoding for `Pcsx2MemoryIO` to use, otherwise it will default to UTF8.  
For example, .hack//Fragment uses "Shift-JIS" encoding.

`Pcsx2MemoryIO` exposes two `bool` properties to check the state of the process such, `IsPcsx2Running` and `IsAttached`.  
Calling `Update()` will attempt to find and attach to a PCSX2 process if not already attached.

### EEmem BaseAddress
`Pcsx2MemoryIO` automatically finds the BaseAddress of EEmem based on whether the process is running in 64bit or 32bit mode. This allows users to simply use the address without worrying about where the EEmem was initialized.  
If you have used Cheat Engine on PCSX2 1.6.0 you might have found a value at address `0x20C461DA`. With `Pcsx2MemoryIO`, you would just ignore the first 2 and use `0x00C461DA`.  
It is recommended to use the methods that use an `int` for the address as they are validated to include the BaseAddress if they are non-zero and not greater than the BaseAddress already.  
`Pcsx2MemoryIO` does expose the `BaseAddress` of EEmem as an `IntPtr` to allow users to use the default `IMemoryIO` methods, without address validation. Advanced users could also use those methods to read/write anywhere in the PCSX2 memory, not just EEmem.

### Example
```csharp
internal struct PlayerStruct
{
    public int NamePointer;
    public short Level;
    public short EXP;
    public int GP;
}

const int playerAddress = 0xC461DA;
const int partyPointersAddress = 0x8B9390;

Pcsx2MemoryIO memoryIO = new(Encoding.GetEncoding("Shift-JIS"));

if (memoryIO.IsAttached)
{
    PlayerStruct player = memoryIO.Read<PlayerStruct>(playerAddress);
    player.GP = 999999;
    player.Level = 99;
    memoryIO.Write(playerAddress, player);

    int[] partyPointers = memoryIO.ReadArray<int>(partyPointersAddress, 3);
    PlayerStruct party3 = memoryIO.Read<PlayerStruct>(partyPointers[2]);
    memoryIO.WriteString(party3.NamePointer, "Kite");
}
```
