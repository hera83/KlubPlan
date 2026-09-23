using System.Globalization;
using ClosedXML.Excel;
using web.Constants;
using web.Repositories.ActivityLists.Dtos;

namespace web.Repositories.ActivityLists
{
    /// <summary>Reads an uploaded sheet into headers/rows and writes a list back out as .xlsx (ClosedXML).</summary>
    public static class ActivityListExcel
    {
        /// <summary>Light fills for the status column in the export, per status color key.</summary>
        private static readonly Dictionary<string, XLColor> StatusFills = new()
        {
            ["muted"] = XLColor.FromHtml("#EEF0F3"),
            ["info"] = XLColor.FromHtml("#DDEBFF"),
            ["ok"] = XLColor.FromHtml("#DDF3E4"),
            ["warn"] = XLColor.FromHtml("#FBEFD5"),
            ["alarm"] = XLColor.FromHtml("#FBDEDC")
        };

        /// <summary>
        /// First visible sheet with content: its first used row becomes the headers, the rest the rows.
        /// Cells are read as displayed in Excel (dates/numbers formatted), empty rows are skipped, empty
        /// header cells become "Kolonne N" and duplicate headers get " (2)", " (3)" ….
        /// </summary>
        public static ActivityListImportDto Parse(Stream content)
        {
            var result = new ActivityListImportDto();

            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(content);
            }
            catch (Exception)
            {
                result.ErrorMessage = "Filen kunne ikke læses som et Excel-ark (.xlsx).";
                return result;
            }

            using (workbook)
            {
                var sheet = workbook.Worksheets.FirstOrDefault(ws => ws.Visibility == XLWorksheetVisibility.Visible && ws.RangeUsed() is not null);
                var range = sheet?.RangeUsed();
                if (sheet is null || range is null)
                {
                    result.ErrorMessage = "Excel-arket er tomt.";
                    return result;
                }

                var firstRow = range.FirstRow().RowNumber();
                var lastRow = range.LastRow().RowNumber();
                var firstCol = range.FirstColumn().ColumnNumber();
                var lastCol = range.LastColumn().ColumnNumber();

                if (lastCol - firstCol + 1 > ActivityListRules.MaxImportColumns)
                {
                    result.ErrorMessage = $"Arket har for mange kolonner (maks. {ActivityListRules.MaxImportColumns}).";
                    return result;
                }
                if (lastRow - firstRow > ActivityListRules.MaxImportRows)
                {
                    result.ErrorMessage = $"Arket har for mange rækker (maks. {ActivityListRules.MaxImportRows.ToString("N0", CultureInfo.GetCultureInfo("da-DK"))}).";
                    return result;
                }

                string? Read(int row, int col)
                {
                    var text = sheet.Cell(row, col).GetFormattedString()?.Trim();
                    if (string.IsNullOrEmpty(text))
                        return null;
                    return text.Length > ActivityListRules.MaxCellLength ? text[..ActivityListRules.MaxCellLength] : text;
                }

                var columns = Enumerable.Range(firstCol, lastCol - firstCol + 1).ToList();
                var rawRows = new List<string?[]>();
                for (var r = firstRow + 1; r <= lastRow; r++)
                {
                    var values = columns.Select(c => Read(r, c)).ToArray();
                    if (values.Any(v => v is not null))
                        rawRows.Add(values);
                }

                // Drop columns with neither a header nor any data (e.g. formatted but empty cells at the edge).
                var keep = new List<int>();
                var headers = new List<string>();
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                {
                    var header = Read(firstRow, columns[i]);
                    if (header is null && rawRows.All(row => row[i] is null))
                        continue;

                    var name = header ?? $"Kolonne {i + 1}";
                    if (name.Length > 200)
                        name = name[..200];
                    var unique = name;
                    for (var n = 2; !used.Add(unique); n++)
                        unique = $"{name} ({n})";

                    keep.Add(i);
                    headers.Add(unique);
                }

                if (headers.Count == 0)
                {
                    result.ErrorMessage = "Excel-arket har ingen kolonner.";
                    return result;
                }

                result.Headers = headers;
                result.Rows = rawRows.Select(row => keep.Select(i => row[i]).ToArray()).ToList();
                return result;
            }
        }

        /// <summary>One sheet with a bold, frozen, filterable header row. statusColorColumn (0-based) gets a fill per row.</summary>
        public static byte[] Build(string sheetTitle, IReadOnlyList<string> headers, IEnumerable<(string?[] Cells, string? StatusColor)> rows, int statusColorColumn)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SheetName(sheetTitle));

            for (var c = 0; c < headers.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
            }

            var header = sheet.Range(1, 1, 1, headers.Count);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8ECF1");

            var rowNumber = 2;
            foreach (var (cells, statusColor) in rows)
            {
                for (var c = 0; c < cells.Length; c++)
                {
                    // Always written as text, so values like postal codes or "007" keep their exact form.
                    sheet.Cell(rowNumber, c + 1).SetValue(cells[c] ?? string.Empty);
                }
                if (statusColor is not null && StatusFills.TryGetValue(statusColor, out var fill))
                {
                    sheet.Cell(rowNumber, statusColorColumn + 1).Style.Fill.BackgroundColor = fill;
                }
                rowNumber++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, Math.Max(1, rowNumber - 1), headers.Count).SetAutoFilter();
            sheet.Columns(1, headers.Count).AdjustToContents(1, Math.Min(rowNumber - 1, 500), 8, 60);

            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }

        private static string SheetName(string title)
        {
            var invalid = new[] { '[', ']', ':', '*', '?', '/', '\\' };
            var cleaned = new string(title.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray()).Trim().Trim('\'');
            if (cleaned.Length == 0)
                cleaned = "Liste";
            return cleaned.Length > 31 ? cleaned[..31] : cleaned;
        }
    }
}
