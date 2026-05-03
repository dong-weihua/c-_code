//球
//用户在cad界面指定一点生成一个二维球面
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Reflection.Metadata;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;

namespace ball
{
    public class Class1
    {
        [CommandMethod("ball")]
        public void Ball()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            PromptPointOptions pPrompt = new PromptPointOptions("\n请输入圆心坐标：");
            PromptPointResult point = doc.Editor.GetPoint(pPrompt);
            if (point.Status == PromptStatus.Cancel) return;

            //获取圆半径
            PromptDistanceOptions dPrompt = new PromptDistanceOptions("\n请输入圆的半径：");
            PromptDoubleResult distance = doc.Editor.GetDistance(dPrompt);
            if (distance.Status == PromptStatus.Cancel) return;

            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //画出主圆
                using (Circle cMain = new Circle())
                {
                    cMain.Center = point.Value;
                    cMain.Radius = distance.Value;
                    btr.AppendEntity(cMain);
                    trans.AddNewlyCreatedDBObject(cMain, true);

                    //建立曲线集合
                    ObjectIdCollection enCollect = new ObjectIdCollection();

                    //建立旋转轴
                    Matrix3d curUCSMatrix = doc.Editor.CurrentUserCoordinateSystem;
                    CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;
                    Vector3d axis = curUCS.Zaxis;
                    Point3d basePoint = cMain.Center;
                    for (int i = 0; i < 3; i++)
                    {
                        if (i == 0)
                        {
                            double radius = distance.Value / 2;
                            Vector3d offsetVec1 = new Vector3d(radius, 0, 0);
                            Point3d center1 = point.Value + offsetVec1;

                            Point3d center2 = point.Value - offsetVec1;
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center1;
                                arcl.Radius = radius;
                                arcl.StartAngle = Math.PI;
                                arcl.EndAngle = Math.PI * 2;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                            }
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center2;
                                arcl.Radius = radius;
                                arcl.StartAngle = 0;
                                arcl.EndAngle = Math.PI;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                            }
                        }
                        if (i == 1)
                        {
                            double radius = distance.Value / 3;
                            Vector3d offsetVec1 = new Vector3d(radius, 0, 0);
                            Point3d center1 = point.Value + offsetVec1;
                            Vector3d offsetVec2 = new Vector3d(radius * 2, 0, 0);
                            Point3d center2 = point.Value - offsetVec2;
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center1;
                                arcl.Radius = (radius + distance.Value) / 2;
                                arcl.StartAngle = Math.PI;
                                arcl.EndAngle = Math.PI * 2;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                                enCollect.Add(arcl.Id);
                            }
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center2;
                                arcl.Radius = radius;
                                arcl.StartAngle = 0;
                                arcl.EndAngle = Math.PI;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                                enCollect.Add(arcl.Id);
                            }
                        }
                        if (i == 2)
                        {
                            double radius = distance.Value / 6;
                            Vector3d offsetVec1 = new Vector3d(radius, 0, 0);
                            Point3d center1 = point.Value + offsetVec1;
                            Vector3d offsetVec2 = new Vector3d(radius * 5, 0, 0);
                            Point3d center2 = point.Value - offsetVec2;
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center1;
                                arcl.Radius = (radius * 4 + distance.Value) / 2;
                                arcl.StartAngle = Math.PI;
                                arcl.EndAngle = Math.PI * 2;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                                enCollect.Add(arcl.Id);
                            }
                            using (Arc arcl = new Arc())
                            {
                                arcl.Center = center2;
                                arcl.Radius = radius;
                                arcl.StartAngle = 0;
                                arcl.EndAngle = Math.PI;
                                btr.AppendEntity(arcl);
                                trans.AddNewlyCreatedDBObject(arcl, true);
                                enCollect.Add(arcl.Id);
                            }

                        }
                    }
                    for (int i = 0; i < enCollect.Count; i++)
                    {
                        using (Arc arcl = trans.GetObject(enCollect[i], OpenMode.ForWrite) as Arc)
                        {
                            using (Arc arcr = arcl.Clone() as Arc)
                            {
                                arcr.TransformBy(Matrix3d.Rotation(Math.PI, axis, basePoint));
                                btr.AppendEntity(arcr);
                                trans.AddNewlyCreatedDBObject(arcr, true);
                            }
                        }
                    }
                }
                trans.Commit();
            }
        }
    }
}
