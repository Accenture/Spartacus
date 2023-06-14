using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Spartacus.ProcMon
{
    class ProcMonConstants
    {
        public enum PMCConfigName
        {
            None,
            Columns,
            ColumnMap,
            ColumnCount,            // Auto-Calculated when creating a new file.
            DbgHelpPath,
            Logfile,
            HighlightFG,
            HighlightBG,
            LogFont,
            BoookmarkFont,          // Not a typo.
            AdvancedMode,
            Autoscroll,
            HistoryDepth,
            Profiling,
            DestructiveFilter,
            AlwaysOnTop,
            ResolveAddresses,
            SourcePath,
            SymbolPath,
            FilterRules,
            HighlightRules
        }

        public enum FilterRuleAction : Byte
        {
            EXCLUDE = 0,
            INCLUDE = 1,
        }

        public enum FilterRuleRelation : Int32
        {
            IS = 0,
            IS_NOT = 1,
            LESS_THAN = 2,
            MORE_THAN = 3,
            BEGINS_WITH = 4,
            ENDS_WITH = 5,
            CONTAINS = 6,
            EXCLUDES = 7
        }

        public enum FilterRuleColumn : Int32
        {
            NONE = 0,
            DATE_AND_TIME = 40052,
            PROCESS_NAME = 40053,
            PID = 40054,
            OPERATION = 40055,
            RESULT = 40056,
            DETAIL = 40057,
            SEQUENCE = 40058,
            COMPANY = 40064,
            DESCRIPTION = 40065,
            COMMAND_LINE = 40066,
            USER = 40067,
            IMAGE_PATH = 40068,
            SESSION = 40069,
            PATH = 40071,
            TID = 40072,
            RELATIVE_TIME = 40076,
            DURATION = 40077,
            TIME_OF_DAY = 40078,
            VERSION = 40081,
            EVENT_CLASS = 40082,
            AUTHENTICATION_ID = 40083,
            VIRTUALIZED = 40084,
            INTEGRITY = 40085,
            CATEGORY = 40086,
            PARENT_PID = 40087,
            ARCHITECTURE = 40088,
            COMPLETION_TIME = 40164
        }

        public enum EventClassType : Int32
        {
            Unknown = 0,
            Process = 1,
            Registry = 2,
            File_System = 3,
            Profiling = 4,
            Network = 5
        }

        public enum EventRegistryOperation: Int16
        {
            None = -1,
            RegOpenKey = 0,
            RegCreateKey = 1,
            RegCloseKey = 2,
            RegQueryKey = 3,
            RegSetValue = 4,
            RegQueryValue = 5,
            RegEnumValue = 6,
            RegEnumKey = 7,
            RegSetInfoKey = 8,
            RegDeleteKey = 9,
            RegDeleteValue = 10,
            RegFlushKey = 11,
            RegLoadKey = 12,
            RegUnloadKey = 13,
            RegRenameKey = 14,
            RegQueryMultipleValueKey = 15,
            RegSetKeySecurity = 16,
            RegQueryKeySecurity = 17
        }

        public enum EventFileSystemOperation : Int16
        {
            None = -1,
            VolumeDismount = 0,
            VolumeMount = 1,
            ProcessCreate = 1,
            FASTIO_MDL_WRITE_COMPLETE = 2,
            WriteFile2 = 3,
            FASTIO_MDL_READ_COMPLETE = 4,
            ReadFile2 = 5,
            LoadImage = 5,
            QueryOpen = 6,
            FASTIO_CHECK_IF_POSSIBLE = 7,
            IRP_MJ_12 = 8,
            IRP_MJ_11 = 9,
            IRP_MJ_10 = 10,
            IRP_MJ_9 = 11,
            IRP_MJ_8 = 12,
            FASTIO_NOTIFY_STREAM_FO_CREATION = 13,
            FASTIO_RELEASE_FOR_CC_FLUSH = 14,
            FASTIO_ACQUIRE_FOR_CC_FLUSH = 15,
            FASTIO_RELEASE_FOR_MOD_WRITE = 16,
            FASTIO_ACQUIRE_FOR_MOD_WRITE = 17,
            FASTIO_RELEASE_FOR_SECTION_SYNCHRONIZATION = 18,
            CreateFileMapping = 19,
            CreateFile = 20,
            CreatePipe = 21,
            IRP_MJ_CLOSE = 22,
            ReadFile = 23,
            WriteFile = 24,
            QueryInformationFile = 25,
            SetInformationFile = 26,
            QueryEAFile = 27,
            SetEAFile = 28,
            FlushBuffersFile = 29,
            QueryVolumeInformation = 30,
            SetVolumeInformation = 31,
            DirectoryControl = 32,
            FileSystemControl = 33,
            DeviceIoControl = 34,
            InternalDeviceIoControl = 35,
            Shutdown = 36,
            LockUnlockFile = 37,
            CloseFile = 38,
            CreateMailSlot = 39,
            QuerySecurityFile = 40,
            SetSecurityFile = 41,
            Power = 42,
            SystemControl = 43,
            DeviceChange = 44,
            QueryFileQuota = 45,
            SetFileQuota = 46,
            PlugAndPlay = 47
        }

        public enum EventResult : UInt32
        {
            SUCCESS = 0,
            NO_MORE_DATA = 0x103,
            REPARSE = 0x104,
            MORE_ENTRIES = 0x105,
            OPLOCK_BREAK_IN_PROGRESS = 0x108,
            NOTIFY_CLEANUP = 0x10b,
            NOTIFY_ENUM_DIR = 0x10c,
            FILE_LOCKED_WITH_ONLY_READERS = 0x12a,
            FILE_LOCKED_WITH_WRITERS = 0x12b,
            OPLOCK_SWITCHED_TO_NEW_HANDLE = 0x215,
            OPLOCK_HANDLE_CLOSED = 0x216,
            WAIT_FOR_OPLOCK = 0x367,
            PREDEFINED_HANDLE = 0x40000016,
            UNSUCCESSFUL = 0xc0000001,
            INVALID_EA_FLAG = 0x80000015,
            DATATYPE_MISALIGNMENT = 0x80000002,
            BUFFER_OVERFLOW = 0x80000005,
            NO_MORE_FILES = 0x80000006,
            NO_MORE_ENTRIES = 0x8000001a,
            NOT_EMPTY = 0xc0000101,
            NOT_IMPLEMENTED = 0xc0000002,
            INVALID_INFO_CLASS = 0xc0000003,
            INFO_LENGTH_MISMATCH = 0xc0000004,
            ACCESS_VIOLATION = 0xc0000005,
            IN_PAGE_ERROR = 0xc0000006,
            INVALID_HANDLE = 0xc0000008,
            INVALID_PARAMETER = 0xc000000d,
            NO_SUCH_DEVICE = 0xc000000e,
            NO_SUCH_FILE = 0xc000000f,
            INVALID_DEVICE_REQUEST = 0xc0000010,
            END_OF_FILE = 0xc0000011,
            WRONG_VOLUME = 0xc0000012,
            NO_MEDIA = 0xc0000013,
            NONEXISTENT_SECTOR = 0xc0000015,
            NO_MEMORY = 0xc0000017,
            ALREADY_COMMITED = 0xc0000021,
            ACCESS_DENIED = 0xc0000022,
            BUFFER_TOO_SMALL = 0xc0000023,
            OBJECT_TYPE_MISMATCH = 0xc0000024,
            DISK_CORRUPT = 0xc0000032,
            NAME_INVALID = 0xc0000033,
            NAME_NOT_FOUND = 0xc0000034,
            NAME_COLLISION = 0xc0000035,
            OBJECT_PATH_INVALID = 0xc0000039,
            PATH_NOT_FOUND = 0xc000003a,
            PATH_SYNTAX_BAD = 0xc000003b,
            DATA_OVERRUN = 0xc000003c,
            CRC_ERROR = 0xc000003f,
            SHARING_VIOLATION = 0xc0000043,
            QUOTA_EXCEEDED = 0xc0000044,
            EAS_NOT_SUPPORTED = 0xc000004f,
            EA_TOO_LARGE = 0xc0000050,
            NONEXISTENT_EA_ENTRY = 0xc0000051,
            NO_EAS_ON_FILE = 0xc0000052,
            EA_CORRUPTED_ERROR = 0xc0000053,
            FILE_LOCK_CONFLICT = 0xc0000054,
            NOT_GRANTED = 0xc0000055,
            DELETE_PENDING = 0xc0000056,
            PRIVILEGE_NOT_HELD = 0xc0000061,
            LOGON_FAILURE = 0xc000006d,
            RANGE_NOT_LOCKED = 0xc000007e,
            DISK_FULL = 0xc000007f,
            FILE_INVALID = 0xc0000098,
            INSUFFICIENT_RESOURCES = 0xc000009a,
            DEVICE_DATA_ERROR = 0xc000009c,
            DEVICE_NOT_CONNECTED = 0xc000009d,
            MEDIA_WRITE_PROTECTED = 0xc00000a2,
            BAD_IMPERSONATION = 0xc00000a5,
            INSTANCE_NOT_AVAILABLE = 0xc00000ab,
            PIPE_NOT_AVAILABLE = 0xc00000ac,
            INVALID_PIPE_STATE = 0xc00000ad,
            PIPE_BUSY = 0xc00000ae,
            PIPE_DISCONNECTED = 0xc00000b0,
            PIPE_CLOSING = 0xc00000b1,
            PIPE_CONNECTED = 0xc00000b2,
            PIPE_LISTENING = 0xc00000b3,
            INVALID_READ_MODE = 0xc00000b4,
            IO_TIMEOUT = 0xc00000b5,
            IS_DIRECTORY = 0xc00000ba,
            NOT_SUPPORTED = 0xc00000bb,
            DUPLICATE_NAME = 0xc00000bd,
            BAD_NETWORK_PATH = 0xc00000be,
            BAD_NETWORK_PATH_2 = 0xc00000c1,
            INVALID_NETWORK_RESPONSE = 0xc00000c3,
            NETWORK_ERROR = 0xc00000c4,
            BAD_NETWORK_NAME = 0xc00000cc,
            BAD_NETWORK_NAME_2 = 0xc00000d4,
            CANT_WAIT = 0xc00000d8,
            PIPE_EMPTY = 0xc00000d9,
            CSC_OBJECT_PATH_NOT_FOUND = 0xc00000db,
            OPLOCK_NOT_GRANTED = 0xc00000e2,
            INVALID_PARAMETER_1 = 0xc00000ef,
            INVALID_PARAMETER_2 = 0xc00000f0,
            INVALID_PARAMETER_3 = 0xc00000f1,
            INVALID_PARAMETER_4 = 0xc00000f2,
            REDIRECTOR_NOT_STARTED = 0xc00000fb,
            FILE_CORRUPT = 0xc0000102,
            NOT_A_DIRECTORY = 0xc0000103,
            FILES_OPEN = 0xc0000107,
            CANNOT_IMPERSONATE = 0xc000010d,
            CANCELLED = 0xc0000120,
            CANNOT_DELETE = 0xc0000121,
            FILE_DELETED = 0xc0000123,
            FILE_CLOSED = 0xc0000128,
            THREAD_NOT_IN_PROCESS = 0xc000012a,
            INVALID_LEVEL = 0xc0000148,
            PIPE_BROKEN = 0xc000014b,
            REGISTRY_CORRUPT = 0xc000014c,
            IO_FAILED = 0xc000014d,
            KEY_DELETED = 0xc000017c,
            CHILD_MUST_BE_VOLATILE = 0xc0000181,
            INVALID_DEVICE_STATE = 0xc0000184,
            IO_DEVICE_ERROR = 0xc0000185,
            LOG_FILE_FULL = 0xc0000188,
            FS_DRIVER_REQUIRED = 0xc000019c,
            INSUFFICIENT_SERVER_RESOURCES = 0xc0000205,
            INVALID_ADDRESS_COMPONENT = 0xc0000207,
            DISCONNECTED = 0xc000020c,
            NOT_FOUND = 0xc0000225,
            USER_MAPPED_FILE = 0xc0000243,
            LOGIN_WKSTA_RESTRICTION = 0xc0000248,
            PATH_NOT_COVERED = 0xc0000257,
            DFS_UNAVAILABLE = 0xc000026d,
            NO_MORE_MATCHES = 0xc0000273,
            NOT_REPARSE_POINT = 0xc0000275,
            CANNOT_MAKE = 0xc00002ea,
            OBJECTID_NOT_FOUND = 0xc00002f0,
            DOWNGRADE_DETECTED = 0xc0000388,
            CANNOT_EXECUTE_FILE_IN_TRANSACTION = 0xc0190044,
            HIVE_UNLOADED = 0xc0000425,
            FILE_SYSTEM_LIMITATION = 0xc0000427,
            DEVICE_FEATURE_NOT_SUPPORTED = 0xc0000463,
            OBJECT_NOT_EXTERNALLY_BACKED = 0xc000046d,
            CANNOT_BREAK_OPLOCK = 0xc0000909,
            STATUS_OFFLOAD_READ_FLT_NOT_SUPPORTED = 0xc000a2a1,
            STATUS_OFFLOAD_WRITE_FLT_NOT_SUPPORTED = 0xc000a2a2,
            TRANSACTIONAL_CONFLICT = 0xc0190001,
            INVALID_TRANSACTION = 0xc0190002,
            TRANSACTION_NOT_ACTIVE = 0xc0190003,
            EFS_NOT_ALLOWED_IN_TRANSACTION = 0xc019003e,
            TRANSACTIONAL_OPEN_NOT_ALLOWED = 0xc019003f,
            TRANSACTED_MAPPING_UNSUPPORTED_REMOTE = 0xc0190040,
            OFFLOAD_READ_FILE_NOT_SUPPORTED = 0xc000a2a3,
            OFFLOAD_READ_FILE_NOT_SUPPORTED_2 = 0xc000a2a4,
            SPARSE_NOT_ALLOWED_IN_TRANSACTION = 0xc0190049,
            FAST_IO_DISALLOWED = 0xc01c0004
        }

        public struct PMCColumn
        {
            public FilterRuleColumn Column;
            public UInt16 Width;
        }

        public struct PMCFont
        {
            public UInt32 Height;
            public UInt32 Width;
            public UInt32 Escapement;
            public UInt32 Orientation;
            public UInt32 Weight;
            public Byte Italic;
            public Byte Underline;
            public Byte StrikeOut;
            public Byte Charset;
            public Byte OutPrecision;
            public Byte ClipPrecision;
            public Byte Quality;
            public Byte PitchAndFamily;
            public String FaceName;         // Fixed 64 bytes.
        }

        public struct PMCFilter
        {
            public FilterRuleColumn Column;
            public FilterRuleRelation Relation;
            public FilterRuleAction Action;
            public String Value;
        }

        public struct PMLHeaderStruct
        {
            public String Signature;
            public Int32 Version;
            public Int32 Architecture;
            public String ComputerName;
            public String SystemRootPath;
            public UInt32 TotalEventCount;
            public Int64 OffsetEventArray;
            public Int64 OffsetEventOffsetArray;
            public Int64 OffsetProcessArray;
            public Int64 OffsetStringArray;
            public Int64 OffsetIconArray;
            public Int32 WindowsVersionMajor;
            public Int32 WindowsVersionMinor;
            public Int32 WindowsVersionBuild;
            public Int32 WindowsVersionRevision;
            public String WindowsServicePack;
            public Int32 LogicalProcessors;
            public Int64 RAMSize;
            public Int64 OffsetEventArray2;
            public Int64 OffsetHostsPortArray;
        }

        public struct PMLProcessStruct
        {
            public Int32 ProcessIndex;
            public Int32 ProcessId;
            public Int32 ParentProcessId;
            public Int64 AuthenticationId;
            public Int32 SessionNumber;
            public FILETIME ProcessStartTime;
            public FILETIME ProcessEndTime;
            public Int32 IsVirtualised;
            public Int32 Is64;
            public Int32 indexStringIntegrity;
            public Int32 indexStringUser;
            public Int32 indexStringProcessName;
            public Int32 indexStringImagePath;
            public Int32 indexStringCommandLine;
            public Int32 indexStringExecutableCompany;
            public Int32 indexStringExecutableVersion;
            public Int32 indexStringExecutableDescription;
            public Int32 indexIconSmall;
            public Int32 indexIconBig;
            public Int32 ProcessModuleCount;

            // Loaded from the indexes above.
            public string Integrity;
            public string User;
            public string ProcessName;
            public string ImagePath;
            public string CommandLine;
            public string ExecutableCompany;
            public string ExecutableVersion;
            public string ExecutableDescription;
        }

        public struct PMLEventStruct
        {
            public Int32 indexProcessEvent;
            public Int32 ThreadId;
            public Int32 EventClass;
            public Int16 OperationType;
            public Int64 DurationOfOperation;
            public FILETIME TimeCaptured;
            public UInt32 Result;
            public Int16 CapturedStackTraceDepth;
            public Int32 DetailStructureSize;
            public UInt32 ExtraDetailOffset;
            public UInt32 ExtraDetailSize;
        }

        public struct PMLEvent
        {
            public EventClassType EventClass;
            public EventFileSystemOperation Operation;
            public EventRegistryOperation RegistryOperation;
            public EventResult Result;
            public string Path;
            public PMLProcessStruct Process;
            public PMLEventStruct OriginalEvent;
            public bool Loaded;
            public string FoundPath;
        }
    }
}
