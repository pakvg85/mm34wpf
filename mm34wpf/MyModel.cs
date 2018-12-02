using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace mm34wpf
{
    public class MyModel
    {
        public const string DefaultRegexString = ".*?";

        public void ParseMask(string maskSrc, string openBracket, string closeBracket, IList<MyVar> varList)
        {
            if (string.IsNullOrEmpty(maskSrc) || string.IsNullOrEmpty(openBracket) || string.IsNullOrEmpty(closeBracket))
                throw new Exception($"Маска и открывающая/закрывающая скобка не должны быть пустыми");

            // (?<prefix>.*?)(?:%)(?<varName>.*?)(?:%)(?=$|%.*?%)

            var regexMaskString = $"(?<prefix>.*?)(?:{Regex.Escape(openBracket)})(?<varname>.*?)(?:{Regex.Escape(closeBracket)})"; // (?=$|.*?{openBracket}.*?{closeBracket})
            var regexMask = new Regex(regexMaskString, RegexOptions.Singleline);
            //var src = maskSrc + $"{openBracket}{closeBracket}";
            var matchesMask = regexMask.Matches(maskSrc);
            if (matchesMask.Count == 0)
            {
                varList.Clear();
                throw new Exception($"Не удалось распарсить входную маску. Проверьте открывающую/закрывающую скобки и текст маски. В маске должна быть хотя бы одна переменная");
            }

            var newMyVarList = new List<MyVar>();
            int lastPos = -1;
            foreach (Match match in matchesMask)
            {
                if (match.Groups.Count != 3)
                {
                    throw new Exception($"Ошибка при парсинге переменных в строке {match.Value}");
                }

                int index = -1;
                string newVarPrefix = null;
                string newCaption = null;
                foreach (Group group in match.Groups)
                {
                    index++;

                    switch (index)
                    {
                        case 1: newVarPrefix = Regex.Escape(group.Value); break;
                        case 2: newCaption = Regex.Escape(group.Value); break;
                    }
                }
                if (newMyVarList.Any(x => x.Caption == newCaption))
                    throw new Exception($"Переменная с именем {newCaption} уже используется в этой маске. Задайте другое имя");

                newMyVarList.Add(new MyVar
                {
                    Caption = newCaption,
                    Prefix = newVarPrefix,
                    RegexString = DefaultRegexString
                });

                lastPos = match.Index + match.Length;
            }
            var newPostfixVar = new MyVar { IsPostfix = true, Prefix = Regex.Escape(maskSrc.Substring(lastPos)) };
            newMyVarList.Add(newPostfixVar);

            varList.RemoveByPredicate(x => !newMyVarList.Any(i => i.Caption == x.Caption));

            var existIndex = -1;
            foreach (var newMyVar in newMyVarList)
            {
                var existMyVar = varList.FirstOrDefault(x => x.Caption == newMyVar.Caption);
                if (existMyVar != null)
                {
                    existIndex++;
                    existMyVar.Prefix = newMyVar.Prefix;
                }
                else
                {
                    existIndex++;
                    varList.Insert(existIndex, newMyVar);
                }
            }

            existIndex = -1;
            foreach (var myVar in varList)
            {
                existIndex++;
                myVar.Id = "GROUP" + existIndex.ToString();
            }
        }

        public IEnumerable<IEnumerable<string>> ParseInputText(string srcInputText, IEnumerable<MyVar> myVars)
        {
            // "(?:<prefix1>)(?<var1><regex1>) ..... (?=$|<prefix1><regex1> .....)"
            // Постфикс (?=.....) нужен чтобы последняя переменная правильно вытаскивалась.
            // Если его убрать, то регексп ".*?" вернет string.Empty

            var result = new List<IEnumerable<string>>();

            var regexInputString = string.Empty;
            var regexInputPostfixString = string.Empty;
            foreach (var myVar in myVars)
            {
                regexInputString += $"(?:{myVar.Prefix})";
                regexInputPostfixString += $"{myVar.Prefix}";
                if (!myVar.IsPostfix)
                {
                    regexInputString += $"(?<{myVar.Id}>{myVar.RegexString})";
                    regexInputPostfixString += $"{myVar.RegexString}";
                }
            }
            if (string.IsNullOrEmpty(regexInputString))
                return result;

            regexInputPostfixString = $"(?=$|{regexInputPostfixString})";
            regexInputString += regexInputPostfixString;

            var regexInput = new Regex(regexInputString, RegexOptions.Singleline);
            var matchesInput = regexInput.Matches(srcInputText);

            foreach (Match match in matchesInput)
            {
                if (match.Groups.Count != myVars.Count())
                {
                    throw new Exception($"Ошибка при парсинге переменных в строке {match.Value}");
                }

                int index = -1;
                var row = new List<string>();
                foreach (Group group in match.Groups)
                {
                    index++;

                    if (index > 0)
                        row.Add(group.Value);
                }
                result.Add(row);
            }

            return result;
        }

        public DataTable BuildDataTable(IEnumerable<Tuple<string, string>> columns, IEnumerable<IEnumerable<string>> rows)
        {
            var dataGrid = new DataTable();

            var indexColumn = new DataColumn("INDEX_COLUMN")
            {
                Caption = "№ п/п"
            };
            dataGrid.Columns.Add(indexColumn);
            foreach (var col in columns)
            {
                var newColumn = new DataColumn(col.Item1)
                {
                    Caption = col.Item2
                };
                dataGrid.Columns.Add(newColumn);
            }
            var rowIndex = -1;
            foreach (var row in rows)
            {
                rowIndex++;
                var newRow = dataGrid.NewRow();

                int cellIndex = 0;
                newRow.SetField(cellIndex, rowIndex.ToString());

                foreach (var cellValue in row)
                {
                    cellIndex++;
                    newRow.SetField(cellIndex, cellValue);
                }
                dataGrid.Rows.Add(newRow);
            }

            return dataGrid;
        }

        public string BuildOutputText(IEnumerable<IEnumerable<object>> tableValues, IEnumerable<string> tableColumnCaptions, IEnumerable<MyVar> outputVarList)
        {
            string result = string.Empty;

            var colDict = tableColumnCaptions
                .Select((x, i) => new { ColumnCaption = x, ColumnIndex = i })
                .ToDictionary(x => x.ColumnCaption, i => i.ColumnIndex);

            foreach (var iterVar in outputVarList)
            {
                if (!iterVar.IsPostfix && !colDict.ContainsKey(iterVar.Caption))
                {
                    throw new Exception($"Выходная маска не соответствует входной маске; проблема в переменной '{iterVar.Caption}'. Проверьте правильность заполнения входной и выходной маски");
                }
            }

            foreach (var row in tableValues)
            {
                foreach (var iterVar in outputVarList)
                {
                    result += Regex.Unescape(iterVar.Prefix);
                    if (!iterVar.IsPostfix)
                    {
                        var cell = row.ElementAt(colDict[iterVar.Caption]);
                        result += cell.ToString();
                    }
                }

            }

            return result;
        }
    }
}