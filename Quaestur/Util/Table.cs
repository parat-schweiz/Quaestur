using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public abstract class CellContent
    {
        public abstract string Render();
    }

    public class TextCellContent : CellContent
    {
        public string Text { get; private set; }

        public TextCellContent(string text)
        {
            Text = text;
        }

        public override string Render()
        {
            return Text;
        }
    }

    public abstract class Cell
    {
        public CellContent Content { get; private set; }
        public int ColumnSpan { get; private set; }
        public int RowSpan { get; private set; }
        public IEnumerable<string> Classes { get; private set; }

        public Cell(CellContent content, int columnSpan, int rowSpan, IEnumerable<string> classes)
        {
            Content = content;
            ColumnSpan = columnSpan;
            RowSpan = rowSpan;
            Classes = classes;
        }

        protected string Attributes
        {
            get 
            {
                var attributes = new StringBuilder();
                if ((Classes != null) && Classes.Any())
                {
                    attributes.Append(string.Format(" class=\"{0}\"", string.Join(" ", Classes)));
                }
                if (ColumnSpan > 1)
                {
                    attributes.Append(string.Format(" colspan=\"{0}\"", ColumnSpan));
                }
                if (RowSpan > 1)
                {
                    attributes.Append(string.Format(" rowspan=\"{0}\"", RowSpan));
                }
                return attributes.ToString();
            }
        }

        public abstract string Render { get; }
    }

    public class HeaderCell : Cell
    {
        public HeaderCell(CellContent content, int columnSpan, int rowSpan, IEnumerable<string> classes)
         : base(content, columnSpan, rowSpan, classes)
        {
        }

        public override string Render
        {
            get
            {
                return string.Format("<th{0}>{1}</th>", Attributes, Content.Render());
            }
        }
    }

    public class DataCell : Cell
    {
        public DataCell(CellContent content, int columnSpan, int rowSpan, IEnumerable<string> classes)
         : base(content, columnSpan, rowSpan, classes)
        {
        }

        public override string Render
        {
            get
            {
                return string.Format("<td{0}>{1}</td>", Attributes, Content.Render());
            }
        }
    }

    public class Row
    {
        private readonly List<Cell> _cells;

        public void Add(Cell cell)
        {
            _cells.Add(cell);
        }

        public string Render
        {
            get
            {
                var result = new StringBuilder();
                foreach (var cell in _cells)
                {
                    result.AppendLine(cell.Render);
                }
                return result.ToString();
            }
        }
    }

    public abstract class Column
    { 
        public string HeaderText { get; private set; }

        public Column(string headerText)
        {
            HeaderText = headerText;
        }
    }

    public abstract class Column<T> : Column
        where T : DatabaseObject, new()
    {
        public abstract string Content(T obj, Translator translator);

        public Column(string headerText)
            : base(headerText)
        {
        }
    }

    public class FieldColumn<T> : Column<T>
        where T : DatabaseObject, new()
    {
        private readonly Func<T, Field> _field;

        public FieldColumn(string headerText, Func<T, Field> field)
            : base(headerText)
        {
            _field = field;
        }

        public override string Content(T obj, Translator translator)
        {
            return _field(obj).GetText(translator);
        }
    }

    public class FormatColumn<T> : Column<T>
        where T : DatabaseObject, new()
    {
        private readonly Func<T, string> _format;

        public FormatColumn(string headerText, Func<T, string> format)
            : base(headerText)
        {
            _format = format;
        }

        public override string Content(T obj, Translator translator)
        {
            return _format(obj);
        }
    }

    public abstract class Table<T>
        where T : DatabaseObject, new()
    {
        private readonly Translator _translator;
        private readonly List<T> _objs;

        public Table(Translator translator, IEnumerable<T> objs)
        {
            _translator = translator;
            _objs = objs.ToList();
        }

        protected abstract IEnumerable<Column<T>> Columns { get; }

        protected virtual IEnumerable<Row> ExtraHeaders { get { return new List<Row>(); } }

        private IEnumerable<Row> Rows
        {
            get
            {
                foreach (var row in ExtraHeaders)
                {
                    yield return row;
                }

                {
                    var row = new Row();

                    foreach (var column in Columns)
                    {
                        row.Add(new HeaderCell(new TextCellContent(column.HeaderText)));
                    }

                    yield return row;
                }

                foreach (var obj in _objs)
                {
                    var row = new Row();

                    foreach (var column in Columns)
                    {
                        var content = column.Content(obj, _translator);
                        row.Add(new DataCell(new TextCellContent(content)));
                    }

                    yield return row;
                }
            }
        }
    }
}
