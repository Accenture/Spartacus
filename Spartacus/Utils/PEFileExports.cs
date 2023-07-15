using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus
{
    class PEFileExports
    {
        /*
         * Don't try and make sense of this file
         * The PE Header read functionality was butchered to only obtain the relevant information.
         */
        private const int SIZEOF_IMAGE_DOS_HEADER = 64;
        private const int SIZEOF_IMAGE_FILE_HEADER = 20;
        private const int SIZEOF_IMAGE_NT_HEADERS32 = 248;
        private const int SIZEOF_IMAGE_NT_HEADERS64 = 264;
        private const int SIZEOF_IMAGE_EXPORT_DIRECTORY = 40;
        private const int SIZEOF_IMAGE_SECTION_HEADER = 40;

        private FileStream stream;
        private BinaryReader reader;

        private struct IMAGE_EXPORT_DIRECTORY
        {
            public UInt32 Characteristics;
            public UInt32 TimeDateStamp;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt32 Name;
            public UInt32 Base;
            public UInt32 NumberOfFunctions;
            public UInt32 NumberOfNames;
            public UInt32 AddressOfFunctions; // RVA from base of image
            public UInt32 AddressOfNames; // RVA from base of image
            public UInt32 AddressOfNameOrdinals; // RVA from base of image
        }

        public struct FileExport
        {
            public String Name;
            public Int16 Ordinal;
            public String Forward;
        }

        public List<FileExport> Extract(string dllPath)
        {
            List<FileExport> exports = new List<FileExport>();

            stream = File.Open(dllPath, FileMode.Open, FileAccess.Read);
            reader = new BinaryReader(stream, Encoding.ASCII, false);

            Int32 newExecutableHeader = GetNewExecutableHeader();
            bool x32 = Is32bit(newExecutableHeader);
            Int32 NumberOfSections = GetNumberOfSections(newExecutableHeader);
            UInt32 VirtualAddress = GetVirtualAddress(newExecutableHeader, x32);
            Int32 SectionOffset = GetSectionOffset(newExecutableHeader, x32, NumberOfSections, VirtualAddress);
            Int32 ExportOffset = (int)(VirtualAddress - SectionOffset);
            IMAGE_EXPORT_DIRECTORY ExportTable = GetImageExportDirectory(ExportOffset);
            string[] Functions = GetFunctionNames(ExportTable, SectionOffset);
            string[] Forwards = GetForwards(ExportTable, SectionOffset);
            Int16[] Ordinals = GetOrdinals(ExportTable, SectionOffset);

            for (int i = 0; i < Functions.Length; i++)
            {
                exports.Add(new FileExport { Name = Functions[i], Ordinal = Ordinals[i], Forward = Forwards[i] });
            }

            reader.Close();
            stream.Close();

            return exports;
        }

        private Int32 GetNewExecutableHeader()
        {
            // Get the file address of the new executable header - https://www.pinvoke.net/default.aspx/Structures.IMAGE_DOS_HEADER
            stream.Seek(SIZEOF_IMAGE_DOS_HEADER - 4, SeekOrigin.Begin);
            return reader.ReadInt32();
        }

        private bool Is32bit(Int32 newExecutableHeader)
        {
            // Get the architecture - https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_file_header
            stream.Seek(newExecutableHeader + 4, SeekOrigin.Begin);
            return reader.ReadUInt16() == 0x014c;   // IMAGE_FILE_MACHINE_I386
        }

        private UInt16 GetNumberOfSections(Int32 newExecutableHeader)
        {
            stream.Seek(newExecutableHeader, SeekOrigin.Begin);
            reader.ReadUInt32();    // Signature.
            reader.ReadUInt16();    // Machine.
            return reader.ReadUInt16();
        }

        private UInt32 GetVirtualAddress(Int32 newExecutableHeader, bool is32bit)
        {
            // Get the virtual address - IMAGE_NT_HEADERS32.IMAGE_OPTIONAL_HEADER32.IMAGE_DATA_DIRECTORY.VirtualAddress
            stream.Seek(newExecutableHeader + 4 + SIZEOF_IMAGE_FILE_HEADER, SeekOrigin.Begin);
            // 9 UInt16, 2 Bytes, 19 UInt32             - IMAGE_OPTIONAL_HEADER32
            // 9 UInt16, 2 Bytes, 13 UInt32, 5 UInt64   - IMAGE_OPTIONAL_HEADER64
            int skipBytesDependingOnMachine = is32bit ? ((2 * 9) + (2 * 1) + (19 * 4)) : ((2 * 9) + (2 * 1) + (13 * 4) + (5 * 8));
            stream.Seek(skipBytesDependingOnMachine, SeekOrigin.Current);
            return reader.ReadUInt32();
        }

        private Int32 GetSectionOffset(Int32 newExecutableHeader, bool is32bit, Int32 NumberOfSections, UInt32 VirtualAddress)
        {
            int sectionOffset = 0;
            int sectionHeaderOffset = newExecutableHeader + (is32bit ? SIZEOF_IMAGE_NT_HEADERS32 : SIZEOF_IMAGE_NT_HEADERS64);
            for (int i = 0; i < NumberOfSections; i++)
            {
                stream.Seek(sectionHeaderOffset, SeekOrigin.Begin);
                reader.ReadBytes(8);    // char[] * 8
                UInt32 sectionImageVirtualSize = reader.ReadUInt32();
                UInt32 sectionImageVirtualAddress = reader.ReadUInt32();
                reader.ReadUInt32();    // SizeOfRawData
                UInt32 sectionImagePointerToRawData = reader.ReadUInt32();

                if (VirtualAddress > sectionImageVirtualAddress && VirtualAddress < (sectionImageVirtualAddress + sectionImageVirtualSize))
                {
                    sectionOffset = (int)(sectionImageVirtualAddress - sectionImagePointerToRawData);
                    break;
                }

                sectionHeaderOffset += SIZEOF_IMAGE_SECTION_HEADER;
            }
            return sectionOffset;
        }

        private IMAGE_EXPORT_DIRECTORY GetImageExportDirectory(Int32 ExportOffset)
        {
            stream.Seek(ExportOffset, SeekOrigin.Begin);
            IMAGE_EXPORT_DIRECTORY exportTable = new IMAGE_EXPORT_DIRECTORY();
            exportTable.Characteristics = reader.ReadUInt32();
            exportTable.TimeDateStamp = reader.ReadUInt32();
            exportTable.MajorVersion = reader.ReadUInt16();
            exportTable.MinorVersion = reader.ReadUInt16();
            exportTable.Name = reader.ReadUInt32();
            exportTable.Base = reader.ReadUInt32();
            exportTable.NumberOfFunctions = reader.ReadUInt32();
            exportTable.NumberOfNames = reader.ReadUInt32();
            exportTable.AddressOfFunctions = reader.ReadUInt32();
            exportTable.AddressOfNames = reader.ReadUInt32();
            exportTable.AddressOfNameOrdinals = reader.ReadUInt32();
            return exportTable;
        }

        private string[] GetForwards(IMAGE_EXPORT_DIRECTORY ExportTable, Int32 SectionOffset)
        {
            int addressOfNamesOffset = (int)(ExportTable.AddressOfFunctions - SectionOffset);
            string[] Functions = new string[ExportTable.NumberOfNames];

            for (int i = 0; i < ExportTable.NumberOfNames; i++)
            {
                stream.Seek(addressOfNamesOffset, SeekOrigin.Begin);
                Int32 nameOffset = reader.ReadInt32() - SectionOffset;
                if (nameOffset < 0)
                {
                    Functions[i] = "";
                    addressOfNamesOffset += 4;
                    continue;
                }

                stream.Seek(nameOffset, SeekOrigin.Begin);
                Functions[i] = "";
                byte c;
                do
                {
                    c = reader.ReadByte();
                    Functions[i] += Encoding.ASCII.GetString(new byte[] { c });
                } while (c != 0x00);
                Functions[i] = Functions[i].Trim('\0');
                addressOfNamesOffset += 4;
            }

            return Functions;
        }

        private string[] GetFunctionNames(IMAGE_EXPORT_DIRECTORY ExportTable, Int32 SectionOffset)
        {
            int addressOfNamesOffset = (int)(ExportTable.AddressOfNames - SectionOffset);
            string[] Functions = new string[ExportTable.NumberOfNames];

            for (int i = 0; i < ExportTable.NumberOfNames; i++)
            {
                stream.Seek(addressOfNamesOffset, SeekOrigin.Begin);
                Int32 nameOffset = reader.ReadInt32() - SectionOffset;

                stream.Seek(nameOffset, SeekOrigin.Begin);
                Functions[i] = "";
                byte c;
                do
                {
                    c = reader.ReadByte();
                    Functions[i] += Encoding.ASCII.GetString(new byte[] { c });
                } while (c != 0x00);
                Functions[i] = Functions[i].Trim('\0');
                addressOfNamesOffset += 4;
            }

            return Functions;
        }

        private Int16[] GetOrdinals(IMAGE_EXPORT_DIRECTORY ExportTable, Int32 SectionOffset)
        {
            int ordinalOffset = (int)(ExportTable.AddressOfNameOrdinals - SectionOffset);
            Int16[] ordinals = new short[ExportTable.NumberOfNames];
            stream.Seek(ordinalOffset, SeekOrigin.Begin);
            for (int i = 0; i < ExportTable.NumberOfNames; i++)
            {
                ordinals[i] = (Int16)(reader.ReadInt16() + ExportTable.Base);
            }

            return ordinals;
        }
    }
}
