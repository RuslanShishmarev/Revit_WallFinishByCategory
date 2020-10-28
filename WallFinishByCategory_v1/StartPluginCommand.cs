using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using WallFinishByCategory_v1.View;

namespace WallFinishByCategory_v1
{  
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class StartPluginCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get current document
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //is workshared document
            bool worksharedStatus = doc.IsWorkshared;
            //Get all rooms in project
            FilteredElementCollector filter = new FilteredElementCollector(doc);
            //filter function
            Func<Element, bool> funcFilter = IsRoomExist;
            ICollection<Element> allRooms = filter.OfCategory(BuiltInCategory.OST_Rooms).ToElements();
            

            //Get all walltypes in project
            List <WallType> wallType = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().ToList();
            //send these rooms to viewmodel class
            //open user window
            UserWindow userWindow = new UserWindow(doc, worksharedStatus, allRooms, wallType);
            userWindow.ShowDialog();

            return Result.Succeeded;
        }
        private bool IsRoomExist(Element elementRoom)
        {
            if ((elementRoom as Room).Area != 0) return true;
            else return false;
        }
    }
}
