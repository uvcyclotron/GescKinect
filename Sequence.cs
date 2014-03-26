using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Xml.Serialization;
using Accord.Math;
using System.Drawing.Drawing2D;

namespace Gestures
{
    [Serializable]
    public class Sequence : ICloneable
    {
        [XmlIgnore]
        [NonSerialized]
        private double[][] input;

        [XmlIgnore]
        [NonSerialized]
        private Bitmap bitmap;


        public BindingList<String> Classes { get; set; }

        public GestureData[] SourcePath { get; set; } //point[] sourcepath

        public int Output { get; set; }

        public int RecognizedAs { get; set; }



        public Sequence()
        {
            RecognizedAs = -1;
        }


        public string OutputName
        {
            get { return Classes[Output]; }
        }

        public string RecognizedAsName
        {
            get { return RecognizedAs >= 0 ? Classes[RecognizedAs] : "-"; }
        }


        public double[][] Input
        {
            get
            {
                if (input == null)
                    input = Preprocess(SourcePath);
                return input;
            }
        }


        public Bitmap Bitmap
        {
            get
            {
                if (bitmap == null && SourcePath != null)
                    bitmap = ToBitmap(SourcePath);
                return bitmap;
            }
        }


        public static double[][] Preprocess(GestureData[] sequence) //point[] seq
        {
            double[][] result = new double[sequence.Length][];
            for (int i = 0; i < sequence.Length; i++)
                result[i] = new double[] { sequence[i].HandR.X, sequence[i].HandR.Y }; //using only handRight for now to paint gesture, using ONLY X,Y TO TRAIN HMM TOO!! UPDATE
            //but need to add all, right now only right hand gesture will be trained using accord. !UPDATE!
            double[][] zscores = Accord.Statistics.Tools.ZScores(result);

            return zscores.Add(10);
        }

        public static Bitmap ToBitmap(GestureData[] sequence) //point[] seq
        {
            if (sequence.Length == 0)
                return null;

            int xmax = (int)sequence.Max(x => x.HandR.X);
            int xmin = (int)sequence.Min(x => x.HandR.X);

            int ymax = (int)sequence.Max(x => x.HandR.Y);
            int ymin = (int)sequence.Min(x => x.HandR.Y);

            int width = xmax - xmin;
            int height = ymax - ymin;


            Bitmap bmp = new Bitmap(width + 16, height + 16);

            Graphics g = Graphics.FromImage(bmp);


            for (int i = 1; i < sequence.Length; i++)
            {
                int x = (int)sequence[i].HandR.X - xmin;
                int y = (int)sequence[i].HandR.Y - ymin;
                int p = (int)Accord.Math.Tools.Scale(0, sequence.Length, 0, 255, i);

                int prevX = (int)sequence[i - 1].HandR.X - xmin;
                int prevY = (int)sequence[i - 1].HandR.Y - ymin;

                using (Brush brush = new SolidBrush(Color.FromArgb(255 - p, 0, p)))
                using (Pen pen = new Pen(brush, 16))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, prevX, prevY, x, y);
                }
            }

            return bmp;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
