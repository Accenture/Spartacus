using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.ProcMon
{
    class ProcMonConfig
    {
        // Columns.
        private List<PMCColumn> _columns = new List<PMCColumn>();
        private List<PMCFilter> _filters = new List<PMCFilter>();
        private List<PMCFilter> _highlights = new List<PMCFilter>();
        public UInt32 ColumnCount = 0;
        public String DbgHelpPath = "";
        public String Logfile = "";
        public String SourcePath = "";
        public String SymbolPath = "";
        public UInt32 HighlightFG = 0;
        public UInt32 HighlightBG = 0;
        public UInt32 AdvancedMode = 0;
        public UInt32 Autoscroll = 0;
        public UInt32 HistoryDepth = 0;
        public UInt32 Profiling = 0;
        public UInt32 DestructiveFilter = 0;
        public UInt32 AlwaysOnTop = 0;
        public UInt32 ResolveAddresses = 0;
        public PMCFont LogFont = new PMCFont();
        public PMCFont BoookmarkFont = new PMCFont();     // Not a typo.

        public ProcMonConfig()
        {
            // We need to make sure some variables have default values.

            // Fonts - https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logfontw
            LogFont.Height = 0;         // The font mapper uses a default height value when it searches for a match.
            LogFont.Width = 0;          // The average width, in logical units, of characters in the font. If lfWidth is zero, the aspect ratio of the device is matched against the digitization aspect ratio of the available fonts to find the closest match, determined by the absolute value of the difference.
            LogFont.Escapement = 0;     // The angle, in tenths of degrees, between the escapement vector and the x-axis of the device. The escapement vector is parallel to the base line of a row of text.
            LogFont.Orientation = 0;    // The angle, in tenths of degrees, between each character's base line and the x-axis of the device.
            LogFont.Weight = 400;       // FW_NORMAL
            LogFont.Italic = 0;         // An italic font if set to TRUE.
            LogFont.Underline = 0;      // An underlined font if set to TRUE.
            LogFont.StrikeOut = 0;      // A strikeout font if set to TRUE.
            LogFont.Charset = 1;        // DEFAULT_CHARSET
            LogFont.OutPrecision = 0;   // OUT_DEFAULT_PRECIS
            LogFont.ClipPrecision = 0;  // CLIP_DEFAULT_PRECIS
            LogFont.Quality = 0;        // DEFAULT_QUALITY
            LogFont.PitchAndFamily = 0; // DEFAULT_PITCH
            LogFont.FaceName = new string('\0', 32);

            // Same for Bookmark Font
            BoookmarkFont.Height = 0;
            BoookmarkFont.Width = 0;
            BoookmarkFont.Escapement = 0;
            BoookmarkFont.Orientation = 0;
            BoookmarkFont.Weight = 400;
            BoookmarkFont.Italic = 0;
            BoookmarkFont.Underline = 0;
            BoookmarkFont.StrikeOut = 0;
            BoookmarkFont.Charset = 1;
            BoookmarkFont.OutPrecision = 0;
            BoookmarkFont.ClipPrecision = 0;
            BoookmarkFont.Quality = 0;
            BoookmarkFont.PitchAndFamily = 0;
            BoookmarkFont.FaceName = new string('\0', 32);

            HighlightBG = 16777215;
        }

        public void SetColumns(List<PMCColumn> Columns)
        {
            _columns = Columns;
        }

        public List<PMCColumn> GetColumns()
        {
            return _columns;
        }

        public void AddColumn(FilterRuleColumn Column, UInt16 Width)
        {
            _columns.Add(new PMCColumn { Column = Column, Width = Width });
        }

        public void SetFilters(List<PMCFilter> Filters)
        {
            _filters = Filters;
        }

        public void AddFilter(FilterRuleColumn Column, FilterRuleRelation Relation, FilterRuleAction Action, String Value)
        {
            _filters.Add(new PMCFilter { Column = Column, Relation = Relation, Action = Action, Value = Value });
        }

        public void SetHighlights(List<PMCFilter> Highlights)
        {
            _highlights = Highlights;
        }

        private void SanityCheck()
        {
            if (_columns.Count() == 0)
            {
                throw new Exception("No columns specified in the configuration");
            }
            else if (_filters.Count() == 0)
            {
                throw new Exception("No filters specified in the configuration");
            }
        }

        public void Save(string saveAs)
        {
            SanityCheck();
            using (var stream = File.Open(saveAs, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.Unicode, false))
                {
                    // Int/UInt
                    WriteConfigToFile(writer, "ColumnCount", _columns.Where(c => c.Column != FilterRuleColumn.NONE).Count());
                    WriteConfigToFile(writer, "HighlightFG", HighlightFG);
                    WriteConfigToFile(writer, "HighlightBG", HighlightBG);
                    WriteConfigToFile(writer, "AdvancedMode", AdvancedMode);
                    WriteConfigToFile(writer, "Autoscroll", Autoscroll);
                    WriteConfigToFile(writer, "HistoryDepth", HistoryDepth);
                    WriteConfigToFile(writer, "Profiling", Profiling);
                    WriteConfigToFile(writer, "DestructiveFilter", DestructiveFilter);
                    WriteConfigToFile(writer, "AlwaysOnTop", AlwaysOnTop);
                    WriteConfigToFile(writer, "ResolveAddresses", ResolveAddresses);

                    // Strings
                    WriteConfigToFile(writer, "DbgHelpPath", DbgHelpPath);
                    WriteConfigToFile(writer, "Logfile", Logfile);
                    WriteConfigToFile(writer, "SourcePath", SourcePath);
                    WriteConfigToFile(writer, "SymbolPath", SymbolPath);

                    // Fonts
                    WriteConfigToFile(writer, "LogFont", LogFont);
                    WriteConfigToFile(writer, "BoookmarkFont", BoookmarkFont);

                    // Columns
                    WriteConfigToFile(writer, "Columns", _columns);
                    WriteConfigToFile(writer, "ColumnMap", _columns);

                    // Filters
                    WriteConfigToFile(writer, "FilterRules", _filters);
                    WriteConfigToFile(writer, "HighlightRules", _filters);
                }
            }
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, Int32 value)
        {
            WriteConfigHeaderToFile(writer, name, GetDataSize(value));
            writer.Write(value);
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, UInt32 value)
        {
            WriteConfigHeaderToFile(writer, name, GetDataSize(value));
            writer.Write(value);
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, String value)
        {
            WriteConfigHeaderToFile(writer, name, GetDataSize(value));
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, PMCFont value)
        {
            WriteConfigHeaderToFile(writer, name, GetDataSize(value));

            writer.Write(value.Height);
            writer.Write(value.Width);
            writer.Write(value.Escapement);
            writer.Write(value.Orientation);
            writer.Write(value.Weight);
            writer.Write(value.Italic);
            writer.Write(value.Underline);
            writer.Write(value.StrikeOut);
            writer.Write(value.Charset);
            writer.Write(value.OutPrecision);
            writer.Write(value.ClipPrecision);
            writer.Write(value.Quality);
            writer.Write(value.PitchAndFamily);
            writer.Write(Encoding.Unicode.GetBytes(value.FaceName));
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, List<PMCColumn> value)
        {
            int dataSize = (name.ToLower() == "columns") ? value.Count() * 2 : value.Count() * 4;
            WriteConfigHeaderToFile(writer, name, dataSize);

            if (name.ToLower() == "columns")
            {
                foreach (PMCColumn item in value)
                {
                    writer.Write(item.Width);
                }
            }
            else if (name.ToLower() == "columnmap")
            {
                foreach (PMCColumn item in value)
                {
                    writer.Write((Int32)item.Column);
                }
            }
        }

        private void WriteConfigToFile(BinaryWriter writer, string name, List<PMCFilter> value)
        {
            WriteConfigHeaderToFile(writer, name, GetDataSize(value));

            // Write the reserved byte.
            writer.Write(Convert.ToByte(1));
            writer.Write(Convert.ToByte(value.Count()));

            foreach (PMCFilter filter in value)
            {
                byte[] filterValue = Encoding.Unicode.GetBytes(filter.Value + '\0');

                writer.Write(Convert.ToByte(0));    // Reserved.
                writer.Write(Convert.ToByte(0));    // Reserved.
                writer.Write(Convert.ToByte(0));    // Reserved.
                writer.Write((UInt32)filter.Column);
                writer.Write((UInt32)filter.Relation);
                writer.Write((Byte)filter.Action);
                writer.Write(filterValue.Length);
                writer.Write(filterValue);
                writer.Write(Convert.ToInt32(0));
                writer.Write(Convert.ToByte(0));
            }

            writer.Write(Convert.ToByte(0));        // Reserved.
            writer.Write(Convert.ToByte(0));        // Reserved.
            writer.Write(Convert.ToByte(0));        // Reserved.
        }

        private void WriteConfigHeaderToFile(BinaryWriter writer, string configName, int dataSize)
        {
            if (!configName.EndsWith("\0"))
            {
                configName += '\0';
            }
            int configNameSize = GetDataSize(configName);

            // Calculate and write the record's size.
            int recordSize = 4              // The recordSize itself.
                + 4                         // First 4 field size = 0x10
                + 4                         // First 5 field size = 0x10 + ConfigName.Length
                + 4                         // Data size length.
                + configNameSize            // The config name itself.
                + dataSize;                 // The data size itself.
            writer.Write(recordSize);

            // Write first four field size, this is always 0x10.
            writer.Write(0x10);

            // Write the length of first 5 fields. This is the previous 0x10 + length of the name value.
            writer.Write(0x10 + configNameSize); // +2 is for \0 in the end.

            // Write the data size.
            writer.Write(dataSize);

            // Write the name.
            // https://stackoverflow.com/questions/47409296/c-sharp-binarywriter-write-method-string-size
            writer.Write(Encoding.Unicode.GetBytes(configName));
        }

        private int GetDataSize(dynamic value)
        {
            int size = 0;
            if (value is String)
            {
                size = value.Length * 2;    // // double it as it's unicode.
            }
            else if (value is Int32 || value is UInt32)
            {
                size = sizeof(Int32);
            }
            else if (value is PMCFont)
            {
                /*
                 * Broken down to make it easier to read:
                 *      5 * Int32
                 *      8 * Byte
                 *      1 * String(32) - Or Byte(64) (Unicode).
                 */
                size = (5 * 4) + (8 * 1) + (32 * 2);
            }
            else if (value is List<PMCFilter>)
            {
                size = 1;                                                           // Reserved Byte
                size += 1;                                                          // Number of rules (byte)

                foreach (PMCFilter filter in value)
                {
                    size += 3;                                                      // Reserved 3 bytes
                    size += 4;                                                      // ColumnType
                    size += 4;                                                      // Relation
                    size += 1;                                                      // Action
                    size += 4;                                                      // Value Length
                    size += Encoding.Unicode.GetBytes(filter.Value + '\0').Length;  // Value
                    size += 4;                                                      // IntValue
                    size += 1;                                                      // Reserved byte
                }

                size += 3;                                                          // Reserved bytes
            }

            return size;
        }
    }
}
