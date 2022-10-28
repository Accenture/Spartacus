using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.ProcMon
{
    class ProcMonPMC
    {
        private readonly string PMCFile = "";

        // This is the dictionary that will hold all the loaded configuration.
        //private Dictionary<string, dynamic> Configuration = new Dictionary<string, dynamic>();

        private ProcMonConfig Configuration = new ProcMonConfig();

        public ProcMonPMC(string PMCFile)
        {
            this.PMCFile = PMCFile;

            // If the file does not exist it means we will be creating a new one.
            if (File.Exists(this.PMCFile))
            {
                Load();
            }
        }

        public ProcMonConfig GetConfiguration()
        {
            return Configuration;
        }

        private void Load()
        {
            using (var stream = File.Open(PMCFile, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream, Encoding.Unicode, false))
                {
                    Int32 currentPosition = 0;
                    do
                    {
                        // The whole file is eventually a bit array of records, and the first 4 bytes is the
                        // size of the current one.
                        Int32 recordSize = reader.ReadInt32();

                        // This is the size of the first 4 fields - always will be 0x10.
                        Int32 firstFourFieldsSize = reader.ReadInt32();
                        
                        // The length of the configuration name. To get it, subtract the size of the first 4
                        // fields we read previously.
                        Int32 configNameLength = reader.ReadInt32() - firstFourFieldsSize;

                        // The size of the data we have to read.
                        Int32 dataSize = reader.ReadInt32();

                        // Now that we have it all, read the actual name of the configuration.
                        string configName = Encoding.Unicode.GetString(reader.ReadBytes(configNameLength));

                        // Try to get the column that has been loaded.
                        Enum.TryParse(configName.Trim('\0'), out PMCConfigName PMCColumn);

                        switch (PMCColumn)
                        {
                            case PMCConfigName.Columns:
                            case PMCConfigName.ColumnMap:
                                Configuration.SetColumns(LoadColumns(PMCColumn, reader, dataSize));
                                break;
                            case PMCConfigName.ColumnCount:
                                Configuration.ColumnCount = reader.ReadUInt32();
                                break;
                            case PMCConfigName.AdvancedMode:
                                Configuration.AdvancedMode = reader.ReadUInt32();
                                break;
                            case PMCConfigName.Autoscroll:
                                Configuration.Autoscroll = reader.ReadUInt32();
                                break;
                            case PMCConfigName.HistoryDepth:
                                Configuration.HistoryDepth = reader.ReadUInt32();
                                break;
                            case PMCConfigName.Profiling:
                                Configuration.Profiling = reader.ReadUInt32();
                                break;
                            case PMCConfigName.DestructiveFilter:
                                Configuration.DestructiveFilter = reader.ReadUInt32();
                                break;
                            case PMCConfigName.AlwaysOnTop:
                                Configuration.AlwaysOnTop = reader.ReadUInt32();
                                break;
                            case PMCConfigName.ResolveAddresses:
                                Configuration.ResolveAddresses = reader.ReadUInt32();
                                break;
                            case PMCConfigName.DbgHelpPath:
                                Configuration.DbgHelpPath = ReadBytesToString(reader, dataSize);
                                break;
                            case PMCConfigName.Logfile:
                                Configuration.Logfile = ReadBytesToString(reader, dataSize);
                                break;
                            case PMCConfigName.SourcePath:
                                Configuration.SourcePath = ReadBytesToString(reader, dataSize);
                                break;
                            case PMCConfigName.SymbolPath:
                                Configuration.SymbolPath = ReadBytesToString(reader, dataSize);
                                break;
                            case PMCConfigName.HighlightFG:
                                Configuration.HighlightFG = reader.ReadUInt32();
                                break;
                            case PMCConfigName.HighlightBG:
                                Configuration.HighlightBG = reader.ReadUInt32();
                                break;
                            case PMCConfigName.LogFont:
                                Configuration.LogFont = LoadFont(reader);
                                break;
                            case PMCConfigName.BoookmarkFont:
                                Configuration.BoookmarkFont = LoadFont(reader);
                                break;
                            case PMCConfigName.FilterRules:
                                Configuration.SetFilters(LoadFilters(reader));
                                break;
                            case PMCConfigName.HighlightRules:
                                Configuration.SetHighlights(LoadFilters(reader));
                                break;
                        }

                        // This is in case there's a config option that we haven't accounted for above, so that
                        // it doesn't start reading random bytes when it's not supposed to.
                        currentPosition += recordSize;
                        stream.Seek(currentPosition, SeekOrigin.Begin);
                    } while (stream.Position < stream.Length);
                }
            }
        }

        private List<PMCFilter> LoadFilters(BinaryReader reader)
        {
            List<PMCFilter> filters = new List<PMCFilter>();

            reader.ReadByte();                                                  // Reserved.
            // Number of filters.
            byte count = reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                PMCFilter filter = new PMCFilter();
                reader.ReadBytes(3);                                            // Reserved.
                filter.Column = (FilterRuleColumn)reader.ReadUInt32();
                filter.Relation = (FilterRuleRelation)reader.ReadUInt32();
                filter.Action = (FilterRuleAction)reader.ReadByte();

                filter.Value = ReadBytesToString(reader, reader.ReadInt32());
                reader.ReadUInt32();                                            // IntValue.
                reader.ReadByte();                                              // Reserved.

                filters.Add(filter);
            }

            reader.ReadBytes(3);                                                // Reserved.

            return filters;
        }

        private PMCFont LoadFont(BinaryReader reader)
        {
            return new PMCFont
            {
                Height = reader.ReadUInt32(),
                Width = reader.ReadUInt32(),
                Escapement = reader.ReadUInt32(),
                Orientation = reader.ReadUInt32(),
                Weight = reader.ReadUInt32(),
                Italic = reader.ReadByte(),
                Underline = reader.ReadByte(),
                StrikeOut = reader.ReadByte(),
                Charset = reader.ReadByte(),
                OutPrecision = reader.ReadByte(),
                ClipPrecision = reader.ReadByte(),
                Quality = reader.ReadByte(),
                PitchAndFamily = reader.ReadByte(),
                FaceName = ReadBytesToString(reader, 64)
            };
        }

        private List<PMCColumn> LoadColumns(PMCConfigName configName, BinaryReader reader, int dataSize)
        {
            List<PMCColumn> columns = Configuration.GetColumns();

            // Depending whether we are reading Columns or ColumnMap we need to calculate the number of elements
            // we have to process, as Columns (widths) are Int16 and ColumnMap (columns) are Int32.
            int maxColumns = configName == PMCConfigName.Columns ? dataSize / 2 : dataSize / 4;

            // If this is the first time the function has been called, we need to pre-populate the array with
            // empty elements.
            if (!columns.Any())
            {
                for (int i = 0; i < maxColumns; i++)
                {
                    columns.Add(new PMCColumn { Column = FilterRuleColumn.NONE, Width = 0 });
                }
            }

            // Now we read the data, depending on the config name.
            for (int i = 0; i < maxColumns; i++)
            {
                PMCColumn item = columns[i];
                if (configName == PMCConfigName.Columns)
                {
                    item.Width = reader.ReadUInt16();
                }
                else if (configName == PMCConfigName.ColumnMap)
                {
                    item.Column = (FilterRuleColumn)reader.ReadUInt32();
                }
                columns[i] = item;
            }

            return columns;
        }

        private string ReadBytesToString(BinaryReader reader, int count)
        {
            return Encoding.Unicode.GetString(reader.ReadBytes(count));
        }
    }
}
