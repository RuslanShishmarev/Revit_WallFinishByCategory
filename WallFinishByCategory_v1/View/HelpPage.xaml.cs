using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WallFinishByCategory_v1.View
{
    /// <summary>
    /// Логика взаимодействия для HelpPage.xaml
    /// </summary>
    public partial class HelpPage : Window
    {
        string _projectLanguage;
        List<string> languageList = new List<string>() { "RUS", "ENG" };
        public HelpPage(string projectLanguage)
        {
            InitializeComponent();
            _projectLanguage = projectLanguage;
            
            LanguageCheckView.ItemsSource = languageList;
            if (_projectLanguage == "Russian")
                LanguageCheckView.SelectedItem = languageList[0];
            else
                LanguageCheckView.SelectedItem = languageList[1];
        }

        private void LanguageCheckView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCheckView.SelectedItem.ToString() == languageList[0])
            {
                RecLabelView.Content = "Рекомендации";
                RecText1.Text = "1. Перед использованием плагина, убедитесь, что у помещений заполнен параметр 'Назначение'. " +
                    "Плагин считывает этот параметр и формирует категории помещений по значению параметра.";
                RecText2.Text = "2. Так же предварительно можно создать два параметра для хранения данных по отделке помещений: " +
                    "info_Отделка_НомерПомещения, info_Отделка_ИмяПомещения. Категория параметра - 'Стены'. Тип данных - 'Текст'.";
                RecText3.Text = "3. У стен, которые окружают помещения, нужно заполнить параметр типа 'Группа модели'. " +
                    "Это необходимо, если вы планируете назначить разный тип отделки на данные типы. " +
                    "Например, данный параметр можно заполнить в зависимости от материала стен (Тип: Стена кирпичная 120мм; Группа модели = 'Кирпич').";
                InstLabelView.Content = "Инструкция";
                InstText1.Text = "1. Выберете категорию помещений.";
                InstText2.Text = "2. Выберете тип отделки на определенные категории стен.";
                InstText3.Text = "3. Задайте один общий тип отделки по умолчанию.";
                InstText4.Text = "4. Выберете уровни, где необходимо построить отделку.";
                InstText5.Text = "5. Нажмите 'Начать'.";
                InstText6.Text = "P.S. Если проект работает в совместной работе, то можно выбрать определенный рабочий набор для отделки.";
                InstText7.Text = "Так же вы можете выбрать конкретные помещения в списке помещений, в которых нужно построить отделку.";
            }
            else
            {
                RecLabelView.Content = "Recommendation";
                RecText1.Text = "1. Make sure that the rooms have filled parameter 'Department' before using. " +
                    "Plugin gets this parameter and create the room categories.";
                RecText2.Text = "2. Then you can create 2 parameters for wall finishing data: " +
                    "info_WallFinish_RoomNumber, info_WallFinish_RoomName. Parameter's category - 'Walls', parameter's type - 'Text'.";
                RecText3.Text = "3. For walls that surround rooms, you need to fill the type parameter 'Model'.";
                InstLabelView.Content = "Instruction";
                InstText1.Text = "1. Select the room category.";
                InstText2.Text = "2. Select wall finish type for certain categories of walls.";
                InstText3.Text = "3. Set one general default finish.";
                InstText4.Text = "4. Select the levels where finish should be created.";
                InstText5.Text = "5. Click on 'Start' button.";
                InstText6.Text = "P.S. If your project is worksharing you can select the necessary workset for finish.";
                InstText7.Text = "You can also select specific rooms in the list of rooms in which you want to build finishing.";
            }
        }
    }
}
