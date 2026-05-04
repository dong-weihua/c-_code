//花环
//在cad上指定一点，与半径生成花环
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;

namespace huan
{
    public class Class1
    {
        [CommandMethod("huan")]
        public void Huan()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            PromptPointOptions pPrompt = new PromptPointOptions("\n请输入圆心坐标：");
            PromptPointResult point = doc.Editor.GetPoint(pPrompt);
            if (point.Status == PromptStatus.Cancel) return;
            PromptDistanceOptions dPrompt = new PromptDistanceOptions("\n请输入半径：");
            PromptDoubleResult distance = doc.Editor.GetDistance(dPrompt);
            if (distance.Status == PromptStatus.Cancel) return;

            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //设立旋转轴
                Matrix3d curUCSMatrix = doc.Editor.CurrentUserCoordinateSystem;
                CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;
                Vector3d axis = curUCS.Zaxis;

                //设立面域集合
                DBObjectCollection regins = new DBObjectCollection();

                //设立实体集合
                DBObjectCollection entitys = new DBObjectCollection();

                //画出中心圆
                using (Circle cCenter = new Circle())
                {
                    cCenter.Center = point.Value;
                    cCenter.Radius = distance.Value;
                    btr.AppendEntity(cCenter);
                    trans.AddNewlyCreatedDBObject(cCenter, true);
                }

                //画外圆
                using(Circle cf = new Circle())
                {
                    Vector3d offsetP = new Vector3d(0, distance.Value, 0);
                    Point3d center = point.Value + offsetP;
                    cf.Center = center;
                    cf.Radius = distance.Value;

                    //将其复制一份旋转2pi/8
                    double radian = Math.PI / 4;
                    using(Circle cf2 = cf.Clone() as Circle)
                    {
                        //作辅助圆
                        cf2.TransformBy(Matrix3d.Rotation(radian, axis, point.Value));

                        entitys.Add(cf2);
                        entitys.Add(cf);
                        regins = Region.CreateFromCurves(entitys);
                    }
                }

                //阵列
                using(Region region1 = regins[0] as Region)
                {
                    region1.BooleanOperation(BooleanOperationType.BoolSubtract, regins[1] as Region);

                    Point3d center = point.Value;
                    int itemCount = 8;
                    Double fillAngle = 360.0;
                    bool rotateItems = true;
                    double partAngle = Math.PI * 2 /8;
                    for (int i = 0; i < itemCount; i++)
                    {
                        // 创建面域的副本
                        Region newRegion = region1.Clone() as Region;

                        // 计算当前项目的旋转角度（弧度）
                        double rotationAngle = partAngle * i;

                        newRegion.TransformBy(Matrix3d.Rotation(rotationAngle, axis, center));

                        // 将副本添加到数据库
                        btr.AppendEntity(newRegion);
                        trans.AddNewlyCreatedDBObject(newRegion, true);
                    }

                }
                trans.Commit();
            }
            
        }
    }
}
