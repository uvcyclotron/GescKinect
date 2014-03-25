using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Web.UI.DataVisualization.Charting;
namespace Gestures
{
    public class GestureData
    {
        public Point3D ShoulderL { get; set; }
        public Point3D ShoulderR { get; set; }
        public Point3D ElbowL { get; set; }
        public Point3D ElbowR { get; set; }
        public Point3D WristL { get; set; }
        public Point3D WristR { get; set; }
        public Point3D HandL { get; set; }
        public Point3D HandR { get; set; }
        public Point3D Spine { get; set; }
        public Point3D Head { get; set; }

        public GestureData()
        {

        }

       // Point3D shl, shr, elbl, elbr, wrstl, wrstr, handl, handr, spine, head;

        public GestureData(Point3D ShoulderL, Point3D ShoulderR, Point3D ElbowL, Point3D ElbowR, Point3D WristL, Point3D WristR, Point3D HandL, Point3D HandR, Point3D Spine, Point3D Head)
        {
            this.ShoulderL = ShoulderL;
            this.ShoulderR = ShoulderR;
            this.ElbowL = ElbowL;
            this.ElbowR = ElbowR;
            this.WristL = WristL;
            this.WristR = WristR;
            this.HandL = HandL;
            this.HandR = HandR;
            this.Spine = Spine;
            this.Head = Head;

         }

    }
}
