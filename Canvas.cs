// Accord.NET Sample Applications
// http://accord.googlecode.com
//
// Copyright © César Souza, 2009-2013
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Gestures
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using System.Drawing.Drawing2D;

    using System.Web.UI.DataVisualization.Charting;
    //using System.Windows.Forms
    
    /*NOTE: This class takes the input.*/

    public partial class Canvas : UserControl
    {
        private bool capturing;
       private List<GestureData> sequence; ////<Point>
        public bool drawnFlag = false;
        

        public Canvas()
        {
            InitializeComponent();

            //sequence = new List<GestureData>(); //<Point>
           // this.sequence = MainForm.GetSeq();
            
            this.DoubleBuffered = true;
        }

        //public GestureData[] Get3DSequence() //point[]
        //{
        //    return sequence.ToArray();
        //}

        public void setSequence()
        {
            sequence = MainForm.GetSeq();
            System.Diagnostics.Debug.WriteLine("------------------ Sequence obtained------------------------");

        }

        public void Clear()
        {
            //sequence.Clear();
          //  System.Diagnostics.Debug.WriteLine(" --------------clear-----------------");
            this.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
          
            base.OnPaint(e);

            if (!this.DesignMode && null !=sequence)
            {
                if (sequence.Count > 1)
                {
                    //  System.Diagnostics.Debug.WriteLine(" --------------draw begins-----------------");
                    for (int i = 1; i < sequence.Count; i++)
                    {
                        int x = (int)sequence[i].HandR.X;
                        int y = (int)sequence[i].HandR.Y;
                        int p = (int)Accord.Math.Tools.Scale(0, sequence.Count, 0, 255, i);

                        int prevX = (int)sequence[i - 1].HandR.X;
                        int prevY = (int)sequence[i - 1].HandR.Y;
                        int prevP = (int)Accord.Math.Tools.Scale(0, sequence.Count, 0, 255, i - 1);

                        if (x == prevX && y == prevY)
                            continue;

                        Point start = new Point(prevX, prevY);
                        Point end = new Point(x, y);
                        //Point3D a = new Point3D(x, y, p); 
                        Color colorStart = Color.FromArgb(255 - p, 0, p);
                        Color colorEnd = Color.FromArgb(255 - prevP, 0, prevP);

                        using (Brush brush = new LinearGradientBrush(start, end, colorStart, colorEnd))
                        using (Pen pen = new Pen(brush, 10))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;

                            e.Graphics.DrawLine(pen, prevX, prevY, x, y);

                            //change
                            System.Diagnostics.Debug.WriteLine(" -----!!!!!-draw completed-!!!!!!--------");
                            drawnFlag = true; //added
                        }
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Clear();

            capturing = true;

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            capturing = false;

            base.OnMouseUp(e);
        }

        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    if (capturing)
        //    {
        //        if (e.X > 0 && e.Y > 0)
        //        {
        //            sequence.Add(new Point(e.X, e.Y));
        //            this.Refresh();
        //        }
        //    }

        //    base.OnMouseMove(e);
        //}

        //protected void TraceTrackedSkeletonsPoints()
        //{

        //}

    }
}
