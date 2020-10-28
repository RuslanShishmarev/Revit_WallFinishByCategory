using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WallFinishByCategory.Model;

namespace WallFinishByCategory_v1.View
{
    /// <summary>
    /// the plagin works with room boundary segments
    /// 1) Plagin get the all rooms, document from the App.cs
    /// 2) Create dictionary for rooms by categories for user selection and create the buttons with these categories after Initialize()
    /// 3) After Category button click the plagin get all rooms from dctionary by the name of button
    ///     3.1) Get all levels by rooms and model group of walls around the rooms
    ///     3.2) Put the model groups of walls as checkbox to view 
    /// 4) Then plagin start work after startwork button click
    ///     4.1) If category or levels checkbox or default wall type in default checkbox is not selected, plugin will not work
    ///     4.2) Then programm go through rooms by selected category and gets all rooms only in selected levels
    ///     4.3) Check one wall instance from first room in selected by category for parameter existing (ROOM_NUMBER_PARAM, ROOM_NAME_PARAM)
    ///     4.4) Go through all rooms by selected category and levels. Start loop
    ///         4.4.1) Get new axis for new wall
    ///             4.4.2) The main problem - get new axis for the new walls due the ProtoGeometry.dll. 
    ///             I this dll there is method to create the PolyCyrve on the Autodesk.DesignScript.Geometry.Curve. 
    ///             When we get the boundary segments in room, we get all curves like Autodesk.Revit.DB.Curve.
    ///             It should be converted to Autodesk.DesignScript.Geometry.Curve with RevitToProtoCurve.ToProtoType method from ProtoGeometry.dll.
    ///             A few moments later this dll doesn't work. Because I use the axis from room, create model curve, move after, get curve and delete after. 
    ///             And then we have problem with moving model curves with Ark location curve. 
    ///             When base line's name is Line, plagin create the Plane on room curve and get the vector by plane normal.
    ///             In RoomWoker.cs there is method to get new curves with ProtoGeometry.dll (GetNewWallCurveWithProto).
    ///         4.4.2) Create the walls and set room parameters (name and number) to instance
    ///         4.4.3) Join new walls with room walls.         
    /// </summary>
    public partial class UserWindow : Window
    {
        private string categoryBtnColorClick = "#bbeafa";
        private string categoryBtnColorStandart = "#FFDDDDDD";
        
        //create the list for room's departmens/categories
        List<String> allCategoryName = new List<String>();
        //create the list for dictionary for rooms by categories
        Dictionary<String, List<Room>> allRoomsByCategory = new Dictionary<string, List<Room>>();
        Document _doc;
        bool _worksharedStatus;
        List <WallType> _allWallType = new List<WallType>();
        string selectedCategoryName = "";
        List<Room> roomBySelectedCategory = new List<Room>();
        List<Level> levelByRoom = new List<Level>();
        String projectLanguage;
        //CONSTANT STRING
        //************************************************************************************
        string ROOM_NUMBER_PARAM = "info_WallFinish_RoomNumber";
        string ROOM_NAME_PARAM = "info_WallFinish_RoomName";
        string WITHOUT_CATEGORY_NAME = "Not filled!";
        string LEVEL_CHECK_MESSAGE = "Select the level!";
        string ROOM_CATEGORY_MESSAGE = "Select a room category!";
        string DEFAULT_FINISH_TYPE_MESSAGE = "Select a default finish type!";
        string READY_WORK_MESSAGE = "Ready!\n" + "Count of all new wall finish: ";
        string ERROR_PARAMETER_MESSAGE = "Please, check the paramenters for room name and number:\n";
        //************************************************************************************
        public UserWindow(Document doc, bool worksharedStatus, ICollection <Element> allRoomsCollection, List<WallType> allWallType)
        {
            InitializeComponent();
            _doc = doc;
            _worksharedStatus = worksharedStatus;
            _allWallType = allWallType;
            
            projectLanguage = _doc.Application.Language.ToString();
            if(projectLanguage == "Russian")
            {
                //message
                ROOM_NUMBER_PARAM = "info_Отделка_НомерПомещения";
                ROOM_NAME_PARAM = "info_Отделка_ИмяПомещения";
                WITHOUT_CATEGORY_NAME = "Не назначено!";
                LEVEL_CHECK_MESSAGE = "Выберете уровень!";
                ROOM_CATEGORY_MESSAGE = "Выберете категорию помещений!";
                DEFAULT_FINISH_TYPE_MESSAGE = "Выберете тип отделки по умолчанию!";
                READY_WORK_MESSAGE = "Готово! \n" + "Всего новых стен отделки: ";
                ERROR_PARAMETER_MESSAGE = "Проверьте наличие параметров стен для хранения помещения:\n";
                //labels and buttons
                CategoryLabelView.Content = "Категории";
                RoomListLabelView.Content = "Список комнат";
                TypeSelectListLabelView.Content = "Выбор типа";
                LevelSelectListLabelView.Content = "Уровни";
                DefaultLabelView.Content = "По умолчанию";
                FinishTypeLabelView.Content = "Тип отделки";
                WorksetLabelView.Content = "Рабочий набор";
                StartCreaterView.Content = "Начать";
                AllLevelsButtonLabelView.Text = "Все этажи";
                NoneLevelsButtonLabelView.Text = "Снять выделение";
                HelpAuthorButtonView.Content = "Помочь автору:)";
            }
            
            if (_worksharedStatus)
            {
                FilteredWorksetCollector filterWorkset = new FilteredWorksetCollector(_doc);
                WorksetsViewList.ItemsSource = filterWorkset.OfKind(WorksetKind.UserWorkset);
                WorksetsViewList.DisplayMemberPath = "Name";
            }
            else WorksetsViewPanel.Visibility = System.Windows.Visibility.Hidden;
            DefaultFinishType.ItemsSource = _allWallType;
            DefaultFinishType.DisplayMemberPath = "Name";
            //go through all the rooms and create dictionary for rooms by categories
            foreach (Element roomEl in allRoomsCollection)
            {
                //get the parameter for category
                Parameter categoryParameter = roomEl.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT);
                //get category name
                String categoryName = categoryParameter.AsString();

                if (categoryName == null) categoryName = WITHOUT_CATEGORY_NAME;

                //get list from dictionary
                List<Room> listFromDict;
                //is category exist?
                if (!allRoomsByCategory.TryGetValue(categoryName, out listFromDict))
                {
                    //put the name of categories to list
                    allCategoryName.Add(categoryName);

                    //create list for unique parameters's value
                    listFromDict = new List<Room>();
                    Room room = roomEl as Room;
                    if (room.Area != 0) listFromDict.Add(room);

                    //Add to the dictionary
                    allRoomsByCategory.Add(categoryName, listFromDict);
                }
                else
                {
                    //update the value of dictionary
                    Room room = roomEl as Room;
                    if (room.Area != 0) listFromDict.Add(room);
                    //Add to the dictionary
                    //allRoomsByCategory.Add(categoryName, listFromDict);
                }
            }
            
            Button lastBtn = null;
            foreach (String name in allRoomsByCategory.Keys)
            {
                //create the buttons
                Button newBtn = new Button();
                newBtn.Content = name;
                newBtn.Height = 30;
                newBtn.Tag = name;
                newBtn.Click += new RoutedEventHandler(CategoryBtn_Click);

                if (name == WITHOUT_CATEGORY_NAME) lastBtn = newBtn;
                else
                    AllNames.Children.Add(newBtn);
            }
            //change btn place with WITHOUT_CATEGORY_NAME
            if(lastBtn != null)
                AllNames.Children.Add(lastBtn);


        }
        private void CategoryBtn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            //change the background color
            ChangeCategoryBtnColor(btn);
            selectedCategoryName = btn.Tag.ToString();
            roomBySelectedCategory = GetRoomsFromCategory(selectedCategoryName);

            //get all levels by rooms
            levelByRoom = GetLevelRooms(roomBySelectedCategory);

            //get all wall categories
            List<string> allWallCategories = new List<string>();
            foreach (Room room in roomBySelectedCategory)
            {
                //get walls in room
                List<Wall> roomWalls = RoomWorker.GetRoomWalls(_doc, room);
                foreach (Wall wall in roomWalls)
                {
                    //get model group value
                    string modelGroup = wall.WallType.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL).AsString();
                    if (modelGroup == null) modelGroup = "Не назначено";
                    if (!allWallCategories.Contains(modelGroup)) allWallCategories.Add(modelGroup);
                }
            }

            AllCategoryWallView.Children.Clear();
            //put checkbox to view
            foreach (string category in allWallCategories)
            {
                //get all wallstype by category and put to combobox
                //create the UI element with lable and combobox
                StackPanel wallTypeLablePanel = new StackPanel();
                wallTypeLablePanel.Orientation = Orientation.Vertical;
                wallTypeLablePanel.Margin = new Thickness(0, 0, 0, 10);

                //create lable
                Label categoryLable = new Label();
                categoryLable.Content = category;

                //create combobox
                ComboBox wallTypeSelecter = new ComboBox();
                wallTypeSelecter.Text = category;
                wallTypeSelecter.Tag = category;
                wallTypeSelecter.ItemsSource = _allWallType;
                wallTypeSelecter.DisplayMemberPath = "Name";

                //put the lable and combobox to panel
                wallTypeLablePanel.Children.Add(categoryLable);
                wallTypeLablePanel.Children.Add(wallTypeSelecter);

                //put the panel to view
                AllCategoryWallView.Children.Add(wallTypeLablePanel);
            }

        }

        private void StartCreateNewWall(object sender, EventArgs e)
        {
            progressBar.Value = 0;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            //get all rooms only in selected levels
            int resultNewWallCount = 0;

            List<Room> roomBySelectedCategoryAndLevel = new List<Room>();
            foreach(Room room in roomBySelectedCategory)
            {
                if (IsLevelChecked(room.LevelId))
                    roomBySelectedCategoryAndLevel.Add(room);
            }
            
            MessageBoxResult continueProgram;
                        
            //get the default wall type and get its width
            Object selectedItem = DefaultFinishType.SelectedItem;
            //width from default wall type
            WallType defaultWallType = null;
            double defaultWallTypeWidth = 0;

            if (roomBySelectedCategory.Count == 0) MessageBox.Show(ROOM_CATEGORY_MESSAGE);
            else if (selectedItem == null) MessageBox.Show(DEFAULT_FINISH_TYPE_MESSAGE);
            else if (!IsJustOneLevelChecked() && AllLevelsView.IsEnabled) MessageBox.Show(LEVEL_CHECK_MESSAGE);
            else
            {
                //check wall instance for parameter existing
                Wall checkWall = RoomWorker.GetRoomWalls(_doc, roomBySelectedCategoryAndLevel[0])[0];
                Parameter checkPar = null;
                try
                {
                    checkPar = checkWall.LookupParameter(ROOM_NUMBER_PARAM);
                }
                catch (NullReferenceException)
                {
                    checkPar = null;
                }
                if (checkPar == null)
                {
                    continueProgram = MessageBox.Show(ERROR_PARAMETER_MESSAGE + ROOM_NUMBER_PARAM + "\n" + ROOM_NAME_PARAM, "STOP", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (continueProgram == MessageBoxResult.Cancel) return;
                }                

                defaultWallType = selectedItem as WallType;
                defaultWallTypeWidth = defaultWallType.Width;

                List<Room> roomBySelectedCategoryLevelAndItems = new List<Room>();

                //go through all rooms by selected category and levels. Start loop
                //if user want to create in selected rooms lets get room from view list
                if (GetSelectedRoomInViewList().Count != 0) roomBySelectedCategoryLevelAndItems = GetSelectedRoomInViewList();
                else roomBySelectedCategoryLevelAndItems = roomBySelectedCategoryAndLevel;

                //start the progress bar settings
                progressBar.IsIndeterminate = false;
                progressBar.Minimum = 0;
                progressBar.Maximum = roomBySelectedCategoryLevelAndItems.Count;

                foreach (Room room in roomBySelectedCategoryLevelAndItems)
                {
                    //get new axis for new wall
                    //get roomcurves
                    List<Curve> roomCurves = RoomWorker.GetRoomCurve(room);
                    List<Wall> roomWalls = RoomWorker.GetRoomWalls(_doc, room);
                    List<Wall> newWalls = new List<Wall>();
                    List<Curve> addCurves = new List<Curve>();
                    List<Curve> allWallNewAxis = new List<Curve>();
                    using (Transaction t = new Transaction(_doc))
                    {
                        t.Start("NewWallFinish");
                        allWallNewAxis = RoomWorker.GetNewWallAxisAll(_doc, room, roomCurves, defaultWallType);

                        //create the new wall and change baseline
                        ElementId selectedWorksetElementId = null;
                        if (WorksetsViewList.SelectedItem != null)
                        {
                            WorksetId selectedWorksetId = (WorksetsViewList.SelectedItem as Workset).Id;
                            selectedWorksetElementId = new ElementId(selectedWorksetId.IntegerValue);
                        }
                        foreach (Curve axis in allWallNewAxis)
                        {
                            //get vector
                            //XYZ resultVectorForWall = RoomWorker.GetVectorForNewWall(room, axis);
                            Wall newWall = null;
                            newWall = RoomWorker.CreateNewWallByCurve(_doc, room, axis, defaultWallType,
                                ROOM_NUMBER_PARAM, ROOM_NAME_PARAM, selectedWorksetElementId);
                            newWalls.Add(newWall);
                            //join with closed room wall

                            Wall roomWallToJoin = RoomWorker.FindClosedRoomWall(_doc, newWall, roomWalls);
                            try
                            {
                                if (!JoinGeometryUtils.AreElementsJoined(_doc, newWall, roomWallToJoin)
                                    && axis.Length > 1)
                                    JoinGeometryUtils.JoinGeometry(_doc, newWall, roomWallToJoin);
                            }
                            catch (Autodesk.Revit.Exceptions.ArgumentException)
                            {
                                continue;
                            }
                            //change walltype of new walls
                            String categoryWall = roomWallToJoin.WallType.get_Parameter(BuiltInParameter.ALL_MODEL_MODEL).AsString();
                            //get room wall category and search the selected wall type in combobox list
                            WallType wallType = GetNeedWallTypeFromSelected(categoryWall);
                            //change wall type for new wall
                            ChangeWallType(newWall, wallType);
                            resultNewWallCount++;
                        }
                        t.Commit();
                    }
                    //update progressbar
                    progressBar.Dispatcher.Invoke(new ProgressBarDelegate(UpdateProgress), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
                //end loop
            if (resultNewWallCount != 0) MessageBox.Show(READY_WORK_MESSAGE + resultNewWallCount.ToString());
            progressBar.Visibility = System.Windows.Visibility.Hidden;
        }
        private List<Room> GetRoomsFromCategory(String categoryName)
        {
            RoomListView.Items.Clear();
            //get all rooms by category
            List<Room> listViewRoom = allRoomsByCategory[categoryName];
            foreach (Room room in listViewRoom)
            {
                if (room.Area!=0)
                    RoomListView.Items.Add(room);
                    RoomListView.DisplayMemberPath = "Name";
            }
            return listViewRoom;
        }
        private List<Level> GetLevelRooms(List<Room> allRooms)
        {
            AllLevelsView.Children.Clear();
            List<Level> levels = new List<Level>();
            List<ElementId> levelIds = new List<ElementId>();

            foreach (Room room in allRooms)
            {
                Level levelRoom = room.Level;
                ElementId levelId = room.LevelId;

                if (!levelIds.Contains(levelId))
                {
                    //put levelId to LevelId list
                    levelIds.Add(levelId);
                    //put level to Level list
                    levels.Add(levelRoom);

                    //create UI checkbox
                    CheckBox levelChecker = new CheckBox();
                    levelChecker.Margin = new Thickness(0, 0, 0, 10);
                    levelChecker.Content = levelRoom.Name;
                    levelChecker.Tag = levelRoom.Id;

                    //put checkbox to view
                    AllLevelsView.Children.Add(levelChecker);
                }
            }
            return levels;
        }
        private void CheckAllLevelInViewEvent(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            CheckAllLevelInView();
        }
        private void UncheckAllLevelInViewEvent(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            UncheckAllLevelInView();
        }
        private void CheckAllLevelInView()
        {
            UIElementCollection allCheckBox = AllLevelsView.Children;
            foreach (CheckBox checker in allCheckBox)
            {
                if (checker.IsChecked != true) checker.IsChecked = true;
            }
        }
        private void UncheckAllLevelInView()
        {
            UIElementCollection allCheckBox = AllLevelsView.Children;
            foreach (CheckBox checker in allCheckBox)
            {
                if (checker.IsChecked == true) checker.IsChecked = false;
            }
        }
        private bool IsLevelChecked(ElementId levelId)
        {
            List<ElementId> selectedLevelIds = new List<ElementId>();
            foreach (CheckBox levelBox in AllLevelsView.Children)
            {
                if (levelBox.IsChecked == true) selectedLevelIds.Add(levelBox.Tag as ElementId);
            }
            if (selectedLevelIds.Contains(levelId)) return true;
            else return false;
        }
        private bool IsJustOneLevelChecked()
        {
            int count = 0;
            foreach (CheckBox levelBox in AllLevelsView.Children)
            {
                if (levelBox.IsChecked == true) count++;
            }
            if (count == 0) return false;
            else return true;
            
        }
        private List<Room> GetSelectedRoomInViewList()
        {
            List<Room> selectedRoom = new List<Room>();
            foreach(Room room in RoomListView.SelectedItems)
            {
                selectedRoom.Add(room);
            }
            return selectedRoom;

        }
        private WallType GetNeedWallTypeFromSelected(string categoryName)
        {
            WallType resultType = null;
            //get all checkboxes in AllCategoryWallView
            var allPanels = AllCategoryWallView.Children;

            foreach(System.Windows.Controls.Panel p in allPanels)
            {
                if ((p.Children[0] as Label).Content.ToString() == categoryName)
                {
                    resultType = (p.Children[1] as ComboBox).SelectedItem as WallType;
                    break;
                }
            }
            return resultType;
        }
        private void ChangeWallType(Wall wall, WallType wallType)
        {
            if (wallType != null) wall.WallType = wallType;
        }
        
        private delegate void ProgressBarDelegate();
        private System.Windows.Media.Color GetColorFromHTML(string htmlColor)
        {
            System.Windows.Media.Color btnColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString(htmlColor);
            return btnColor;
        }
        //change color in category navigation
        private void ChangeCategoryBtnColor(Button btn)
        {
            var allBtns = AllNames.Children;
            btn.Background = new SolidColorBrush(GetColorFromHTML(categoryBtnColorClick));
            foreach (Button otherbtn in allBtns)
            {
                if (otherbtn != btn) otherbtn.Background = new SolidColorBrush(GetColorFromHTML(categoryBtnColorStandart));
            }
        }
        private void UpdateProgress()
        {
            progressBar.Value += 1;
        }
        private void IsRoomSelectedInViewList(object sender, RoutedEventArgs e)
        {
            if (GetSelectedRoomInViewList().Count != 0)
            {
                AllLevelsView.IsEnabled = false;
                AllLevelsView.Visibility = System.Windows.Visibility.Hidden;
                CheckAllLevelInView();
            }
            else
            {
                AllLevelsView.Visibility = System.Windows.Visibility.Visible;
                AllLevelsView.IsEnabled = true;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://infobim.ru/");
        }
        private void HyperlinkToPay_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://money.yandex.ru/to/410013882137616");
        }

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpPage helpPage = new HelpPage(projectLanguage);
            helpPage.Show();
        }
    }
}
