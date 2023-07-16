using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Utils
{
    class PEFileExports
    {
        public struct FileExport
        {
            public string Name;
            public uint Ordinal;
            public string Forward;
        }

        private FileStream stream;
        private BinaryReader reader;

        private const uint SIZEOF_IMAGE_DOS_HEADER = 0x3c;

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_OPTIONAL_HEADER32
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;             // Only this is missing from the x64 structure.

            public uint ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public uint SizeOfStackReserve;
            public uint SizeOfStackCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_OPTIONAL_HEADER64
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;

            public ulong ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public ulong SizeOfStackReserve;
            public ulong SizeOfStackCommit;
            public ulong SizeOfHeapReserve;
            public ulong SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DATA_DIRECTORY
        {
            public uint VirtualAddress;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_HEADER_DIRECTORIES
        {
            public IMAGE_DATA_DIRECTORY ExportTable;
            public IMAGE_DATA_DIRECTORY ImportTable;
            public IMAGE_DATA_DIRECTORY ResourceTable;
            public IMAGE_DATA_DIRECTORY ExceptionTable;
            public IMAGE_DATA_DIRECTORY CertificateTable;
            public IMAGE_DATA_DIRECTORY BaseRelocationTable;
            public IMAGE_DATA_DIRECTORY Debug;
            public IMAGE_DATA_DIRECTORY Architecture;
            public IMAGE_DATA_DIRECTORY GlobalPtr;
            public IMAGE_DATA_DIRECTORY TLSTable;
            public IMAGE_DATA_DIRECTORY LoadConfigTable;
            public IMAGE_DATA_DIRECTORY BoundImport;
            public IMAGE_DATA_DIRECTORY IAT;
            public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
            public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
            public IMAGE_DATA_DIRECTORY ReservedMustBeZero;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_SECTION_HEADER
        {
            // Don't want to enable unsafe code, so will split into 8 bytes.
            public byte NameChar1;
            public byte NameChar2;
            public byte NameChar3;
            public byte NameChar4;
            public byte NameChar5;
            public byte NameChar6;
            public byte NameChar7;
            public byte NameChar8;

            public uint VirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLinenumbers;
            public uint Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXPORT_DIRECTORY_TABLE
        {
            public uint ExportFlags;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint NameRVA;
            public uint OrdinalBase;
            public uint AddressTableEntries;
            public uint NumberOfNamePointers;
            public uint ExportAddressTableRVA;
            public uint NamePointerRVA;
            public uint OrdinalTableRVA;
        }

        public List<FileExport> Extract(string dllFile)
        {
            try
            {
                stream = File.Open(dllFile, FileMode.Open, FileAccess.Read);
                reader = new BinaryReader(stream, Encoding.ASCII, false);

                IMAGE_FILE_HEADER header = GetImageFileHeader();
                bool isPEPlus = IsPEPlus();
                
                if (isPEPlus)
                {
                    IMAGE_OPTIONAL_HEADER64 optionalHeader = ReadStructFromStream<IMAGE_OPTIONAL_HEADER64>();
                }
                else
                {
                    IMAGE_OPTIONAL_HEADER32 optionalHeader = ReadStructFromStream<IMAGE_OPTIONAL_HEADER32>();
                }

                IMAGE_HEADER_DIRECTORIES headerDirectories = ReadStructFromStream<IMAGE_HEADER_DIRECTORIES>();

                uint sectionBaseOffset = GetSectionBaseOffset(headerDirectories, header.NumberOfSections);

                EXPORT_DIRECTORY_TABLE exportTable = GetExportDirectoryTable(headerDirectories.ExportTable.VirtualAddress - sectionBaseOffset);

                uint[] exportOrdinals = ExtractOrdinals(exportTable, sectionBaseOffset);
                string[] exportFunctions = ExtractFunctions(exportTable, sectionBaseOffset, headerDirectories.ExportTable);
                string[] exportForwards = ExtractForwards(exportTable, sectionBaseOffset, headerDirectories.ExportTable);

                return GenerateResult(exportOrdinals, exportFunctions, exportForwards);
            }
            finally
            {
                reader?.Close();
                stream?.Close();
            }
        }

        private List<FileExport> GenerateResult(uint[] ordinals, string[] functions, string[] forwards)
        {
            if (ordinals == null || functions == null || forwards == null)
            {
                return new();
            }
            else if (ordinals.Length == 0 || functions.Length == 0 || forwards.Length == 0)
            {
                return new();
            }

            List<FileExport> exports = new();
            for (int i = 0; i < ordinals.Length; i++)
            {
                exports.Add(new FileExport() { Ordinal = ordinals[i], Name = functions[i], Forward = forwards[i] });
            }

            return exports;
        }

        private string[] ExtractTable(EXPORT_DIRECTORY_TABLE exportTable, uint sectionBaseOffset, IMAGE_DATA_DIRECTORY exportDataTable, uint tableIndexOffset)
        {
            string[] output = new string[exportTable.NumberOfNamePointers];
            uint[] index = ExtractTableIndex(tableIndexOffset - sectionBaseOffset, exportTable.NumberOfNamePointers);

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = "";
                // If the index[i] is within the boundaries of the export table (virtual address + size), extract the string. Otherwise
                // it's a reference to a function offset rather than a forward.
                if (index[i] > 0 && index[i] >= exportDataTable.VirtualAddress && index[i] <= (exportDataTable.VirtualAddress + exportDataTable.Size))
                {
                    output[i] = ReadString(index[i] - sectionBaseOffset);
                }
            }

            return output;
        }

        private string[] ExtractFunctions(EXPORT_DIRECTORY_TABLE exportTable, uint sectionBaseOffset, IMAGE_DATA_DIRECTORY exportDataTable)
        {
            return ExtractTable(exportTable, sectionBaseOffset, exportDataTable, exportTable.NamePointerRVA);
        }

        private string[] ExtractForwards(EXPORT_DIRECTORY_TABLE exportTable, uint sectionBaseOffset, IMAGE_DATA_DIRECTORY exportDataTable)
        {
            return ExtractTable(exportTable, sectionBaseOffset, exportDataTable, exportTable.ExportAddressTableRVA);
        }

        private string ReadString(uint offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            List<byte> output = new();
            do
            {
                byte c = reader.ReadByte();
                if (c == 0x00)
                {
                    break;
                }
                output.Add(c);
            } while (true);

            return Encoding.ASCII.GetString(output.ToArray());
        }

        private uint[] ExtractTableIndex(uint offset, uint size)
        {
            uint[] output = new uint[size];
            stream.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < size; i++)
            {
                output[i] = reader.ReadUInt32();
            }
            return output;
        }

        private uint[] ExtractOrdinals(EXPORT_DIRECTORY_TABLE exportTable, uint sectionBaseOffset)
        {
            uint[] output = new uint[exportTable.NumberOfNamePointers];
            stream.Seek(exportTable.OrdinalTableRVA - sectionBaseOffset, SeekOrigin.Begin);
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = reader.ReadUInt16() + exportTable.OrdinalBase;
            }
            return output;
        }

        private EXPORT_DIRECTORY_TABLE GetExportDirectoryTable(uint offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            return ReadStructFromStream<EXPORT_DIRECTORY_TABLE>();
        }

        private uint GetSectionBaseOffset(IMAGE_HEADER_DIRECTORIES headerDirectories, ushort numberOfSections)
        {
            IMAGE_SECTION_HEADER foundSection = new();

            for (int i = 0; i < numberOfSections; i++)
            {
                IMAGE_SECTION_HEADER sectionHeader = ReadStructFromStream<IMAGE_SECTION_HEADER>();

                // We want the section where the ExportTable's VirtualAddress is within its range.
                if (headerDirectories.ExportTable.VirtualAddress < sectionHeader.VirtualAddress)
                {
                    continue;
                }
                else if (headerDirectories.ExportTable.VirtualAddress > (sectionHeader.VirtualAddress + sectionHeader.VirtualSize))
                {
                    continue;
                }

                foundSection = sectionHeader;
                break;
            }

            return foundSection.VirtualAddress - foundSection.PointerToRawData;
        }

        private bool IsPEPlus()
        {
            /*
             * We need to manually grab the next byte as that's the magic byte that defines if the
             * file is PE or PE+, in order to grab the right data. But as the magic byte is also
             * part of each structure, we'll have to move back to its original position before reading.
             */
            long c = stream.Position;
            bool result = reader.ReadUInt16() == 0x20b;
            stream.Seek(c, SeekOrigin.Begin);
            return result;
        }

        private IMAGE_FILE_HEADER GetImageFileHeader()
        {
            IMAGE_FILE_HEADER header = new();

            // Get the PE location - https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#signature-image-only
            stream.Seek(SIZEOF_IMAGE_DOS_HEADER, SeekOrigin.Begin);
            uint executableHeaderOffset = reader.ReadUInt32() + 4;  // +4 as the first 4 bytes are PE\0\0

            // Navigate to that offset.
            stream.Seek(executableHeaderOffset, SeekOrigin.Begin);
            return BytesToStructure<IMAGE_FILE_HEADER>(reader.ReadBytes(Marshal.SizeOf(header)));
        }

        private T ReadStructFromStream<T>()
        {
            return BytesToStructure<T>(reader.ReadBytes(Marshal.SizeOf(typeof(T))));
        }

        private T BytesToStructure<T>(byte[] data)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
