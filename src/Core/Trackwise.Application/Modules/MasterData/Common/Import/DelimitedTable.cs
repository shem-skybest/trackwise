using System.Text;
using Trackwise.Application.Abstractions.Modules.MasterData;

namespace Trackwise.Application.Modules.MasterData.Common.Import;

/// <summary>
/// Reads a steward's delimited file into header-keyed rows.
/// <para>
/// Hand-rolled rather than taken from a CSV library for one reason: every row has to remember the
/// physical line it started on, including rows whose quoted fields span several lines. That number
/// is what the import report points the steward at, and a reader that only yields records cannot
/// produce it.
/// </para>
/// </summary>
internal static class DelimitedTable
{
    /// <summary>
    /// Delimiters worth sniffing for. Comma is the norm; tab arrives from anything pasted out of a
    /// grid, and semicolon from Excel on a locale that uses the comma as its decimal separator -
    /// which is most of the exports that will be handed to this module.
    /// </summary>
    private static readonly char[] Candidates = [',', '\t', ';', '|'];

    public static (IReadOnlyList<string> Header, IReadOnlyList<MasterDataImportRow> Rows, string? Error)
        Read(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ([], [], "The file is empty.");
        }

        var records = Split(text.TrimStart('\uFEFF'), SniffDelimiter(text));

        if (records.Count == 0)
        {
            return ([], [], "The file has no header row.");
        }

        var header = records[0].Fields;

        if (header.All(string.IsNullOrWhiteSpace))
        {
            return ([], [], "The file has no header row.");
        }

        var keys = header.Select(MasterDataImportHeader.Normalize).ToList();
        var rows = new List<MasterDataImportRow>(records.Count - 1);

        foreach (var record in records.Skip(1))
        {
            var values = new Dictionary<string, string?>(keys.Count);

            for (var i = 0; i < keys.Count; i++)
            {
                // A duplicated header would otherwise throw on insert, which reads to the steward
                // as a crash rather than as the file problem it is. First column of that name wins.
                if (keys[i].Length > 0 && !values.ContainsKey(keys[i]))
                {
                    values[keys[i]] = i < record.Fields.Count ? record.Fields[i] : null;
                }
            }

            rows.Add(new MasterDataImportRow(record.Line, values));
        }

        return (header, rows, null);
    }

    /// <summary>
    /// Picks the delimiter by counting candidates in the header line. Guessing beats asking: the
    /// steward exporting from nSFS does not know which separator their Excel wrote.
    /// </summary>
    private static char SniffDelimiter(string text)
    {
        var firstLine = text.Split('\n', 2)[0];

        return Candidates
            .Select(c => (Char: c, Count: firstLine.Count(x => x == c)))
            .OrderByDescending(x => x.Count)
            .First() is { Count: > 0 } best
            ? best.Char
            : ',';
    }

    private static List<(int Line, List<string> Fields)> Split(string text, char delimiter)
    {
        var records = new List<(int, List<string>)>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var recordLine = 1;

        void EndField() => fields.Add(field.ToString().Trim());

        void EndRecord()
        {
            EndField();

            // Trailing newlines and blank separator lines are not rows. Only a record that is
            // entirely empty is dropped - a row with one populated column is a row with errors,
            // and the steward needs to be told about it rather than have it disappear.
            if (fields.Any(f => f.Length > 0))
            {
                records.Add((recordLine, [..fields]));
            }

            fields.Clear();
            field.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    if (c == '\n')
                    {
                        line++;
                    }

                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                // A quote only opens a field at its start; one in the middle of a bare value is a
                // literal character, which is how legacy names like 5" hose survive the trip.
                case '"' when field.Length == 0:
                    inQuotes = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRecord();
                    line++;
                    recordLine = line;
                    break;
                default:
                    if (c == delimiter)
                    {
                        EndField();
                        field.Clear();
                    }
                    else
                    {
                        field.Append(c);
                    }

                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            EndRecord();
        }

        return records;
    }

    /// <summary>Quotes a value for the template file. Nothing reads these back, but Excel does.</summary>
    public static string Quote(string? value) =>
        value is null ? "" : value.Any(c => c is ',' or '"' or '\n' or '\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
