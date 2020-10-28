# Revit_WallFinishByCategory
Create new wall finish by different values wall model group parameter
the plagin works with room boundary segments
1) Plagin get the all rooms, document from the App.cs
2) Create dictionary for rooms by categories for user selection and create the buttons with these categories after Initialize()
3) After Category button click the plagin get all rooms from dctionary by the name of button<br>
  3.1) Get all levels by rooms and model group of walls around the rooms<br>
  3.2) Put the model groups of walls as checkbox to view <br>
4) Then plagin start work after startwork button click<br>
  4.1) If category or levels checkbox or default wall type in default checkbox is not selected, plugin will not work<br>
  4.2) Then programm go through rooms by selected category and gets all rooms only in selected levels<br>
  4.3) Check one wall instance from first room in selected by category for parameter existing (ROOM_NUMBER_PARAM, ROOM_NAME_PARAM)<br>
  4.4) Go through all rooms by selected category and levels. Start loop<br>
    4.4.1) Get new axis for new wall<br>
    4.4.2) The main problem - get new axis for the new walls due the ProtoGeometry.dll.<br>
    This dll has method to create the PolyCyrve on the Autodesk.DesignScript.Geometry.Curve. 
    When we get the boundary segments in room, we get all curves like Autodesk.Revit.DB.Curve.
    It should be converted to Autodesk.DesignScript.Geometry.Curve with RevitToProtoCurve.ToProtoType method from ProtoGeometry.dll.
    A few moments later this dll doesn't work. Because I use the axis from room, create model curve, move after, get curve and delete after. 
    And then we have problem with moving model curves with Ark location curve. 
    When base line's name is Line, plagin create the Plane on room curve and get the vector by plane normal.
    In RoomWoker.cs there is method to get new curves with ProtoGeometry.dll (GetNewWallCurveWithProto).<br>
    4.4.3) Create the walls and set room parameters (name and number) to instance<br>
    4.4.4) Join new walls with room walls.<br>
