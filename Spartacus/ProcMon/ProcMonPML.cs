using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;


namespace Spartacus.ProcMon
{
    class ProcMonPML
    {
        private readonly string PMLFile = "";

        private FileStream stream = null;

        private BinaryReader reader = null;

        private PMLHeaderStruct LogHeader = new PMLHeaderStruct();

        private string[] LogStrings = new string[0];

        Dictionary<int, PMLProcessStruct> LogProcesses = new Dictionary<int, PMLProcessStruct>();

        private UInt32[] LogEventOffsets = new UInt32[0];

        private UInt32 currentEventIndex = 0;

        public ProcMonPML(string pMLFile)
        {
            PMLFile = pMLFile;
            Load();
        }

        public void Close()
        {
            if (reader != null)
            {
                reader.Close();
            }

            if (stream != null)
            {
                stream.Close();
            }
        }

        public void Rewind()
        {
            currentEventIndex = 0;
        }

        public PMLEvent? GetNextEvent()
        {
            return GetEvent(currentEventIndex++);
        }

        public PMLEvent? GetEvent(UInt32 eventIndex)
        {
            if (eventIndex >= TotalEvents())
            {
                return null;
            }

            int pVoidSize = LogHeader.Architecture == 1 ? 8 : 4;
            stream.Seek(LogEventOffsets[eventIndex], SeekOrigin.Begin);

            PMLEventStruct logEvent = new PMLEventStruct();

            logEvent.indexProcessEvent = reader.ReadInt32();
            logEvent.ThreadId = reader.ReadInt32();
            logEvent.EventClass = reader.ReadInt32();
            logEvent.OperationType = reader.ReadInt16();

            /*
             * In order to speed up the I/O I'm reading in bulk data that I'm not using further down. 
             * By doing so, reading 8 million events drops from 65 to 46 seconds.
             */
            //reader.ReadBytes(6);    // Unknown.
            //logEvent.DurationOfOperation = reader.ReadInt64();
            //reader.ReadInt64(); // FILETIME.
            reader.ReadBytes(6 + 8 + 8);    // Comment this and uncomment the 3 lines above if needed.
            
            
            logEvent.Result = reader.ReadUInt32();
            logEvent.CapturedStackTraceDepth = reader.ReadInt16();
            reader.ReadInt16(); // Unknown.
            logEvent.ExtraDetailSize = reader.ReadUInt32();
            logEvent.ExtraDetailOffset = reader.ReadUInt32();

            int sizeOfStackTrace = logEvent.CapturedStackTraceDepth * pVoidSize;

            /* Check the comment about speeding this up from above. */
            //stream.Seek(sizeOfStackTrace, SeekOrigin.Current);
            //stream.Seek(pVoidSize * 5 + 0x14, SeekOrigin.Current);
            //reader.ReadInt32(); // Should be 0
            stream.Seek(sizeOfStackTrace + (pVoidSize * 5 + 0x14) + 4, SeekOrigin.Current);
            byte stringSize = reader.ReadByte();
            reader.ReadBytes(3); // Not relevant for now.
            string eventPath = Encoding.ASCII.GetString(reader.ReadBytes(stringSize));

            return new PMLEvent()
            {
                EventClass = (EventClassType)logEvent.EventClass,
                Operation = (EventFileSystemOperation)logEvent.OperationType,
                Result = (EventResult)logEvent.Result,
                Path = eventPath,
                Process = LogProcesses[logEvent.indexProcessEvent],
                OriginalEvent = logEvent,
                Loaded = true,
                FoundPath = ""
            };
        }

        public UInt32 TotalEvents()
        {
            return LogHeader.TotalEventCount;
        }

        private void Load()
        {
            stream = File.Open(PMLFile, FileMode.Open, FileAccess.Read);
            reader = new BinaryReader(stream, Encoding.Unicode);

            Logger.Debug("Reading event log header...");
            ReadHeader();

            Logger.Debug("Reading event log strings...");
            ReadStrings();

            Logger.Debug("Reading event log processes...");
            ReadProcesses();

            Logger.Debug("Reading event offsets...");
            ReadEventOffsets();
        }

        private void ReadHeader()
        {
            LogHeader.Signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
            LogHeader.Version = reader.ReadInt32();
            if (LogHeader.Signature != "PML_")
            {
                throw new Exception("Invalid file signature - it should be PML_ but it is: " + LogHeader.Signature);
            }
            else if (LogHeader.Version != 9)
            {
                throw new Exception("Invalid file version: " + LogHeader.Version);
            }

            LogHeader.Architecture = reader.ReadInt32();
            LogHeader.ComputerName = new String(reader.ReadChars(0x10));
            LogHeader.SystemRootPath = new string(reader.ReadChars(0x104));
            LogHeader.TotalEventCount = reader.ReadUInt32();
            reader.ReadInt64(); // Unknown.
            LogHeader.OffsetEventArray = reader.ReadInt64();
            LogHeader.OffsetEventOffsetArray = reader.ReadInt64();
            LogHeader.OffsetProcessArray = reader.ReadInt64();
            LogHeader.OffsetStringArray = reader.ReadInt64();
            LogHeader.OffsetIconArray = reader.ReadInt64();
            reader.ReadBytes(0xC);  // Unknown.
            LogHeader.WindowsVersionMajor = reader.ReadInt32();
            LogHeader.WindowsVersionMinor = reader.ReadInt32();
            LogHeader.WindowsVersionBuild = reader.ReadInt32();
            LogHeader.WindowsVersionRevision = reader.ReadInt32();
            LogHeader.WindowsServicePack = Encoding.Unicode.GetString(reader.ReadBytes(0x32));

            reader.ReadBytes(0xD6); // Unknown.
            LogHeader.LogicalProcessors = reader.ReadInt32();
            LogHeader.RAMSize = reader.ReadInt64();
            LogHeader.OffsetEventArray2 = reader.ReadInt64();
            LogHeader.OffsetHostsPortArray = reader.ReadInt64();
        }

        private void ReadStrings()
        {
            stream.Seek(LogHeader.OffsetStringArray, SeekOrigin.Begin);
            Int32 stringCount = reader.ReadInt32();
            Logger.Verbose("Found " + stringCount + " strings...");

            Logger.Verbose("Reading string offsets...");
            Int32[] stringOffsets = new Int32[stringCount];
            for (int i = 0; i < stringOffsets.Length; i++)
            {
                stringOffsets[i] = reader.ReadInt32();
            }

            Logger.Verbose("Reading strings...");
            Array.Resize(ref LogStrings, stringCount);
            for (int i = 0; i < stringOffsets.Length; i++)
            {
                stream.Seek((LogHeader.OffsetStringArray + stringOffsets[i]), SeekOrigin.Begin);
                Int32 stringSize = reader.ReadInt32();
                LogStrings[i] = Encoding.Unicode.GetString(reader.ReadBytes(stringSize)).Trim('\0');
            }
        }

        private void ReadProcesses()
        {
            stream.Seek((LogHeader.OffsetProcessArray), SeekOrigin.Begin);
            Int32 processCount = reader.ReadInt32();
            Logger.Verbose("Found " + processCount + " processes...");

            Logger.Verbose("Reading process offsets...");
            // The array of process indexes is not essential becuase they appear in the process structure itself.
            stream.Seek(processCount * 4, SeekOrigin.Current);
            Int32[] processOffsets = new Int32[processCount];
            for (int i = 0; i < processOffsets.Length; i++)
            {
                processOffsets[i] = reader.ReadInt32();
            }

            Logger.Verbose("Reading processes...");
            for (int i = 0; i < processOffsets.Length; i++)
            {
                stream.Seek((LogHeader.OffsetProcessArray + processOffsets[i]), SeekOrigin.Begin);
                PMLProcessStruct process = new PMLProcessStruct();

                process.ProcessIndex = reader.ReadInt32();
                process.ProcessId = reader.ReadInt32();
                process.ParentProcessId = reader.ReadInt32();
                reader.ReadInt32();     // Unknown.
                process.AuthenticationId = reader.ReadInt64();
                process.SessionNumber = reader.ReadInt32();
                reader.ReadInt32();     // Unknown.
                reader.ReadInt64();     // Start Process FILETIME.
                reader.ReadInt64();     // End Process FILETIME.
                process.IsVirtualised = reader.ReadInt32();
                process.Is64 = reader.ReadInt32();
                process.indexStringIntegrity = reader.ReadInt32();
                process.indexStringUser = reader.ReadInt32();
                process.indexStringProcessName = reader.ReadInt32();
                process.indexStringImagePath = reader.ReadInt32();
                process.indexStringCommandLine = reader.ReadInt32();
                process.indexStringExecutableCompany = reader.ReadInt32();
                process.indexStringExecutableVersion = reader.ReadInt32();
                process.indexStringExecutableDescription = reader.ReadInt32();

                process.Integrity = LogStrings[process.indexStringIntegrity];
                process.User = LogStrings[process.indexStringUser];
                process.ProcessName = LogStrings[process.indexStringProcessName];
                process.ImagePath = LogStrings[process.indexStringImagePath];
                process.CommandLine = LogStrings[process.indexStringCommandLine];
                process.ExecutableCompany = LogStrings[process.indexStringExecutableCompany];
                process.ExecutableVersion = LogStrings[process.indexStringExecutableVersion];
                process.ExecutableDescription = LogStrings[process.indexStringExecutableDescription];

                LogProcesses.Add(process.ProcessIndex, process);
            }
        }

        private void ReadEventOffsets()
        {
            // Load Events.
            Logger.Verbose("Reading event log offsets...");
            stream.Seek(LogHeader.OffsetEventOffsetArray, SeekOrigin.Begin);
            Array.Resize(ref LogEventOffsets, (int)LogHeader.TotalEventCount);
            for (int i = 0; i < LogEventOffsets.Length; i++)
            {
                LogEventOffsets[i] = reader.ReadUInt32();
                reader.ReadByte();      // Unknown.
            }
        }
    }
}
