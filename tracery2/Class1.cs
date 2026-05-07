//用户输入圆心坐标，半径，得到六角窗花
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;

namespace tracery2
{
    public class Class1
    {
        [CommandMethod("tracery2")]
        public void Tracery()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            PromptPointOptions pPrompt = new PromptPointOptions("\n请输入圆心坐标：");
            PromptPointResult point = doc.Editor.GetPoint(pPrompt);
            if (point.Status == PromptStatus.Cancel) return;

            PromptDistanceOptions dPrompt = new PromptDistanceOptions("\n请输入半径：");
            PromptDoubleResult radius = doc.Editor.GetDistance(dPrompt);
            if (radius.Status == PromptStatus.Cancel) return;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Point2dCollection pointPol = new Point2dCollection();
                ObjectIdCollection lines = new ObjectIdCollection();
                ObjectIdCollection rotute = new ObjectIdCollection();
                Point3dCollection points = new Point3dCollection();

                Matrix3d curUCSMatrix = doc.Editor.CurrentUserCoordinateSystem;
                CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;
                Vector3d axis = curUCS.Zaxis;
                //画主圆
                using (Circle cMain = new Circle())
                {
                    cMain.Center = point.Value;
                    cMain.Radius = radius.Value;
                    btr.AppendEntity(cMain);
                    trans.AddNewlyCreatedDBObject(cMain, true);

                    for(int i = 0; i<6;  i++)
                    {
                        double currentAngle = Math.PI / 2 + Math.PI / 3 * i;
                        double currentRadius = radius.Value;
                        double x = point.Value.X +  currentRadius * Math.Cos(currentAngle);
                        double y = point.Value.Y + currentRadius * Math.Sin(currentAngle);
                        Point2d p = new Point2d(x, y);
                        pointPol.Add(p);
                    }

                }
                //画三角形
                using (Polyline poly = new Polyline())
                {
                    for(int i = 0;i<3;i++)
                    {
                        poly.AddVertexAt(i, pointPol[i*2], 0,0,0);
                    }
                    poly.Closed = true;
                    btr.AppendEntity(poly);
                    trans.AddNewlyCreatedDBObject(poly, true);
                    
                }

                //画圆弧
                using(Arc arcMain = new Arc())
                {
                    arcMain.Center = new Point3d(pointPol[3].X, pointPol[3].Y, 0);
                    arcMain.Radius = radius.Value;
                    arcMain.StartAngle = Math.PI / 6 ;
                    arcMain.EndAngle = Math.PI/6*5;
                    
                   
                    for(int i = 0; i<3;i++)
                    {
                        using (Arc arc = arcMain.Clone() as Arc)
                        {
                            arc.TransformBy(Matrix3d.Rotation(Math.PI / 3*2 * i, axis, point.Value));
                            btr.AppendEntity(arc);
                            trans.AddNewlyCreatedDBObject(arc, true);
                        }
                    }
                }

                //画三角形
                using (Polyline poly = new Polyline())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        poly.AddVertexAt(i, pointPol[i * 2+1], 0, 0, 0);
                    }
                    poly.Closed = true;
                    
                    lines.Add(poly.Id);
                }

                //画正三角形底边直线
                using(Line line = new Line())
                {
                    Point3d p1 =  new Point3d(pointPol[2].X, pointPol[2].Y, 0);
                    Point3d p2 = new Point3d(pointPol[4].X, pointPol[4].Y, 0);
                    line.StartPoint = p1;
                    line.EndPoint = p2;
                    Line line2 = new Line(new Point3d(pointPol[1].X, pointPol[1].Y, 0), new Point3d(pointPol[3].X, pointPol[3].Y, 0));
                    line.IntersectWith(line2, Intersect.OnBothOperands, points, 0, 0);
                    Line line3 = new Line(new Point3d(pointPol[5].X, pointPol[5].Y, 0), new Point3d(pointPol[3].X, pointPol[3].Y, 0));
                    line.IntersectWith(line3, Intersect.OnBothOperands, points, 0, 0);
                }

                //获取旋转对象
                for(int i = 0;i<2;i++)
                {
                    Point3d p = new Point3d(pointPol[3].X, pointPol[3].Y, 0);
                    using (Line line = new Line(points[i],p))
                    {
                        if(i== 0)
                        {
                            using (Arc arcMain = new Arc())
                            {
                                arcMain.Center = new Point3d(pointPol[4].X, pointPol[4].Y, 0);
                                arcMain.Radius = radius.Value;
                                arcMain.StartAngle = Math.PI;
                                arcMain.EndAngle = - Math.PI / 6 * 5;
                                btr.AppendEntity(arcMain);
                                trans.AddNewlyCreatedDBObject(arcMain, true);
                                rotute.Add(arcMain.Id);
                            }
                        }
                        if (i == 1)
                        {
                            using (Arc arcMain = new Arc())
                            {
                                arcMain.Center = new Point3d(pointPol[2].X, pointPol[2].Y, 0);
                                arcMain.Radius = radius.Value;
                                arcMain.StartAngle = -Math.PI / 6 ;
                                arcMain.EndAngle = 0;
                                btr.AppendEntity(arcMain);
                                trans.AddNewlyCreatedDBObject(arcMain, true);
                                rotute.Add(arcMain.Id);
                            }
                        }
                        btr.AppendEntity(line);
                        trans.AddNewlyCreatedDBObject(line, true);
                        rotute.Add(line.Id);
                    }

                }

                //旋转
                for (int i = 1; i < 3; i++)
                {
                   for(int j = 0; j < rotute.Count; j++) 
                    {

                        Entity ro = trans.GetObject(rotute[j], OpenMode.ForWrite) as Entity;
                        using (Entity roMain = ro.Clone() as Entity)
                        {
                            roMain.TransformBy(Matrix3d.Rotation(Math.PI / 3 * 2 * i, axis, point.Value));
                            btr.AppendEntity(roMain);
                            trans.AddNewlyCreatedDBObject(roMain, true);
                        }
                    }
                }
                trans.Commit();
            }
        }
    }
}
