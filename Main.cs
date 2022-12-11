using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWRCreation
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            Level level2 = listLevel
               .Where(x => x.Name.Equals("Уровень 2"))
               .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);

            CreateWalls(doc, width, depth, level1, level2);

            return Result.Succeeded;
        }
        //private static void CreateWalls(Document doc, Level level1, Level level2)
        public void CreateWalls(Document doc, double width, double depth, Level lvlstr, Level lvlfn)
        {
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {

                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, lvlstr.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(lvlfn.Id);

            }
            CreateDoor(doc, lvlstr, walls[0]);
            CreateWindow(doc, lvlstr, walls[1]);
            CreateWindow(doc, lvlstr, walls[2]);
            CreateWindow(doc, lvlstr, walls[3]);
            AddRoof(doc, lvlfn, points);
            transaction.Commit();
        }

        private static void CreateDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        private static void CreateWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol winType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!winType.IsActive)
                winType.Activate();

            var window = doc.Create.NewFamilyInstance(point, winType, wall, level1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }

        private static void AddRoof(Document doc, Level level2, List<XYZ> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();


            /* double wallWidth = walls[0].Width;
             double dt = wallWidth / 2;
             List<XYZ> points = new List<XYZ>();
             points.Add(new XYZ(-dt, -dt, 0));
             points.Add(new XYZ(dt, -dt, 0));
             points.Add(new XYZ(dt, dt, 0));
             points.Add(new XYZ(-dt, dt, 0));
             points.Add(new XYZ(-dt, -dt, 0));*/


            //public static void Main()
            // {
            //    List<int> list = new List<int> { 1, 2, 3, 4, 5 };

            // int[] array = List.ToArr

            // list<XYZ> .ToArray();

            //Console.WriteLine(String.Join(", ", array));        // 1, 2, 3, 4, 5

            //CurveArray curveArray = walls.ToArray[];
            //List<T>
            // CurveArray curveArray = points.ToArray(points);

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-20, -10, 13.5), new XYZ(-20, 0, 20)));
            curveArray.Append(Line.CreateBound(new XYZ(-20, 0, 20), new XYZ(-20, 10, 13.5)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, -20, 20);
        }
    }
}
