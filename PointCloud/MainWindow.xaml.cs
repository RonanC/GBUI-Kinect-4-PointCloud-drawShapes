using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Media.Media3D;

using Microsoft.Kinect;

namespace PointCloud
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;

        public MainWindow()
        {
            InitializeComponent();
            setupKinectSensor();
        }

        private void setupKinectSensor()
        {
            // should check for one, going to presume that kinect[0] is working.
            try
            {
                // presuming that sensor[0[ is there and configured, if not, this will crash.
                sensor = KinectSensor.KinectSensors[0];
                // if connected, set up the event handlersxz.
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

                sensor.AllFramesReady += sensor_AllFramesReady;

                sensor.Start();
            }
            catch (Exception error)
            {
                tblUpdates.Text = "Error Connecting: " + error.Message;
            }



        }

        ColorImageFrame CFrame;
        SkeletonFrame SFrame;
        Skeleton[] skeletonArray;

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            BitmapSource bmap;

            // when the frames are ready, retrieve the video data,
            // mark the location of the head on the video with an ellipse
            CFrame = e.OpenColorImageFrame();
            if (CFrame == null) return;

            // have data, then I can do stuff.
            imgDisplay.Width = CFrame.Width;
            imgDisplay.Height = CFrame.Height;
            // copy the frame to the bitmap source
            bmap = imageToBitmap(CFrame);

            //            imgDisplay.Source = bmap;

            // get the skeleton data
            SFrame = e.OpenSkeletonFrame();
            if (SFrame == null) return;

            // otherwise, get the array of skeletons that are available
            // check for ones that are tracked
            skeletonArray = new Skeleton[SFrame.SkeletonArrayLength];
            SFrame.CopySkeletonDataTo(skeletonArray);
            foreach (Skeleton S in skeletonArray)
            {
                // check if tracked
                // if it is, draw an ellipse at the head or some other joint
                if (S.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // get the joint location for the head.
                    // all joints are named, return a coordinate.s
                    // x - across body, y - vertical , z+ve is towards the sensor.
                    SkeletonPoint sHead = S.Joints[JointType.Head].Position;
                    SkeletonPoint sRWrist = S.Joints[JointType.WristRight].Position;

                    ColorImagePoint cLocHead = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sHead,
                                                                                    ColorImageFormat.RgbResolution640x480Fps30);

                    ColorImagePoint cLocRWrist = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sRWrist,
                                                                                    ColorImageFormat.RgbResolution640x480Fps30);

                    markAtPoint(cLocHead, cLocRWrist, bmap);

                } // end if tracking state

            }// end foreach

        }

        private void markAtPoint(ColorImagePoint head, ColorImagePoint rWrist, BitmapSource bmap)
        {
            // use the bmap to create a writeable bitmap and then overlay the ellipse
            // where the persons head should be.
            DrawingVisual myDV = new DrawingVisual();
            DrawingContext myDC = myDV.RenderOpen();
            myDC.DrawImage(bmap, new Rect(0, 0, imgDisplay.Width, imgDisplay.Height));
            myDC.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 3), new Point(head.X, head.Y), 5, 5);
            myDC.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 3), new Point(rWrist.X, rWrist.Y), 5, 5);

            myDC.Close();
            // use the generated images as the source for the imgDisplay
            var img = new DrawingImage(myDV.Drawing);
            imgDisplay.Source = img;

        }

        private BitmapSource imageToBitmap(ColorImageFrame imgData)
        {
            // set the size of the array from the description of the frame.
            byte[] pixelData = new byte[imgData.PixelDataLength];
            imgData.CopyPixelDataTo(pixelData);

            BitmapSource bmap = BitmapSource.Create(imgData.Width,
                                                    imgData.Height,
                                                    96,
                                                    96,
                                                    PixelFormats.Bgr32,
                                                    null,
                                                    pixelData,
                                                    imgData.Width * imgData.BytesPerPixel);
            return bmap;
        }


    }
}
