using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace WallFinishByCategory.Model
{
    static class RoomWorker
    {
        /// <summary>
        /// Method to get all finish boundary from one room as Curve list
        /// </summary>
        /// <returns></returns>
        public static List<Curve> GetRoomCurve(Room room)
        {
            List<Curve> resultCurve = new List<Curve>();
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            foreach (BoundarySegment segment in room.GetBoundarySegments(options)[0])
            {
                Curve curve = segment.GetCurve();
                resultCurve.Add(curve);
            }
            return resultCurve;
        }
        //method to get all wall around the room
        public static List<Wall> GetRoomWalls(Document doc, Room room)
        {
            List<Wall> walls = new List<Wall>();

            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            foreach (BoundarySegment segment in room.GetBoundarySegments(options)[0])
            {
                Wall wall = doc.GetElement(segment.ElementId) as Wall;
                if (wall != null) walls.Add(wall);
            }
            return walls;
        }
        public static XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
        }
        public static XYZ GetRoomCenter(Room room)
        {
            XYZ boundingCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ center = new XYZ(boundingCenter.X, boundingCenter.Y, locPt.Point.Z);
            return center;
        }
        public static Wall FindClosedRoomWall(Document doc, Wall newWall, List<Wall> roomWalls)
        {
            Wall resutWall = null;
            //create the normal of newWall curve and find the intersect with roomWall curve
            //get newWall curve 
            Curve newWallCurve = ((LocationCurve)newWall.Location).Curve;
            //create the line
            Line newLine = CreateNewLineViaSides(newWallCurve, -(newWallCurve.Length/2- 1.5));
            //create the model line
            Level refLevel = doc.GetElement(newWall.LevelId) as Level;
            SketchPlane skplane = SketchPlane.Create(doc, refLevel.GetPlaneReference());
            ModelCurve modelCurve = doc.Create.NewModelCurve(newLine as Curve, skplane);
            //get axis for rotate
            Line axis = GetVerticalLine(newWallCurve.Evaluate(0.5, true));
            //rotate the model line
            modelCurve.Location.Rotate(axis, Math.PI / 2);
            foreach(Wall roomWall in roomWalls)
            {
                Curve roomWallCurve = ((LocationCurve)roomWall.Location).Curve;
                if (modelCurve.GeometryCurve.Intersect(roomWallCurve) != SetComparisonResult.Disjoint) resutWall = roomWall;
            }
            doc.Delete(modelCurve.Id);
            return resutWall;
        }
        public static XYZ GetVectorForNewWall(Room room, Curve axis)
        {
            //get center of axis and room for vector
            XYZ roomCenter = GetRoomCenter(room);
            XYZ vector = null;
            XYZ resultVectorForWall = null;

            //if type of curve is line, create the plane ad get it's normal
            if (axis.GetType().Name == "Line")
            {
                XYZ normal = null;
                //get the plane from 3 points
                XYZ aboveAxisPoint = new XYZ(axis.GetEndPoint(0).X, axis.GetEndPoint(0).Y, axis.GetEndPoint(0).Z + 10);
                Autodesk.Revit.DB.Plane axisPlane = Autodesk.Revit.DB.Plane.CreateByThreePoints(axis.GetEndPoint(0), axis.GetEndPoint(1), aboveAxisPoint);
                normal = axisPlane.Normal;
                vector = normal * (-1.0);

                resultVectorForWall = vector;
            }
            else
            {
                Autodesk.Revit.DB.Arc arcCurve = axis as Autodesk.Revit.DB.Arc;
                resultVectorForWall = arcCurve.XDirection + arcCurve.YDirection;
            }
            
            return resultVectorForWall;
        }
        private static ModelCurve CreateModelCurve(Document doc, Room room, Curve curve)
        {
            //get vector
            XYZ resultVectorForWall = RoomWorker.GetVectorForNewWall(room, curve);
            //Create ModelCurve               
            SketchPlane skplane = SketchPlane.Create(doc, room.Level.GetPlaneReference());
            ModelCurve modelCurve = doc.Create.NewModelCurve(curve, skplane);
            return modelCurve;
        }
        private static ModelCurve CreateModelCurveAndMove(Document doc, Room room, Curve curve, double distanse)
        {
            //get vector
            XYZ resultVectorForWall = RoomWorker.GetVectorForNewWall(room, curve);
            ModelCurve modelCurve = CreateModelCurve(doc, room, curve);
            if (curve.GetType().Name == "Line")
            {                
                modelCurve.Location.Move(resultVectorForWall * distanse);
            }
            return modelCurve;
        }
        private static Curve GetNewWallAxis(Document doc, Room room, Curve curve, WallType wallType)
        {
            //get vector
            XYZ resultVectorForWall = RoomWorker.GetVectorForNewWall(room, curve);
            double defaultWallTypeWidth = wallType.Width;
            Curve axisNewWall = null;
            
            //create offset curve
            if (curve.GetType().Name == "Line")
            {
                //Create ModelCurve
                //********************************************************                
                ModelCurve modelCurve = CreateModelCurveAndMove(doc, room, curve, defaultWallTypeWidth / 2);
                //change the length of this curve
                // create a new LineBound
                Line newWallLine = CreateNewLineViaSides(modelCurve.GeometryCurve, -defaultWallTypeWidth);

                axisNewWall = newWallLine as Curve;
                //delete the modelcurve
                doc.Delete(modelCurve.Id);
                //********************************************************
            }
            ////problem to move the walls with arc location curve. it should be fixed, but not now
            else axisNewWall = curve;

            return axisNewWall;
        }
        public static List<Curve> GetNewWallAxisAll(Document doc, Room room, List<Curve> allCurves, WallType wallType)
        {
            List<Curve> resultCurves = new List<Curve>();
            foreach (Curve curve in allCurves)
            {
                Curve c = GetNewWallAxis(doc, room, curve, wallType);
                resultCurves.Add(c);
            }

            for (int i=0; i<resultCurves.Count; i++)
            {
                Curve firstCurve;
                Curve secondCurve;
                int firstIndexCurve = i;
                int secondIndexCurve = i+1;
                if (firstIndexCurve == resultCurves.Count - 1)
                {
                    firstIndexCurve = i;
                    secondIndexCurve = 0;
                }
                firstCurve = resultCurves[firstIndexCurve];
                secondCurve = resultCurves[secondIndexCurve];

                if (IsIntersected(firstCurve, secondCurve) == false)
                {
                    XYZ firstEndPoint;
                    XYZ secondStartPoint;
                    //get end point of the first line
                    firstEndPoint = firstCurve.GetEndPoint(1);
                    //get start point of the second line
                    secondStartPoint = secondCurve.GetEndPoint(0);
                    //get normal from first to second line
                    double firstNormal = secondCurve.Distance(firstEndPoint);
                    //get normal from second to first line
                    double secondNormal = firstCurve.Distance(secondStartPoint);
                    //get angle between the lines
                    //get first vector 
                    XYZ firstVector = firstCurve.GetEndPoint(1) - firstCurve.GetEndPoint(0);
                    //get second vector
                    XYZ secondVector = secondCurve.GetEndPoint(1) - secondCurve.GetEndPoint(0);
                    //get angle between the lines
                    double alfa = firstVector.AngleTo(secondVector);
                    double secondExtend;
                    double firstExtend;
                    if (Math.Round(alfa) == 0 || Math.Round(alfa) == Math.PI)
                    {
                        secondExtend = firstExtend = firstEndPoint.DistanceTo(secondStartPoint);
                    }
                    else
                    {
                        //get extend distance for first line 
                        firstExtend = firstNormal * Math.Cos(alfa - Math.PI / 2);
                        //get extend distance for second line
                        secondExtend = secondNormal * Math.Cos(alfa - Math.PI / 2);
                    }
                    
                    
                    
                    Curve newFirst = TrimExtendLineOneDirect(firstCurve, firstExtend/2, 1) as Curve;
                    Curve newSecond= TrimExtendLineOneDirect(secondCurve, secondExtend/2, 0) as Curve;

                    resultCurves.Remove(firstCurve);
                    resultCurves.Insert(firstIndexCurve, newFirst);

                    resultCurves.Remove(secondCurve);
                    resultCurves.Insert(secondIndexCurve, newSecond);
                }
            }

            return resultCurves;
        }
        private static Line GetVerticalLine(XYZ center)
        {
            XYZ pt2 = new XYZ(center.X, center.Y, center.Z + 100);
            Line newLine = Line.CreateBound(center, pt2);
            return newLine;
        }
        private static Line CreateNewLineViaSides(Curve curve, double reduceLength)
        {
            //change the length of this curve
            Line newLine = TrimExtendLineOneDirect(curve, reduceLength, 0);
            newLine = TrimExtendLineOneDirect(newLine, reduceLength, 1);
            return newLine;
        }
        /// <summary>
        /// Method to trim or extend the Line. If trim, trimExtendLength with "-" sing
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="trimExtendLength">if trim then trimExtendLength with "-" sing</param>
        /// <param name="endPointNum"> 0 or 1</param>
        /// <returns></returns>
        private static Line TrimExtendLineOneDirect(Curve curve, double trimExtendLength, int endPointNum)
        {
            XYZ pt1 = curve.GetEndPoint(0);
            XYZ pt2 = curve.GetEndPoint(1);
            Line newLine = null;
            // create a new LineBound
            if (endPointNum == 0)
            {                
                XYZ newpt = pt1 + (pt1 - pt2) * (trimExtendLength / curve.Length);
                newLine = Line.CreateBound(newpt, pt2);
            }
            else
            {
                XYZ newpt = pt2 + (pt2 - pt1) * (trimExtendLength / curve.Length);
                newLine = Line.CreateBound(pt1, newpt);
            }
            return newLine;
        }
        public static Wall CreateNewWallByCurve(Document doc, Room room, Curve curve, 
            WallType wallType, string ROOM_NUMBER_PARAM, string ROOM_NAME_PARAM, ElementId selectedWorksetElementId)
        {
            string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            string roomNumber = room.Number.ToString();

            Wall newWall = Wall.Create(doc, curve,
                                    wallType.Id,
                                    room.LevelId,
                                    room.UnboundedHeight,
                                    0, false, false);
            newWall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).Set(0);
            newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set(3);
            if(selectedWorksetElementId != null) newWall.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(selectedWorksetElementId);
            try
            {
                newWall.LookupParameter(ROOM_NUMBER_PARAM).Set(roomNumber);
                newWall.LookupParameter(ROOM_NAME_PARAM).Set(roomName);
            }
            catch (NullReferenceException)
            {
                newWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(roomName + "; " + roomNumber);
            }

            return newWall;

        }        
        private static XYZ GetIntersection(Curve line1, Curve line2)
        {
            IntersectionResultArray results;
            SetComparisonResult result = line1.Intersect(line2, out results);
            if ((result != SetComparisonResult.Overlap) || (results == null || results.Size != 1))
            {
                return null;
            }
            IntersectionResult iResult = results.get_Item(0);
            return iResult.XYZPoint;
        }
        private static Boolean IsIntersected(Curve line1, Curve line2)
        {
            IntersectionResultArray results;
            SetComparisonResult result = line1.Intersect(line2, out results);
            if (result != SetComparisonResult.Overlap)
            {
                return false;
                //throw new InvalidOperationException("Input lines did not intersect");
            }
            if (results == null || results.Size != 1)
            {
                return false;
                //throw new InvalidOperationException("Could not extract line intesection point");
            }
            IntersectionResult iResult = results.get_Item(0);
            //return iResult.XYZPoint;
            return true;
        }
        ////method to create the new curve for new wall with Autodesk.DesignScript.Geometry
        //public static List<Autodesk.Revit.DB.Curve> GetNewWallCurveWithProto(List<Autodesk.Revit.DB.Curve> roomCurvesOld, double offset)
        //{
        //    List<Autodesk.Revit.DB.Curve> resultCurve = new List<Autodesk.Revit.DB.Curve>();

        //    //convert roomCurves to Autodesk.DesignScript.Geometry
        //    List<Autodesk.DesignScript.Geometry.Curve> curvesFromDBRoomCurves = new List<Autodesk.DesignScript.Geometry.Curve>();
        //    foreach (Autodesk.Revit.DB.Curve dbCurve in roomCurvesOld)
        //    {
        //        //covert curve
        //        Autodesk.DesignScript.Geometry.Curve geometryCurve = RevitToProtoCurve.ToProtoType(dbCurve, false);
        //        curvesFromDBRoomCurves.Add(geometryCurve);
        //    }
        //    // create Autodesk.DesignScript.Geometry Curve
        //    Autodesk.DesignScript.Geometry.Curve polycurve = PolyCurve.ByJoinedCurves(curvesFromDBRoomCurves, 0.001);

        //    //create offset curve Autodesk.DesignScript.Geometry
        //    Autodesk.DesignScript.Geometry.Curve offsetPolycurve = polycurve.Offset(offset * (-0.5));

        //    //Create the list of offsetPolycurve that divide method works with list
        //    List<Autodesk.DesignScript.Geometry.Curve> offsetCurveList = new List<Autodesk.DesignScript.Geometry.Curve>();
        //    offsetCurveList.Add(offsetPolycurve);

        //    //get the axis for new walls
        //    PolyCurve p_curve = PolyCurve.ByJoinedCurves(offsetCurveList, 0.001);
        //    Autodesk.DesignScript.Geometry.Curve[] axis = p_curve.Curves();

        //    foreach (Autodesk.DesignScript.Geometry.Curve c in axis)
        //    {
        //        Autodesk.Revit.DB.Curve resultC = ProtoToRevitCurve.ToRevitType(c, false);
        //        resultCurve.Add(resultC);

        //    }
        //    return resultCurve;
        //}
    }
}
