using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GradingCommentary.Code;
using Microsoft.Win32;

namespace GradingCommentary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _allGrades;
        private bool _relative;
        public MainWindow()
        {
            InitializeComponent();
            var display = Settings.Default.Display;
            Options.ItemsSource = display.Select(x => x.Name).ToArray();
            Options.SelectedIndex = Enumerable.Range(0, display.Count).FirstOrDefault(i => display[i].Default);
            ShowMistakesCheckbox.IsChecked = false;
            ExecuteOptions();
        }

        private void ExecuteOptions()
        {
            if (Options == null) return;
            var item = Options.SelectedValue.ToString();
            var mistakes = ShowMistakesCheckbox.IsChecked ?? false;
            _allGrades = !mistakes;
            _relative = RelativeCheck.IsChecked ?? false;
            Grade.SetDisplayAdapter(Settings.Default.Display[item]);
        }

        private void ToGradeable_Click(object sender, RoutedEventArgs e)
        {
            var problems = new ProblemMapping(ProblemsText.Text);
            var students = problems.GetFillers(StudentsText.Text);
            StudentsText.Text = ProblemMapping.ToCsv(students, "\t");
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textbox = (TextBox) sender;
            textbox.SelectAll();
            e.Handled = true;
        }

        private void Blackboard_Click(object sender, RoutedEventArgs e)
        {
            var problems = new ProblemMapping(ProblemsText.Text);
            var students = problems.GetFillers(StudentsText.Text);
            SaveToCsv(problems, students);
        }

        private void UpdateDefaultStudents_Click(object sender, RoutedEventArgs e)
        {
            var problems = new ProblemMapping(ProblemsText.Text);
            var students = problems.GetFillers(StudentsText.Text);
            var defaultStudents =
                from grade in Grade.FetchAll(problems, students, _allGrades, _relative)
                select grade.PopulateDefaultGrades();
            StudentsText.Text = ProblemMapping.ToCsv(defaultStudents.ToArray(), "\t");
        }

        private void ProblemsText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ExecuteOptions();
        }

        private void DropObject(object sender, DragEventArgs e)
        {
            var filename = ((string[]) e.Data.GetData(DataFormats.FileDrop, true)).FirstOrDefault();
            if (filename != null)
            {
                using (var processor = new ExcelProcessor(filename))
                {
                    var problems = new ProblemMapping(processor.FetchProblems());
                    var students = problems.GetFillers(processor.FetchStudents());
                    SaveToCsv(problems, students);
                }
            }
        }

        private void SaveToCsv(ProblemMapping problems, ICollection<IDictionary<string, string>> students)
        {
            var gradedStudents = Grade.FetchAll(problems, students, _allGrades, _relative).Select(grade => grade.Student).ToArray();
            var csv = ProblemMapping.ToCsv(gradedStudents);
            var dialog = new SaveFileDialog { DefaultExt = "csv", Filter = "Comma Separated Values|*.csv|All files|*.*" };
            if (dialog.ShowDialog() == true)
            {
                using (var stream = dialog.OpenFile())
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(csv);
                }
                System.Diagnostics.Process.Start(dialog.FileName);
            }

        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        private void ShowMistakesCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ExecuteOptions();
        }
    }
}
