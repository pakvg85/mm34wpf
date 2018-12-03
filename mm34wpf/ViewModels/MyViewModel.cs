using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Input;

namespace mm34wpf
{
    public class MyViewModel : ViewModelBaseWithStore
    {
        private MyModel _myModel;

        public string InputText { get => Get<string>(); set => Set(value); }
        public string InputMask { get => Get<string>(); set => Set(value); }
        public string InputOpenBracket { get => Get<string>(); set => Set(value); }
        public string InputCloseBracket { get => Get<string>(); set => Set(value); }
        public string OutputMask { get => Get<string>(); set => Set(value); }
        public string OutputOpenBracket { get => Get<string>(); set => Set(value); }
        public string OutputCloseBracket { get => Get<string>(); set => Set(value); }
        public string OutputText { get => Get<string>(); set => Set(value); }
        public int TabcontrolSelectedIndex { get => Get<int>(); set => Set(value); }
        public DataTable InputParsed { get => Get<DataTable>(); set => Set(value); }
        public MyVarList InputVarList { get => Get<MyVarList>(); set => Set(value); }
        public MyVarList OutputVarList { get => Get<MyVarList>(); set => Set(value); }

        public string StatusBarText
        {
            get => Get<string>();
            private set => Set(value);
        }

        private ICommand _command1;
        public ICommand Command1 => _command1 ?? (_command1 = new RelayCommand(obj => ProcessParseInput()));
        private ICommand _command2;
        public ICommand Command2 => _command2 ?? (_command2 = new RelayCommand(obj => ProcessFillPreview()));

        public Logger GlobalLogger = LogManager.GetCurrentClassLogger();

        public MyViewModel()
        {
            InputText =
                "\r\n[11.10.2010 21:06:45] Владимир говорит: Светлана, добрый вечер"
                + "\r\n[11.10.2010 21:07:04] Светлана говорит: Добрый вечер"
                + "\r\n[11.10.2010 21:07:32] Владимир говорит: я в почту зайти не могу"
                + "\r\nфайлы с тестами созранил но письма посмотреть не могу"
                + "\r\nсмотрю файл тест косв"
                + "\r\nкосвенные расходы формирутся неправильно?";
            InputMask = "\r\n[%d% %t%] %n% говорит: %text%";
            OutputMask = "\r\n[%d% %t%] %n% говорит: %text%";
            InputOpenBracket = Properties.Settings.Default.defaultOpenBracket;
            InputCloseBracket = Properties.Settings.Default.defaultCloseBracket;
            OutputOpenBracket = Properties.Settings.Default.defaultOpenBracket;
            OutputCloseBracket = Properties.Settings.Default.defaultCloseBracket;

            ProcessMessage("Это статусбар. Сюда будут выводиться сообщения об ошибках");

            InputVarList = new MyVarList();
            OutputVarList = new MyVarList();
            _myModel = new MyModel();

            PropertyChanged += MyViewModel_PropertyChanged;
        }

        private void ProcessException(Exception ex, bool isFatal = false)
        {
            ProcessMessage(ex.Message + " (детальное инфо об ошибке - в файле лога в папке с программой)");

            if (isFatal)
                GlobalLogger.Fatal(ex, "UNHANDLED EXCEPTION");
            else
                GlobalLogger.Error(ex);
        }

        private void ProcessMessage(string msg)
        {
            StatusBarText = msg;
        }

        private void MyViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(InputMask):
                    case nameof(InputOpenBracket):
                    case nameof(InputCloseBracket):
                        _myModel.ParseMask(InputMask, InputOpenBracket, InputCloseBracket, InputVarList);
                        break;
                    case nameof(OutputMask):
                    case nameof(OutputOpenBracket):
                    case nameof(OutputCloseBracket):
                        _myModel.ParseMask(OutputMask, OutputOpenBracket, OutputCloseBracket, OutputVarList);
                        break;
                }
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        public void ProcessParseInput()
        {
            try
            {
                _myModel.ParseMask(InputMask, InputOpenBracket, InputCloseBracket, InputVarList);
                var rows = _myModel.ParseInputText(InputText, InputVarList);
                InputParsed = _myModel.BuildDataTable(InputVarList.Columns, rows);

                TabcontrolSelectedIndex++;

                ProcessMessage("Входная строка распарсена успешно");
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        public void ProcessFillPreview()
        {
            try
            {
                // TODO: переделать работу с InputParsed
                if (InputParsed == null)
                    throw new Exception("Первый шаг пропущен. Вернитесь на предыдущую страницу и нажмите next");

                _myModel.ParseMask(OutputMask, OutputOpenBracket, OutputCloseBracket, OutputVarList);
                var tableColumnCaptions = InputParsed
                    .Columns.Cast<DataColumn>()
                    .Select(x => x.Caption)
                    .ToArray();
                var tableRows = InputParsed.Rows.OfType<DataRow>();
                var tableValues = tableRows.Select(x => x.ItemArray.ToList());
                OutputText = _myModel.BuildOutputText(tableValues, tableColumnCaptions, OutputVarList);

                TabcontrolSelectedIndex++;

                ProcessMessage("Выходная строка сформирована успешно");
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }
    }
}