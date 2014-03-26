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

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;

using Accord.Statistics.Models.Fields;
using Accord.Statistics.Models.Fields.Functions;
using Accord.Statistics.Models.Fields.Learning;

using Accord.Statistics.Models.Markov;
using Accord.Statistics.Models.Markov.Learning;
using Accord.Statistics.Models.Markov.Topology;
using Gestures.Native;

using Microsoft.Kinect; //for Kinect libraries
using System.Web.UI.DataVisualization.Charting; //for Point3D class
using System.Collections.Generic; //for List Collection
using System.Drawing.Imaging;

using System.Runtime.InteropServices;//for Marshal

//import CCT
using CCT.NUI.KinectSDK;
using CCT.NUI.Core;
using CCT.NUI.HandTracking;
using CCT.NUI.Visual;
using CCT.NUI.Core.Clustering;
using CCT.NUI.Core.Shape;
using CCT.NUI.Core.Video;


namespace Gestures
{
    public partial class MainForm : Form
    {
        private Database database;
        private HiddenMarkovClassifier<MultivariateNormalDistribution> hmm;
        private HiddenConditionalRandomField<double[]> hcrf;

        //Kinect code
        KinectSensor _kinectDevice;
        Skeleton[] skeletonData;

        byte[] pixeldata;
       // SpeechSynthesizer ttsout; //for Speech synth
        bool ShouldSpeakOut = true;

        Boolean flagRecording = false;
        Boolean isGestureOver = false;
        List<GestureData> sequence;
        static List<GestureData> passableSequence;

        //global label strin
        string label;
        bool detectionDone = false;
        //to limit the no of frames being captured in skeletonTracker. currently at FPS/3. (24FPS)
        int frameCount = 0;

        //for CCT func
        private ClusterDataSourceSettings clusteringSettings = new ClusterDataSourceSettings();
        private ShapeDataSourceSettings shapeSettings = new ShapeDataSourceSettings();
        private HandDataSourceSettings handDetectionSettings = new HandDataSourceSettings();

        public MainForm()
        {
            InitializeComponent();

            database = new Database();
            gridSamples.AutoGenerateColumns = false;
            cbClasses.DataSource = database.Classes;
            gridSamples.DataSource = database.Samples;

            openDataDialog.InitialDirectory = Path.Combine(Application.StartupPath, "Resources");
            sequence= new List<GestureData>(); //init the GestureData list
            InitKinect(); //initialize Kinect sensor and its various data streams
            //ttsout = new SpeechSynthesizer(); //init the speech synth object
            passableSequence = sequence;
           // SpeakOut("Hello everyone!");
        }

        void InitKinect()
        {

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this._kinectDevice = potentialSensor;
                    break;
                }
            }

            if( null != this._kinectDevice)
            {
                //_kinectDevice = KinectSensor.KinectSensors[0];
                _kinectDevice.SkeletonStream.Enable();
                _kinectDevice.ColorStream.Enable();
                _kinectDevice.DepthStream.Enable();
                _kinectDevice.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(myKinect_ColorFrameReady);
                _kinectDevice.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonHandler);
                try
                {
                    _kinectDevice.Start();
                    skeletonData = new Skeleton[_kinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                    EnableNearModeSkeletalTracking();
                }
                catch (IOException)
                {
                    this._kinectDevice = null;
                }

                System.Diagnostics.Debug.WriteLine("detected:========="+ _kinectDevice.Status);
                //button for gesture recording.ADD.
                btnRecord.Enabled = true;
                btnRecord.Click += new EventHandler(btnRecord_Click);

                BtnCameraUp.Enabled = true;
                BtnCameraDown.Enabled = true;
                BtnCameraUp.Click += new EventHandler(BtnCameraUpClick);
                BtnCameraDown.Click += new EventHandler(BtnCameraDownClick);
            }

            #region CCT code
            //CCT hand tracking code:
            try
            {
                this.clusteringSettings.MaximumDepthThreshold = 2000; //added threshold. as per CCT eg
                IDataSourceFactory dataSourceFactory = new SDKDataSourceFactory(useNearMode: false); //trying new modded src
                var handDataSource = new HandDataSource(dataSourceFactory.CreateShapeDataSource(this.clusteringSettings, this.shapeSettings), this.handDetectionSettings);
                handDataSource.NewDataAvailable += new NewDataHandler<HandCollection>(handDataSource_NewDataAvailable);
                handDataSource.Start();
                System.Diagnostics.Debug.WriteLine("hand data src started");

            }
            catch (ArgumentOutOfRangeException exc)
            {
                //Cursor.Current = Cursors.Default;
                //MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine("===Error using CCT !!=========== " + exc.Message);
                //return;
            }
            #endregion

        }

        void handDataSource_NewDataAvailable(HandCollection data)
        {
            for (int index = 0; index < data.Count; index++)
            {
                var hand = data.Hands[index];
                Console.WriteLine(string.Format("Fingers on hand {0}: {1}", index, hand.FingerCount));
                //HINT : the current contet when running is on a separate thread, need to use begininvoke or a backgroundworker to change the UItext

                //uncomment the following 1 line, and get it working if you can. 
                //updateHandLabels(data.Count, hand.FingerCount);

                //offloaded to a separate method.
                //handLabel.Text = "Hands: " + data.Count;
                //fingerLabel.Text = "Fingers: " + hand.FingerCount;
            }
        }

        void updateHandLabels(int hands, int fingers)
        {
            handLabel.Text = "Hands: " + hands.ToString();
            fingerLabel.Text = "Fingers: " + fingers.ToString();
        }

        private void SkeletonHandler(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame sF = e.OpenSkeletonFrame())
            {

                // check that a frame is available
                if (sF != null && this.skeletonData != null)
                {
                    // get the skeletal information in this frame
                    sF.CopySkeletonDataTo(this.skeletonData);
                    foreach (Skeleton sk in this.skeletonData)
                    {
                        if (sk.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            TraceTrackedSkeletonJoints(sk.Joints);
                        }
                    }
                }
               
            }
        }

        void TraceTrackedSkeletonJoints(JointCollection jCollection)
        {
            Point3D shl, shr, elbl, elbr, wrstl, wrstr, handl, handr, spine, head;
            if (flagRecording && frameCount >= 3)
            {

                //add check if jointdata is non null
                if (jCollection[JointType.ShoulderLeft].TrackingState == JointTrackingState.Tracked)
                {
                     shl = new Point3D(jCollection[JointType.ShoulderLeft].Position.X, jCollection[JointType.ShoulderLeft].Position.Y, jCollection[JointType.ShoulderLeft].Position.Z);
                }
                else 
                {
                     shl = new Point3D(0,0,0);
                }

                if (jCollection[JointType.ShoulderRight].TrackingState == JointTrackingState.Tracked)
                {
                     shr = new Point3D(jCollection[JointType.ShoulderRight].Position.X, jCollection[JointType.ShoulderRight].Position.Y, jCollection[JointType.ShoulderRight].Position.Z);
                }
                else 
                {
                     shr = new Point3D(0,0,0);
                }

                
                if(jCollection[JointType.ElbowLeft].TrackingState == JointTrackingState.Tracked)
                {
                     elbl = new Point3D(jCollection[JointType.ElbowLeft].Position.X, jCollection[JointType.ElbowLeft].Position.Y, jCollection[JointType.ElbowLeft].Position.Z);
                }
                else 
                {
                     elbl = new Point3D(0,0,0);
                }

                if(jCollection[JointType.ElbowRight].TrackingState == JointTrackingState.Tracked )
                {
                     elbr = new Point3D(jCollection[JointType.ElbowRight].Position.X, jCollection[JointType.ElbowRight].Position.Y, jCollection[JointType.ElbowRight].Position.Z);
                }
                else 
                {
                     elbr = new Point3D(0,0,0);
                }


               
                    if(jCollection[JointType.WristLeft].TrackingState == JointTrackingState.Tracked)
                    {
                         wrstl = new Point3D(jCollection[JointType.WristLeft].Position.X, jCollection[JointType.WristLeft].Position.Y, jCollection[JointType.WristLeft].Position.Z);
                    }
                else 
                {
                     wrstl = new Point3D(0,0,0);
                }


                    if(jCollection[JointType.WristRight].TrackingState == JointTrackingState.Tracked)
                    {
                          wrstr = new Point3D(jCollection[JointType.WristRight].Position.X, jCollection[JointType.WristRight].Position.Y, jCollection[JointType.WristRight].Position.Z);
                    }
                else 
                {
                     wrstr = new Point3D(0,0,0);
                }

                   if(jCollection[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                   {
                         handl = new Point3D(jCollection[JointType.HandLeft].Position.X, jCollection[JointType.HandLeft].Position.Y, jCollection[JointType.HandLeft].Position.Z);
                   }
                else 
                {
                     handl = new Point3D(0,0,0);
                }


                if(jCollection[JointType.HandRight].TrackingState == JointTrackingState.Tracked )
                {
                      handr = new Point3D(jCollection[JointType.HandRight].Position.X, jCollection[JointType.HandRight].Position.Y, jCollection[JointType.HandRight].Position.Z);
                }
                else 
                {
                     handr = new Point3D(0,0,0);
                }
                    
                
                if(jCollection[JointType.Spine].TrackingState == JointTrackingState.Tracked )
                {
                     spine = new Point3D(jCollection[JointType.Spine].Position.X, jCollection[JointType.Spine].Position.Y, jCollection[JointType.Spine].Position.Z);
                }
                else 
                {
                     spine = new Point3D(0,0,0);
                }

                if(jCollection[JointType.Head].TrackingState == JointTrackingState.Tracked)
                {
                     head = new Point3D(jCollection[JointType.Head].Position.X, jCollection[JointType.Head].Position.Y, jCollection[JointType.Head].Position.Z);
                }
                else 
                {
                     head = new Point3D(0,0,0);
                }
                //frameCount++;

                    GestureData gd = new GestureData(shl, shr, elbl, elbr, wrstl, wrstr, handl, handr, spine, head);
                    sequence.Add(gd);
                    System.Diagnostics.Debug.WriteLine("Seq added===========");
                    frameCount = 0;

                }
            else if(flagRecording)
            {
                frameCount++;
                System.Diagnostics.Debug.Write("~");
            }
      }

        
        private void EnableNearModeSkeletalTracking()
        {
            if (this._kinectDevice != null && this._kinectDevice.DepthStream != null && this._kinectDevice.SkeletonStream != null)
            {
                //this._kinectDevice.DepthStream.Range = DepthRange.Near; // Depth in near range enabled
                this._kinectDevice.SkeletonStream.EnableTrackingInNearRange = true; // enable returning skeletons while depth is in Near Range
                this._kinectDevice.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated; // Use seated tracking
                System.Diagnostics.Debug.WriteLine("============enabled near mode=============");
            }
        }

        private void btnRecord_Click(object sender, EventArgs e) //this is a gesture recording toggle button.
        {
            if (flagRecording) //current recording, stopping now
            {
                
                flagRecording = false;//stopeed
                inputCanvas_GestureDone();
                btnRecord.Text = "Start Recording!"; //button toggled, can be used to start now
                btnRecord.ForeColor = Color.Black;
                //call func to get 3d point data, keep adding points as hand moves
                //with func recording data if skel is moving and flag is true.
            }
            else
            {
                //start recording
                flagRecording = true;
                canvas_GestureBegin();
                btnRecord.Text = "Stop Recording!"; //button toggled, can be used to stop recording
                btnRecord.ForeColor = Color.Red; //visual indication.
            }
        }


        byte[] colorData = null;
        Bitmap kinectVideoBitmap = null;
        IntPtr colorPtr;

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;
 
                if (colorData == null)
                    colorData = new byte[colorFrame.PixelDataLength];
 
                colorFrame.CopyPixelDataTo(colorData);
 
                Marshal.FreeHGlobal(colorPtr);
                colorPtr = Marshal.AllocHGlobal(colorData.Length);
                Marshal.Copy(colorData, 0, colorPtr, colorData.Length);
 
                kinectVideoBitmap = new Bitmap(
                    colorFrame.Width,
                    colorFrame.Height,
                   colorFrame.Width * colorFrame.BytesPerPixel,
                    PixelFormat.Format32bppRgb,
                    colorPtr);
                    
                kinImage.Image = kinectVideoBitmap;
 
                //kinectVideoBitmap.Dispose();
 
            }
 
        }
        

        //void ColorImageFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        //{
        //    // PlanarImage Image = e.ImageFrame.Image;
        //    ColorImageFrame Image;
        //    //BitmapImage bitmap;
        //    using (Image = e.OpenColorImageFrame())
        //    {
        //        if(Image == null) return;
        //        {


        //        Bitmap bm = ImageToBitmap(Image);
               
        //            kinImage = Image;

        //            //CalculateFps();
        //        }
                
        //        {
        //            System.Diagnostics.Debug.WriteLine("bitmap empty :/");
        //        }
        //    }
        //}

    //Bitmap ImageToBitmap(ColorImageFrame Image)
    //    {

    //        if (Image != null)
    //        {
    //            if (pixeldata == null)
    //            {
    //                pixeldata = new byte[Image.PixelDataLength];
    //            }

    //            Image.CopyPixelDataTo(pixeldata);
    //            bmap = new Bitmap(Image.Width, Image.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
    //            BitmapData bmapdata = bmap.LockBits(
    //                new System.Drawing.Rectangle(0, 0, Image.Width, Image.Height),
    //                ImageLockMode.WriteOnly,
    //                bmap.PixelFormat);
    //            IntPtr ptr = bmapdata.Scan0;
    //            Marshal.Copy(pixeldata, 0, ptr, Image.PixelDataLength);
    //            bmap.UnlockBits(bmapdata);
    //            return bmap;
    //        }
    //        // byte[] pixeldata = new byte[Image.PixelDataLength];
    //        // Image.CopyPixelDataTo(pixeldata);
    //        else
    //        {
    //            return null;
    //        }
    //    }

        private void btnLearnHMM_Click(object sender, EventArgs e)
        {
            if (gridSamples.Rows.Count == 0)
            {
                MessageBox.Show("Please load or insert some data first.");
                return;
            }

            BindingList<Sequence> samples = database.Samples;
            BindingList<String> classes = database.Classes;

            double[][][] inputs = new double[samples.Count][][];
            int[] outputs = new int[samples.Count];

            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = samples[i].Input; //fine.
                outputs[i] = samples[i].Output;
            }

            int states = 5;//def:5,should change this.
            int iterations = 0;
            double tolerance = 0.01; //reqd to change?
            bool rejection = false;


            hmm = new HiddenMarkovClassifier<MultivariateNormalDistribution>(classes.Count,
                new Forward(states), new MultivariateNormalDistribution(12) , classes.ToArray()); //!UPDATE!, no of dimension/features2 stand for?, and forward states?


            // Create the learning algorithm for the ensemble classifier
            var teacher = new HiddenMarkovClassifierLearning<MultivariateNormalDistribution>(hmm,

                // Train each model using the selected convergence criteria
                i => new BaumWelchLearning<MultivariateNormalDistribution>(hmm.Models[i])
                {
                    Tolerance = tolerance,
                    Iterations = iterations,


                    FittingOptions = new NormalOptions()
                    {
                        Regularization = 1e-5
                    }
                }
            );

            teacher.Empirical = true;
            teacher.Rejection = rejection;


            // Run the learning algorithm
            double error = teacher.Run(inputs, outputs);


            // Classify all training instances
            foreach (var sample in database.Samples)
            {
                sample.RecognizedAs = hmm.Compute(sample.Input);
            }

            foreach (DataGridViewRow row in gridSamples.Rows)
            {
                var sample = row.DataBoundItem as Sequence;
                row.DefaultCellStyle.BackColor = (sample.RecognizedAs == sample.Output) ?
                    Color.LightGreen : Color.White;
            }

            btnLearnHCRF.Enabled = true;
        }

        private void btnLearnHCRF_Click(object sender, EventArgs e)
        {
            if (gridSamples.Rows.Count == 0)
            {
                MessageBox.Show("Please load or insert some data first.");
                return;
            }

            var samples = database.Samples;
            var classes = database.Classes;

            double[][][] inputs = new double[samples.Count][][];
            int[] outputs = new int[samples.Count];

            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = samples[i].Input;
                outputs[i] = samples[i].Output;
            }

            int iterations = 100;
            double tolerance = 0.01;


            hcrf = new HiddenConditionalRandomField<double[]>(
                new MarkovMultivariateFunction(hmm));


            // Create the learning algorithm for the ensemble classifier
            var teacher = new HiddenResilientGradientLearning<double[]>(hcrf)
            {
                Iterations = iterations,
                Tolerance = tolerance
            };


            // Run the learning algorithm
            double error = teacher.Run(inputs, outputs);


            foreach (var sample in database.Samples)
            {
                sample.RecognizedAs = hcrf.Compute(sample.Input);
            }

            foreach (DataGridViewRow row in gridSamples.Rows)
            {
                var sample = row.DataBoundItem as Sequence;
                row.DefaultCellStyle.BackColor = (sample.RecognizedAs == sample.Output) ?
                    Color.LightGreen : Color.White;
            }
        }



        // Load and save database methods
        private void openDataStripMenuItem_Click(object sender, EventArgs e)
        {
            openDataDialog.ShowDialog();
        }

        private void saveDataStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDataDialog.ShowDialog();
        }

        private void openDataDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            hmm = null;
            hcrf = null;

            using (var stream = openDataDialog.OpenFile())
                database.Load(stream);

            btnLearnHMM.Enabled = true;
            btnLearnHCRF.Enabled = false;

            panelClassification.Visible = false;
            panelUserLabeling.Visible = false;
        }

        private void saveDataDialog_FileOk(object sender, CancelEventArgs e)
        {
            using (var stream = saveDataDialog.OpenFile())
                database.Save(stream);
        }

        private void btnFile_MouseDown(object sender, MouseEventArgs e)
        {
            menuFile.Show(button4, button4.PointToClient(Cursor.Position));
        }



        // Top user interaction panel box events
        private void btnYes_Click(object sender, EventArgs e)
        {
            //addGesture();
            detectionDone = true; //modified
            add3DGesture();
            panelClassification.Visible = false;//hide after confirmation
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            panelClassification.Visible = false;
            panelUserLabeling.Visible = true;
        }


        // Bottom user interaction panel box events
        private void btnClear_Click(object sender, EventArgs e)
        {
            canvas.Clear();
            panelUserLabeling.Visible = false;
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            //addGesture();
            add3DGesture();
        }

        public GestureData[] Get3DSequence()
        {
            return sequence.ToArray();
        }

        //add GEsture -- obsolete
        //private void addGesture()
        //{
        //    //get Text Lable for the performed gesture
        //    string selectedItem = cbClasses.SelectedItem as String;
        //    string classLabel = String.IsNullOrEmpty(selectedItem) ?
        //        cbClasses.Text : selectedItem;
        //    //add labelled gesture to database 
        //    if (database.Add(canvas.GetSequence(), classLabel) != null)
        //    {
        //        canvas.Clear();

        //        if (database.Classes.Count >= 2 &&
        //            database.SamplesPerClass() >= 3)
        //            btnLearnHMM.Enabled = true;

        //        panelUserLabeling.Visible = false;
        //    }
        //}

        //add 3D gesture event (based on addGEsture) to database, adds the sequence(list of GestureData objects) and classLabel
        
        private void add3DGesture()
        {
            //get Text Lable for the performed gesture
            string selectedItem = cbClasses.SelectedItem as String;
            string classLabel;
            if (detectionDone)
            {
                classLabel = label; //from global label (detected)
                detectionDone = false;
            }
            else
            {
                classLabel = String.IsNullOrEmpty(selectedItem) ?
                    cbClasses.Text : selectedItem;
            }
            //add labelled gesture to database 
           // canvas.setSequence();
            if (database.Add(Get3DSequence(), classLabel) != null) //!modify! canvas.get3Dsequnce
            {
                canvas.Clear();

                if (database.Classes.Count >= 2 &&
                    database.SamplesPerClass() >= 3)
                    btnLearnHMM.Enabled = true;

                panelUserLabeling.Visible = false;
            }

            System.Diagnostics.Debug.WriteLine("Sequence complete");

                //clear the sequence list, so it may be ready to receive data for next gesture
                sequence.Clear(); // = new List<GestureData>(); //!added. check 
           
        }

        


        // Canvas events - obsolete
        private void inputCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            double[][] input = Sequence.Preprocess(Get3DSequence());

            if (input.Length < 5)
            {
                panelUserLabeling.Visible = false;
                panelClassification.Visible = false;
                return;
            }

            if (hmm == null && hcrf == null)
            {
                panelUserLabeling.Visible = true;
                panelClassification.Visible = false;
            }

            else
            {
                int index = (hcrf != null) ? hcrf.Compute(input) : hmm.Compute(input);
                string label = database.Classes[index];
                lbHaveYouDrawn.Text = String.Format("Have you painted a {0}?", label);
                panelClassification.Visible = true;
                panelUserLabeling.Visible = false;
            }
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            canvas.Clear();
            lbIdle.Visible = false;
        }

        //Gesture events based on Canvas events, modded for 3D space-time gestures
        private void inputCanvas_GestureDone()
        {
            double[][] input = Sequence.Preprocess(Get3DSequence());

            if (input.Length < 5) //length of input, might incr this because now gestures not screen drawing.
            {
                System.Diagnostics.Debug.WriteLine("----input gesture was too short---!");
                panelUserLabeling.Visible = false;
                panelClassification.Visible = false;
                return;
            }

            if (hmm == null && hcrf == null)
            {
                panelUserLabeling.Visible = true;
                panelClassification.Visible = false;
            }

            else
            {
                int index = (hcrf != null) ?
                    hcrf.Compute(input) : hmm.Compute(input); //check if input is well formed from sequence preprocessing, only xy breakpoint

                label = database.Classes[index]; //modified
                lbHaveYouDrawn.Text = String.Format("Have you drawn a {0}?", label);
                //SpeakOut(label); //speak the label
                
                panelClassification.Visible = true;
                panelUserLabeling.Visible = false;

            }
        }

        private void canvas_GestureBegin()
        {
            lbIdle.Visible = false;
        }



        // Aero Glass settings
        //
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Perform special processing to enable aero
            if (SafeNativeMethods.IsAeroEnabled)
            {
                ThemeMargins margins = new ThemeMargins();
                margins.TopHeight = canvas.Top;
                margins.LeftWidth = canvas.Left;
                margins.RightWidth = ClientRectangle.Right - gridSamples.Right;
                margins.BottomHeight = ClientRectangle.Bottom - canvas.Bottom;

                // Extend the Frame into client area
                SafeNativeMethods.ExtendAeroGlassIntoClientArea(this, margins);
            }
        }

        /// <summary>
        ///   Paints the background of the control.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            if (SafeNativeMethods.IsAeroEnabled)
            {
                // paint background black to enable include glass regions
                e.Graphics.Clear(Color.FromArgb(0, this.BackColor));
            }
        }

        /// <summary>
        /// Camera Angle Button Handlers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCameraUpClick(object sender, EventArgs e)
        {
            try
            {
                _kinectDevice.ElevationAngle = _kinectDevice.ElevationAngle + 5;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (ArgumentOutOfRangeException outOfRangeException)
            {
                //Elevation angle must be between Elevation Minimum/Maximum"
                MessageBox.Show(outOfRangeException.Message);
            }
        }

        private void BtnCameraDownClick(object sender, EventArgs e)
        {
            try
            {
                _kinectDevice.ElevationAngle = _kinectDevice.ElevationAngle - 5;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (ArgumentOutOfRangeException outOfRangeException)
            {
                //Elevation angle must be between Elevation Minimum/Maximum"
                MessageBox.Show(outOfRangeException.Message);
            }
        }


        public static List<GestureData> GetSeq()
        {
            return passableSequence;
        }

        
        //private void SpeakOut(String s)
        //{
        //    //Speech TTS demo
        //    //if(null != ttsout)
        //    //ttsout.Dispose();
        //    if (ShouldSpeakOut && null != s)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Speaking things~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        //        ttsout = new SpeechSynthesizer();
        //        ttsout.SpeakAsync(s);
        //        //ttsout.Speak(s);
        //    }
        //}

       


    }
}
