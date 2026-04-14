using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Parses item OPL (Object Properties List) tooltip data into structured property information.
    /// Used by the GridHighLight system to match items against highlight rules.
    /// </summary>
    internal class ItemPropertiesData
    {
        public readonly bool HasData = false;
        public string Name = "";
        public readonly string RawData = "";
        public readonly uint serial;
        public string[] RawLines;
        public readonly Item item;
        public List<SinglePropertyData> singlePropertyData = new List<SinglePropertyData>();

        public ItemPropertiesData(World world, Item item)
        {
            if (item == null)
                return;
            this.item = item;

            serial = item.Serial;
            if (world.OPL.TryGetNameAndData(item.Serial, out Name, out RawData))
            {
                Name = Name?.Trim() ?? "";
                HasData = true;
                ProcessData();
            }
        }

        private void ProcessData()
        {
            // Strip basic HTML tags that might be in OPL data
            string formattedData = StripHtmlTags(RawData ?? "");

            RawLines = formattedData.Split(new string[] { "\n", "<br>" }, StringSplitOptions.None);

            foreach (string line in RawLines)
            {
                singlePropertyData.Add(new SinglePropertyData(line));
            }
        }

        private static string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Remove HTML-like tags
            return Regex.Replace(input, "<[^>]+>", "");
        }

        public class SinglePropertyData
        {
            public string OriginalString;
            public string Name = "";
            public double FirstValue = double.MinValue;
            public double SecondValue = double.MinValue;

            private static readonly Regex NumberRegex = new Regex(@"-?\d+(\.\d+)?", RegexOptions.Compiled);
            private static readonly Regex NameCleanRegex = new Regex(@"[-+]?\d+(\.\d+)?[%]?([- ]*\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public SinglePropertyData(string line)
            {
                OriginalString = line;

                // Remove any color tags
                string cleaned = Regex.Replace(line ?? "", @"/c\[[#a-zA-Z0-9]+\]", "").Replace("/cd", "").Trim();

                // Extract numbers
                MatchCollection matches = NumberRegex.Matches(cleaned);

                if (matches.Count > 0)
                {
                    double.TryParse(matches[0].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out FirstValue);
                    if (matches.Count > 1)
                        double.TryParse(matches[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out SecondValue);
                }

                // Remove all numbers and symbols from the cleaned string to isolate the name
                Name = NameCleanRegex.Replace(cleaned, "").Trim();

                // Fallback if something went wrong
                if (string.IsNullOrWhiteSpace(Name))
                    Name = line;
            }

            public override string ToString()
            {
                string output = "";
                if (Name != null)
                    output += Name;
                if (FirstValue != double.MinValue)
                    output += $" {FirstValue}";
                if (SecondValue != double.MinValue)
                    output += $" {SecondValue}";
                return output;
            }
        }
    }
}
