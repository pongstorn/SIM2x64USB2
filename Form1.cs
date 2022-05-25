//=============================================================================
// Copyright © 2010 Point Grey Research, Inc. All Rights Reserved.
//
// This software is the confidential and proprietary information of Point
// Grey Research, Inc. ("Confidential Information").  You shall not
// disclose such Confidential Information and shall use it only in
// accordance with the terms of the license agreement you entered into
// with PGR.
//
// PGR MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF THE
// SOFTWARE, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE, OR NON-INFRINGEMENT. PGR SHALL NOT BE LIABLE FOR ANY DAMAGES
// SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
// THIS SOFTWARE OR ITS DERIVATIVES.
//=============================================================================
//=============================================================================
// $Id: Form1.cs,v 1.4 2011-02-03 23:34:52 soowei Exp $
// $Id: Form1.cs,v 2 2012-02-03 23:34:52 Pongstorn Exp $
//=============================================================================

//#define Decimal2 //for Thailand : Decimal2
#define Decimal //for vietnam is Decimal -> change to Demical2 for Thailand



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Xml;



using System.Diagnostics;


using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

using ZedGraph;
using SpinnakerNET;
using SpinnakerNET.GenApi;




namespace FlyCapture2SimpleGUI_CSharp
{
    public partial class Form1 : Form
    {



        public const int iMaxEdge = 500;
        private int intMaxShrimp;
        private int intSampleSize;
        //private FlyCapture2Managed.Gui.CameraControlDialog m_camCtlDlg;
        //private ManagedCameraBase m_camera = null;
        private IManagedCamera m_camera = null;
        private IManagedImage m_rawImage;
        private IManagedImage m_processedImage;
        private bool m_grabImages;
        private bool m_grabShrimp;
        private bool boolOneShot = false;
        private AutoResetEvent m_grabThreadExited;
        private BackgroundWorker m_grabThread;
        private int intX1, intX2, intY1, intY2;
        private int intBoxX1, intBoxX2, intBoxY1, intBoxY2;
        private int intPixOverThresholdPos = 0;
        private byte ByteThreshold;
        private byte ByteThresholdS;
        private byte ByteThresholdL;
        private byte ByteLineThreshold;
        private byte byteNumSection;
        private byte ByteBubble;
        byte byteRow;
        int intSizeWidth, intSizeHeight;
        private byte[,] arrayBitmap0;
        private int EdgeCount = 0;
        private int PixelState = 0;
        private int intLineMinX = 0;
        private int intLineMaxX = 0;
        private int intLineMinY = 0;
        private int intLineMaxY = 0;
        private int intCoarsePixel;
        private byte byteFinePixel;
        private int intBmpWidth, intBmpHeight;
        private int intShrimpCounter;
        private double dblMaxWidth, dblMinWidth, dblMinHeight;
        private double dblMaxRatio;
        private double dblTL1, dblTL2, dblTL3, dblTL4, dblTL5;
        private int intMaxWidth, intMinWidth, intMinHeight;
        private string subNewPath;
        int intHairBrightness;
        const int cornersX = 13;
        const int cornersY = 7;
        int[] iMedianDot;
        int[,] iMedianDotShrimp;
        int iSectionSpace;
        PointF[] corners;
        PointF[] ChessCorners = new PointF[cornersX * cornersY];
        PointF[] ShrimpSection;
        int[] intArrLineMaxX;
        int[] intArrLineMinX;
        int[] intArrLineMaxY;
        int[] intArrLineMinY;
        int[] intEdgeX;
        int[] intEdgeY;
        bool bDrawLine;
        bool bDrawShrimpCircleUp;
        bool bDrawShrimpCircleDown;
        bool bBombEdgeCount = false;
        bool bBombCW = false;
        double[] ldblShrimpLength;
        double dblCorFactor;
        double dblPix2mm;
        double dblSlope;
        double dblOffset;
        double dblOffsetHuman;
        double dblShrimpHair_mm;
        double SlopeLargeThreshold;
        int intIndexData = 0;
        Image<Gray, Byte> grayFrame;
        byte bytePixel10mm;
        byte bytePixel7mm;
        byte bytePixel4mm;
        int intSkipFrame;
        int intCountSkipFrame;
        int intNewImage = 0;

        public Form1()
        {
            InitializeComponent();

            m_rawImage = new ManagedImage();
            m_processedImage = new ManagedImage();
            //m_camCtlDlg = new CameraControlDialog();

            m_grabThreadExited = new AutoResetEvent(false);
        }

        private void UpdateUI(object sender, ProgressChangedEventArgs e)
        {
            UpdateStatusBar();
            //Image<Gray, Byte> grayFrame = new Image<Gray, Byte>(m_processedImage.bitmap); //where bmp is a Bitmap

            if (intNewImage == 1)
            {
                grayFrame = new Image<Gray, Byte>(m_processedImage.bitmap); //where bmp is a Bitmap
                imageBox1.Image = grayFrame;
                imageBox1.Invalidate();
            }

        }


        Image<Gray, Byte> gray0;

        private void UpdateUIshrimp(object sender, ProgressChangedEventArgs e)
        {
            int i, j;
            int intPixOverThreshold;
            int iSectionSpace;
            byte bSection;
            byte bytePixelR1;
            byte bytePixelR2;

            bool isWhite = false;
            bool Logic0, Logic1, Logic2;
            string strPathSave;
            byte bFindMedianState;
            int iFirstWhiteDot;
            int iLastWhiteDot;

            bBombEdgeCount = false;
            bBombCW = false;

            Stopwatch watch;
            watch = Stopwatch.StartNew();
            //UpdateStatusBar();
            Image<Bgr, Byte> img1 = new Image<Bgr, Byte>(m_processedImage.bitmap);
            grayFrame = img1.Convert<Gray, Byte>(); //where bmp is a Bitmap                                 
            // save to buffer


            int intROIWidth, intROIHeight;
            intROIHeight = intBoxY2 - intBoxY1;
            intROIWidth = intBoxX2 - intBoxX1;

            if (boolOneShot)
            {
                gray0 = grayFrame.Copy();

                for (i = 0; i < intBmpWidth; i = i + 1)
                {

                    for (j = intBoxY1; j < intBmpHeight; j = j + 1)
                        arrayBitmap0[i, j] = grayFrame.Data[j, i, 0];
                }

                if (Math.Abs(gray0.Data[320, 240, 0] - gray0.Data[320, 241, 0]) < 5)
                {
                    byteBackColor = gray0.Data[320, 240, 0];
                }

                gray0.ROI = new Rectangle(intBoxX1, intBoxY1, intROIWidth, intROIHeight);
                if (chkSaveImage.Checked)
                {
                    strPathSave = subNewPath + "\\" + (intShrimpCounter).ToString() + ".bmp";
                    m_processedImage.Save(strPathSave);
                }
            }

            boolOneShot = false;

            //    Add algorithm to detect shrimp
            Logic0 = false;
            if (rdoAllSize.Checked)
            {
                Logic2 = true;
                Logic1 = false;
            }
            else
            {
                Logic2 = false;
                Logic1 = true;
            }

            if (Logic0)
            {
                #region useSlowLineScan
                switch (PixelState)
                {
                    case 0: //detect shrimp

                        intPixOverThreshold = 0;
                        for (i = intY1; i < intY2; i = i + intCoarsePixel)
                        {
                            bytePixelR1 = grayFrame.Data[i, intX1, 0];
                            bytePixelR2 = grayFrame.Data[i, intX2, 0];

                            //LineX2 is a variable to increase possibility to detect shrimp
                            if (bytePixelR2 - arrayBitmap0[intX2, i] > ByteThreshold)
                            {
                                intPixOverThreshold++;
                                intPixOverThresholdPos = i;
                                BlobTrack(intX2, i);
                                bDrawShrimpCircleDown = true;
                                bDrawShrimpCircleUp = false;
                                break;
                            }
                            else if (bytePixelR1 - arrayBitmap0[intX1, i] > ByteThreshold)
                            {
                                intPixOverThreshold++;
                                intPixOverThresholdPos = i;
                                BlobTrack(intX1, i);
                                bDrawShrimpCircleDown = false;
                                bDrawShrimpCircleUp = true;
                                break;
                            }
                        }

                        watch.Stop();

                        if (bBombEdgeCount)
                        {
                            msg("Code Bomb Edge Count");
                            bDrawShrimpCircleUp = false;
                            bDrawShrimpCircleDown = false;
                        }
                        else if (bBombCW)
                        {
                            msg("Code Bomb Clock Wise");
                            bDrawShrimpCircleUp = false;
                            bDrawShrimpCircleDown = false;
                        }
                        else if (intPixOverThreshold > 0)
                        {
                            if ((intLineMaxX - intLineMinX > ByteBubble) ||
                                (intLineMaxY - intLineMinY > ByteBubble))
                            {
                                //***********************  Calculate Length  ************************
                                double dTempX, dTempY;
                                double dSumSection = 0;
                                double dTempSection = 0;

                                intShrimpCounter++;
                                intArrLineMaxX[intShrimpCounter] = intLineMaxX;
                                intArrLineMinX[intShrimpCounter] = intLineMinX;
                                intArrLineMaxY[intShrimpCounter] = intLineMaxY;
                                intArrLineMinY[intShrimpCounter] = intLineMinY;

                                lblShrimpCounter.Text = intShrimpCounter.ToString();
                                lblShrimpCounter.Refresh();
                                //lblCounter.Text = Convert.ToString(ShrimpCounter);
                                // Create a unique filename
                                //string filename = String.Format(
                                //"FlyCapture2Test_CSharp-{0}.bmp",
                                //intShrimpCounter);
                                strPathSave = subNewPath + "\\" + intShrimpCounter.ToString() + ".bmp";
                                m_processedImage.Save(strPathSave);
                                // Save the image

                                if ((intLineMaxY - intLineMinY) > (intLineMaxX - intLineMinX))
                                //shrimp in Vertical position, therefore scan X
                                {
                                    //Divide Shrimp into section
                                    iSectionSpace = (intLineMaxY - intLineMinY) / byteNumSection;
                                    for (bSection = 0; bSection < byteNumSection + 1; bSection++)
                                    {
                                        if (bSection == byteNumSection)
                                        {
                                            j = intLineMaxY;
                                        }
                                        else
                                        {
                                            j = intLineMinY + iSectionSpace * bSection;
                                        }

                                        //find Median from EdgeCount Array
                                        iFirstWhiteDot = 640;
                                        iLastWhiteDot = 0;

                                        for (i = 0; i < EdgeCount; i++)
                                        {
                                            if (intEdgeY[i] == j)
                                            {
                                                if (intEdgeX[i] < iFirstWhiteDot)
                                                {
                                                    iFirstWhiteDot = intEdgeX[i];
                                                }
                                                if (intEdgeX[i] > iLastWhiteDot)
                                                {
                                                    iLastWhiteDot = intEdgeX[i];
                                                }
                                            }
                                        }

                                        iMedianDot[bSection] = (iFirstWhiteDot + iLastWhiteDot) / 2;
                                        iMedianDotShrimp[intShrimpCounter, bSection] = iMedianDot[bSection];
                                        Pixel2Millimeter(bSection, iMedianDot[bSection], j);
                                    }

                                    dSumSection = 0;
                                    dTempSection = 0;
                                    for (i = 0; i < byteNumSection; i = i + 1)
                                    {
                                        dTempX = ShrimpSection[i].X - ShrimpSection[i + 1].X;
                                        dTempY = ShrimpSection[i].Y - ShrimpSection[i + 1].Y;
                                        dTempSection = Math.Sqrt(dTempX * dTempX + dTempY * dTempY);
                                        dSumSection = dTempSection + dSumSection;
                                    }

                                    dSumSection = dSumSection * dblCorFactor;          //Correct Factor

                                    lblShrimpLength.Text = String.Format("{0:0.000}", dSumSection);      // "123.46"                               
                                    ldblShrimpLength[intShrimpCounter - 1] = Convert.ToDouble(lblShrimpLength.Text);
                                    chkListBox.Items.Add(Convert.ToString(intShrimpCounter) + "   " + lblShrimpLength.Text, true);
                                }
                                else
                                //***********************  Calculate Length  Horizon  ************************
                                //therefore scan Y
                                {
                                    //Divide Shrimp into sections
                                    iSectionSpace = (intLineMaxX - intLineMinX) / byteNumSection;
                                    for (bSection = 0; bSection < byteNumSection + 1; bSection++)
                                    {
                                        //  FindShrimpMedian Position
                                        if (bSection == byteNumSection)
                                        {
                                            i = intLineMaxX;
                                        }
                                        else
                                        {
                                            i = intLineMinX + iSectionSpace * bSection;
                                        }

                                        //find Median from EdgeCount Array
                                        iFirstWhiteDot = 752;
                                        iLastWhiteDot = 0;

                                        for (j = 0; j < EdgeCount; j++)
                                        {
                                            if (intEdgeX[j] == i)
                                            {
                                                if (intEdgeY[j] < iFirstWhiteDot)
                                                {
                                                    iFirstWhiteDot = intEdgeY[j];
                                                }
                                                if (intEdgeY[j] > iLastWhiteDot)
                                                {
                                                    iLastWhiteDot = intEdgeY[j];
                                                }
                                            }
                                        }

                                        iMedianDot[bSection] = (iFirstWhiteDot + iLastWhiteDot) / 2;
                                        iMedianDotShrimp[intShrimpCounter, bSection] = iMedianDot[bSection];
                                        Pixel2Millimeter(bSection, i, iMedianDot[bSection]);

                                    }

                                    dSumSection = 0;
                                    dTempSection = 0;
                                    for (i = 0; i < byteNumSection; i = i + 1)
                                    {
                                        dTempX = ShrimpSection[i].X - ShrimpSection[i + 1].X;
                                        dTempY = ShrimpSection[i].Y - ShrimpSection[i + 1].Y;
                                        dTempSection = Math.Sqrt(dTempX * dTempX + dTempY * dTempY);
                                        dSumSection = dTempSection + dSumSection;
                                    }
                                    dSumSection = dSumSection * dblCorFactor;          //Correct Factor

                                    lblShrimpLength.Text = String.Format("{0:0.000}", dSumSection);      // "123.46"
                                    ldblShrimpLength[intShrimpCounter - 1] = Convert.ToDouble(lblShrimpLength.Text);
                                    chkListBox.Items.Add(Convert.ToString(intShrimpCounter) + "   " + lblShrimpLength.Text, true);
                                }

                                //m_processedImage.Save(strPathSave);

                                PixelState = 1;
                            }
                            else
                            {
                                msg("Detect Bubble");
                                bDrawShrimpCircleUp = false;
                                bDrawShrimpCircleDown = false;
                            }
                            imageBox1.Image = grayFrame;
                            imageBox1.Invalidate();
                            //txtPixel.AppendText(Convert.ToString(intPixOverThresholdPos) + ": " + Convert.ToString(bytePixelR) +
                            //    " Over: " + Convert.ToString(intPixOverThreshold) + Environment.NewLine);
                        }
                        break;

                    case 1:  //detect blank
                        isWhite = false;
                        for (i = intX1; i < intX2; i = i + intCoarsePixel)
                        {
                            //if (image.GetPixel(centerX + LengthX, centerY + i).R > intThreshold)
                            //if (convertedImage.bitmap.GetPixel(i, intY).R > ByteThreshold)
                            //bytePixelR1 = convertedImage.bitmap.GetPixel(i, intY).R;
                            //bytePixelR2 = convertedImage.bitmap.GetPixel(i, intY + intSpaceY).R;

                            bytePixelR1 = grayFrame.Data[intY1, i, 0];
                            bytePixelR2 = grayFrame.Data[intY2, i, 0];
                            if ((bytePixelR1 - arrayBitmap0[i, intY1] > ByteThreshold) ||
                                (bytePixelR2 - arrayBitmap0[i, intY2] > ByteThreshold))
                            {
                                isWhite = true;
                                break;
                            }
                        }

                        if (!isWhite)
                        {
                            PixelState = 0;
                        }
                        break;

                    case 2:

                        break;

                }


                #endregion
            }
            else if (Logic1)
            {
                imageBox1.BackColor = Color.Gray;

                bool bDetectShrimpX = false;

                grayFrame.ROI = new Rectangle(intBoxX1, intBoxY1, intROIWidth, intROIHeight);

                Image<Gray, Byte> S = grayFrame.Sub(gray0);

                //Point2D
                LineSegment2D LsX1 = new LineSegment2D(new Point(intX1 - intBoxX1, intY1 - intBoxY1), new Point(intX1 - intBoxX1, intY2 - intBoxY1));
                LineSegment2D LsX2 = new LineSegment2D(new Point(intX2 - intBoxX1, intY1 - intBoxY1), new Point(intX2 - intBoxX1, intY2 - intBoxY1));




                int iLineWidth = intY2 - intY1;
                byte[,] ArrayLX1, ArrayLX2;

                ArrayLX1 = new byte[iLineWidth + 1, 1];
                ArrayLX1 = S.Sample(LsX1);
                ArrayLX2 = new byte[iLineWidth + 1, 1];
                ArrayLX2 = S.Sample(LsX2);

                for (i = 0; i < iLineWidth; i++)
                {
                    if ((ArrayLX1[i, 0] > ByteLineThreshold) || (ArrayLX2[i, 0] > ByteLineThreshold))
                    {
                        //Detect Shrimp
                        bDetectShrimpX = true;
                        break;
                    }
                }



                if (bDetectShrimpX)
                {
                    if (bOldClearLine)
                    {

                        Image<Gray, Byte> Sbin = S.ThresholdBinary(new Gray(ByteThreshold), new Gray(255));
                        //Image<Gray, Byte> S = grayFrame.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY_INV, 3, new Gray(4));

                        #region Find triangles and rectangles

                        List<Triangle2DF> triangleList = new List<Triangle2DF>();
                        List<MCvBox2D> boxList = new List<MCvBox2D>();

                        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                            for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                            {
                                //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                if (contours.Area > 10) //only consider contours with area greater than 250
                                {
                                    boxList.Add(contours.GetMinAreaRect());
                                }
                            }

                        #endregion

                        img1.ROI = new Rectangle(intBoxX1, intBoxY1, intBoxX2 - intBoxX1, intBoxY2 - intBoxY1);

                        #region draw triangles and rectangles
                        Image<Bgr, Byte> RectangleImage = img1.Copy();
                        //Image<Gray, Byte> RectangleImage = S.Copy();

                        //intShrimpCounter = 0;
                        bool bFoundShrimp = false;
                        foreach (MCvBox2D box in boxList)
                        //cannyFrame.Draw(box, new Gray(255), 1);
                        {

                            if ((box.size.Width > ByteBubble) || (box.size.Height > ByteBubble))
                            {

                                // x' = xcos@ + ysin@
                                // y' = -xsin@ + ycos@
                                double dblbH = box.size.Height; //y
                                double dblbW = box.size.Width;  //x
                                double dblbA = box.angle;
                                double angle = Math.PI * dblbA / 180.0;
                                double sinA = Math.Sin(angle);
                                double cosA = Math.Cos(angle);
                                double centerX = box.center.X;
                                double centerY = box.center.Y;
                                double dblShrimpLength;
                                double dblShrimpDiameter;
                                bool bRotatePicture = false;

                                if (dblbW > dblbH)
                                {
                                    dblShrimpLength = dblbW;
                                    dblShrimpDiameter = dblbH;
                                }
                                else
                                {
                                    dblShrimpLength = dblbH;
                                    dblShrimpDiameter = dblbW;
                                    bRotatePicture = true;
                                }





                                double dblShrimpLengthmm = dblPix2mm * dblShrimpLength;

                                if (dblShrimpLength > intMaxWidth)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.AliceBlue), 1);
                                }
                                else if (dblShrimpLength < intMinWidth)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                                }
                                else if (dblShrimpDiameter < intMinHeight)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                                }
                                else if (dblShrimpLength / dblShrimpDiameter < dblMaxRatio)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.BlueViolet), 1);
                                }
                                else
                                {
                                    // check leftframe
                                    bFoundShrimp = true;
                                    double dblWidth = dblbW * cosA - dblbH * sinA;
                                    double dblHeight = dblbH * cosA - dblbW * sinA;
                                    //if (dblHeight > dblWidth)
                                    //{
                                    //    double dblTemp;
                                    //    dblTemp = dblHeight;
                                    //    dblHeight = dblWidth;
                                    //    dblWidth = dblTemp;

                                    //}
                                    if (centerX < dblWidth / 2)
                                    {
                                        // check leftframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if ((centerX + (dblWidth) / 2) + 2 > (intBoxX2 - intBoxX1)) //+ 2 to prevent error
                                    {
                                        // check rightframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerY < (dblHeight) / 2)
                                    {
                                        // check topframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if ((centerY + (dblHeight) / 2) > (intBoxY2 - intBoxY1))
                                    {
                                        // check bottomframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (dblShrimpLengthmm > dblMaxWidth)
                                    {

                                    }
                                    else if (intBoxY2 - intBoxY1 - dblShrimpDiameter / 2 - centerY < 5)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerY - dblShrimpDiameter / 2 < 5)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerX - dblShrimpLength > intX2)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                    }
                                    else if (centerX + dblShrimpLength < intX1)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                    }
                                    else
                                    {
                                        bOldClearLine = false;
                                        //RectangleImage.Draw(box, new Bgr(Color.Lime), 1);

                                        msg(dblShrimpLength.ToString("F"));
                                        //msg(box.angle.ToString("F") + "," + box.size.Height.ToString("F") + "," + box.size.Width.ToString("F"));

                                        intShrimpCounter++;


                                        int intULX = Convert.ToInt16(centerX - (dblbW * cosA - dblbH * sinA) / 2);
                                        int intULY = Convert.ToInt16(centerY - (dblbH * cosA - dblbW * sinA) / 2);
                                        Image<Bgr, Byte> imgA = RectangleImage.Copy();
                                        //Image<Gray, Byte> imgGA = imgA.Convert<Gray, Byte>();
                                        int intWidth = Convert.ToInt16(dblWidth);
                                        int intHeight = Convert.ToInt16(dblHeight);

                                        imgA.ROI = new Rectangle(intULX, intULY, intWidth, intHeight);
                                        Image<Bgr, Byte> grayFrameA = imgA.Copy();
                                        Image<Bgr, Byte> grayFrameB;
                                        if (bRotatePicture)
                                        {
                                            grayFrameB = grayFrameA.Rotate(90 - dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false); ;
                                        }
                                        else
                                        {
                                            grayFrameB = grayFrameA.Rotate(-dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false);
                                        }


                                        int intShrimpLength = Convert.ToInt16(dblShrimpLength + 1);
                                        int intShrimpDiameter = Convert.ToInt16(dblShrimpDiameter + 1);
                                        intULX = (grayFrameB.Size.Width - intShrimpLength) / 2;
                                        intULY = (grayFrameB.Size.Height - intShrimpDiameter) / 2;

                                        grayFrameB.ROI = new Rectangle(intULX, intULY, intShrimpLength + 5, intShrimpDiameter + 5);
                                        imageBox2.Image = grayFrameB;
                                        Image<Gray, Byte> grayFrameSection = grayFrameB.Convert<Gray, Byte>();


                                        byte[,] ArrayLSection;
                                        int index = 0;
                                        int indexOld = 0;
                                        double dblSumSection = 0;
                                        //add code of Divide Shrimp into section
                                        iSectionSpace = intShrimpLength / byteNumSection;
                                        double dblShrimpNumeratorLength = dblShrimpLength - iSectionSpace * (byteNumSection - 1);

                                        for (bSection = 0; bSection < byteNumSection + 1; bSection++)
                                        {
                                            if (bSection == byteNumSection)
                                            {
                                                j = intShrimpLength - 3;
                                            }
                                            else
                                            {
                                                if (bSection == 0)
                                                    j = 5;
                                                else
                                                    j = iSectionSpace * bSection + 2;
                                            }


                                            //Point2D scanshrimp section
                                            LineSegment2D Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                            ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                            ArrayLSection = grayFrameSection.Sample(Lsection);
                                            int max = 0;
                                            int rowGreen = 4;
                                            int countMax = 0;
                                            int sumPixelValue = 0;

                                            for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                            {
                                                sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                            }
                                            int Avg = sumPixelValue / (intShrimpDiameter - rowGreen + 1) + 10;

                                            bool _recOnce = true;
                                            int[] indexDiameter = new int[100];

                                            for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                            {
                                                if (ArrayLSection[i + rowGreen / 2, 0] > Avg)
                                                {
                                                    //max = ArrayLSection[i + rowGreen/2, 0];
                                                    //record first index
                                                    if (_recOnce)
                                                    {
                                                        index = i;
                                                        _recOnce = false;
                                                    }
                                                    indexDiameter[countMax] = i;
                                                    countMax++;

                                                }
                                            }

                                            //index = index + rowGreen / 2 + countMax / 2;
                                            index = rowGreen / 2 + indexDiameter[countMax / 2];

                                            if (bSection > 0)
                                            {
                                                if (bSection == byteNumSection)
                                                {
                                                    LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace, indexOld), new Point(bSection * iSectionSpace, index));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                    LB = new LineSegment2D(new Point(0, 0), new Point(0, intShrimpDiameter));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                    LB = new LineSegment2D(new Point(intShrimpLength, 0), new Point(intShrimpLength, intShrimpDiameter));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                    dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + dblShrimpNumeratorLength * dblShrimpNumeratorLength);
                                                }
                                                else
                                                {
                                                    LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace, indexOld), new Point(bSection * iSectionSpace, index));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                    dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + iSectionSpace * iSectionSpace);
                                                }
                                                // pythagoras triangular

                                            }

                                            indexOld = index;

                                        }

                                        //msg("T: " + dblSumSection.ToString("F"));

                                        dblShrimpLengthmm = dblSumSection * dblPix2mm;
                                        dblShrimpLengthmm = dblShrimpLengthmm * dblSlope + dblOffset;
                                        ldblShrimpLength[intShrimpCounter - 1] = dblShrimpLengthmm;
                                        chkListBox.Items.Add(Convert.ToString(intShrimpCounter) + "   " + dblShrimpLengthmm.ToString("#.00"), true);
                                        //lblShrimpLength.Text = dblShrimpLengthmm.ToString("F");
                                        lblShrimpLength.Text = dblShrimpLengthmm.ToString("#.00");


                                        if (bFirstFrame)
                                        {
                                            bFirstFrame = false;
                                            grayFrameC = grayFrameB.Copy();
                                            int iaddHeight = intSizeHeight - grayFrameC.Size.Height;
                                            //int iaddWidth = 250 - grayFrameC.Size.Width;
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateHorizontal(grayFrameD);
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                            imageBox3.Image = grayFrameC;
                                        }
                                        else
                                        {
                                            grayFrameD = grayFrameB.Copy();

                                            grayFrameB = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameD = grayFrameD.ConcateHorizontal(grayFrameB);

                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                            //add more space
                                            int iaddHeight = intSizeHeight - grayFrameD.Size.Height;
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);

                                            imageBox3.Image = grayFrameC;
                                            if ((intShrimpCounter % byteRow) == 0)
                                            {
                                                bFirstFrame = true;
                                                if (bFirstImageColumn)
                                                {
                                                    bFirstImageColumn = false;
                                                    grayFrameE = grayFrameC.Copy();
                                                }
                                                else
                                                {
                                                    Image<Bgr, byte> grayFrameF = grayFrameC.Copy();
                                                    grayFrameE = grayFrameE.ConcateHorizontal(grayFrameF);
                                                    imageBox3.Image = grayFrameE;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //imageBox1.Image = cannyFrame;
                        #endregion


                        //imageBox1.Image = Sbin;

                        //if (bFoundShrimp)
                        //{
                        imageBox1.Image = RectangleImage;
                        //imageBox1.Image = S;
                        //}

                        lblShrimpCounter.Text = intShrimpCounter.ToString();

                        //imageBox1.Image = img1;
                        //imageBox1.Image = S;

                        watch.Stop();
                        lblWatch.Text = watch.ElapsedMilliseconds.ToString() + " ms";
                    }
                    else
                    {
                        imageBox1.Image = img1;
                    }

                }
                else
                {
                    imageBox1.Image = img1;
                    bOldClearLine = true;
                    //watch.Stop();
                    //lblShrimpCounter.Text = "none";
                    //lblShrimpLength.Text = watch.ElapsedMilliseconds.ToString();
                }
            }



            else if (Logic2)
            {
                imageBox1.BackColor = Color.Gray;

                bool bDetectShrimpX = false;



                grayFrame.ROI = new Rectangle(intBoxX1, intBoxY1, intROIWidth, intROIHeight);

                Image<Gray, Byte> S = grayFrame.Sub(gray0);

                //Point2D
                LineSegment2D LsX1 = new LineSegment2D(new Point(intX1 - intBoxX1, intY1 - intBoxY1), new Point(intX1 - intBoxX1, intY2 - intBoxY1));
                LineSegment2D LsX2 = new LineSegment2D(new Point(intX2 - intBoxX1, intY1 - intBoxY1), new Point(intX2 - intBoxX1, intY2 - intBoxY1));




                int iLineWidth = intY2 - intY1;
                byte[,] ArrayLX1, ArrayLX2;

                ArrayLX1 = new byte[iLineWidth + 1, 1];
                ArrayLX1 = S.Sample(LsX1);
                ArrayLX2 = new byte[iLineWidth + 1, 1];
                ArrayLX2 = S.Sample(LsX2);

                for (i = 0; i < iLineWidth; i++)
                {
                    if ((ArrayLX1[i, 0] > ByteLineThreshold) || (ArrayLX2[i, 0] > ByteLineThreshold))
                    {
                        //Detect Shrimp
                        bDetectShrimpX = true;
                        break;
                    }
                }



                if (bDetectShrimpX)
                {
                    if (bOldClearLine)
                    {

                        Image<Gray, Byte> Sbin = S.ThresholdBinary(new Gray(ByteThreshold), new Gray(255));
                        //Image<Gray, Byte> S = grayFrame.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY_INV, 3, new Gray(4));

                        #region Find triangles and rectangles

                        List<Triangle2DF> triangleList = new List<Triangle2DF>();
                        List<MCvBox2D> boxList = new List<MCvBox2D>();


                        bool bDetectLarge = false;
                        bool bDetectSmall = false;
                        double OffsetLargeThreshold;
                        byte ByteOffsetLargeThreshold = 0;


                        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation


                            for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                            {
                                //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                if (contours.Area > 10) //only consider contours with area greater than 250
                                {
                                    boxList.Add(contours.GetMinAreaRect());
                                    if (boxList[(boxList.Count) - 1].size.Height > bytePixel10mm)
                                    {
                                        bDetectLarge = true;
                                        OffsetLargeThreshold = boxList[(boxList.Count) - 1].size.Height - bytePixel10mm;
                                        ByteOffsetLargeThreshold = Convert.ToByte(SlopeLargeThreshold * OffsetLargeThreshold);

                                        boxList.Clear();
                                        break;
                                    }
                                    if (boxList[(boxList.Count) - 1].size.Width > bytePixel10mm)
                                    {
                                        bDetectLarge = true;
                                        OffsetLargeThreshold = boxList[(boxList.Count) - 1].size.Width - bytePixel10mm;
                                        ByteOffsetLargeThreshold = Convert.ToByte(SlopeLargeThreshold * OffsetLargeThreshold);

                                        boxList.Clear();
                                        break;
                                    }
                                    if ((boxList[(boxList.Count) - 1].size.Height < bytePixel7mm) && (boxList[(boxList.Count) - 1].size.Height > bytePixel4mm))
                                    {
                                        bDetectSmall = true;
                                        boxList.Clear();
                                        break;
                                    }
                                    if ((boxList[(boxList.Count) - 1].size.Width < bytePixel7mm) && (boxList[(boxList.Count) - 1].size.Width > bytePixel4mm))
                                    {
                                        bDetectSmall = true;
                                        boxList.Clear();
                                        break;
                                    }
                                }
                            }


                        #endregion

                        if (bDetectLarge)
                        {
                            Sbin = S.ThresholdBinary(new Gray(ByteThresholdL + ByteOffsetLargeThreshold), new Gray(255));

                            #region Find triangles and rectangles Large Size

                            boxList = new List<MCvBox2D>();


                            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                                for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                                {
                                    //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                    if (contours.Area > 10) //only consider contours with area greater than 250
                                    {
                                        boxList.Add(contours.GetMinAreaRect());
                                    }
                                }

                            #endregion

                        }
                        if (bDetectSmall)
                        {
                            Sbin = S.ThresholdBinary(new Gray(ByteThresholdS), new Gray(255));

                            #region Find triangles and rectangles Large Size

                            boxList = new List<MCvBox2D>();


                            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                                for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                                {
                                    //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                    if (contours.Area > 10) //only consider contours with area greater than 250
                                    {
                                        boxList.Add(contours.GetMinAreaRect());
                                    }
                                }

                            #endregion

                        }

                        if (chkSaveImage.Checked)
                        {
                            strPathSave = subNewPath + "\\" + (intShrimpCounter + 1).ToString() + ".bmp";
                            m_processedImage.Save(strPathSave);
                        }


                        img1.ROI = new Rectangle(intBoxX1, intBoxY1, intBoxX2 - intBoxX1, intBoxY2 - intBoxY1);

                        #region draw triangles and rectangles
                        Image<Bgr, Byte> RectangleImage = img1.Copy();
                        //Image<Gray, Byte> RectangleImage = S.Copy();

                        //intShrimpCounter = 0;
                        bool bFoundShrimp = false;
                        foreach (MCvBox2D box in boxList)
                        //cannyFrame.Draw(box, new Gray(255), 1);
                        {

                            if ((box.size.Width > ByteBubble) || (box.size.Height > ByteBubble))
                            {

                                // x' = xcos@ + ysin@
                                // y' = -xsin@ + ycos@
                                double dblbH = box.size.Height; //y
                                double dblbW = box.size.Width;  //x
                                double dblbA = box.angle;
                                double angle = Math.PI * dblbA / 180.0;
                                double sinA = Math.Sin(angle);
                                double cosA = Math.Cos(angle);
                                double centerX = box.center.X;
                                double centerY = box.center.Y;
                                double dblShrimpLength;
                                double dblShrimpDiameter;
                                bool bRotatePicture = false;

                                if (dblbW > dblbH)
                                {
                                    dblShrimpLength = dblbW;
                                    dblShrimpDiameter = dblbH;
                                }
                                else
                                {
                                    dblShrimpLength = dblbH;
                                    dblShrimpDiameter = dblbW;
                                    bRotatePicture = true;
                                }


                                double dblShrimpLengthmm = dblPix2mm * dblShrimpLength;

                                if (dblShrimpLength > intMaxWidth)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.AliceBlue), 1);
                                }
                                else if (dblShrimpLength < intMinWidth)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                                }
                                else if (dblShrimpDiameter < intMinHeight)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                                }
                                else if (dblShrimpLength / dblShrimpDiameter < dblMaxRatio)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.BlueViolet), 1);
                                }
                                else
                                {
                                    // check leftframe
                                    bFoundShrimp = true;
                                    double dblWidth = dblbW * cosA - dblbH * sinA;
                                    double dblHeight = dblbH * cosA - dblbW * sinA;
                                    //if (dblHeight > dblWidth)
                                    //{
                                    //    double dblTemp;
                                    //    dblTemp = dblHeight;
                                    //    dblHeight = dblWidth;
                                    //    dblWidth = dblTemp;

                                    //}
                                    if (centerX < dblWidth / 2)
                                    {
                                        // check leftframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if ((centerX + (dblWidth) / 2) + 2 > (intBoxX2 - intBoxX1)) //+ 2 to prevent error
                                    {
                                        // check rightframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerY < (dblHeight) / 2)
                                    {
                                        // check topframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if ((centerY + (dblHeight) / 2) > (intBoxY2 - intBoxY1))
                                    {
                                        // check bottomframe
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (dblShrimpLengthmm > dblMaxWidth)
                                    {

                                    }
                                    else if (intBoxY2 - intBoxY1 - dblShrimpDiameter / 2 - centerY < 5)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerY - dblShrimpDiameter / 2 < 5)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                    }
                                    else if (centerX - dblShrimpLength > intX2)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                    }
                                    else if (centerX + dblShrimpLength < intX1)
                                    {
                                        RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                    }
                                    else
                                    {
                                        bOldClearLine = false;
                                        //RectangleImage.Draw(box, new Bgr(Color.Lime), 1);

                                        msg(dblShrimpLength.ToString("F"));
                                        //msg(box.angle.ToString("F") + "," + box.size.Height.ToString("F") + "," + box.size.Width.ToString("F"));

                                        intShrimpCounter++;



                                        int intULX = Convert.ToInt16(centerX - (dblbW * cosA - dblbH * sinA) / 2);
                                        int intULY = Convert.ToInt16(centerY - (dblbH * cosA - dblbW * sinA) / 2);
                                        Image<Bgr, Byte> imgA = RectangleImage.Copy();
                                        //Image<Gray, Byte> imgGA = imgA.Convert<Gray, Byte>();
                                        int intWidth = Convert.ToInt16(dblWidth);
                                        int intHeight = Convert.ToInt16(dblHeight);

                                        imgA.ROI = new Rectangle(intULX, intULY, intWidth, intHeight);
                                        Image<Bgr, Byte> grayFrameA = imgA.Copy();
                                        Image<Bgr, Byte> grayFrameB;
                                        if (bRotatePicture)
                                        {
                                            grayFrameB = grayFrameA.Rotate(90 - dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false); ;
                                        }
                                        else
                                        {
                                            grayFrameB = grayFrameA.Rotate(-dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false);
                                        }


                                        int intShrimpLength = Convert.ToInt16(dblShrimpLength + 1);
                                        int intShrimpDiameter = Convert.ToInt16(dblShrimpDiameter + 1);
                                        intULX = (grayFrameB.Size.Width - intShrimpLength) / 2;
                                        intULY = (grayFrameB.Size.Height - intShrimpDiameter) / 2;

                                        grayFrameB.ROI = new Rectangle(intULX, intULY, intShrimpLength + 5, intShrimpDiameter + 5);
                                        imageBox2.Image = grayFrameB;
                                        Image<Gray, Byte> grayFrameSection = grayFrameB.Convert<Gray, Byte>();

                                        //if ShrimpLength is more than 11 mm detect hair is true
                                        bool detectHair = false;
                                        dblShrimpHair_mm = Convert.ToDouble(txtShrimpHair_mm.Text);
                                        if (dblShrimpLengthmm > dblShrimpHair_mm)
                                        {
                                            detectHair = true;
                                        }


                                        byte[,] ArrayLSection;
                                        int index = 0;
                                        int indexOld = 0;
                                        double dblSumSection = 0;
                                        //add code of Divide Shrimp into section
                                        iSectionSpace = intShrimpLength / byteNumSection;
                                        double dblShrimpNumeratorLength = dblShrimpLength - iSectionSpace * (byteNumSection - 1);
                                        int intOffsetHairOld = 0;

                                        for (bSection = 0; bSection < byteNumSection + 1; bSection++)
                                        {
                                            if (bSection == 0)
                                                j = 5;                              //start scan first line
                                            else if (bSection == byteNumSection)
                                                j = intShrimpLength - 3;            //end scan
                                            else
                                                j = iSectionSpace * bSection + 2;   //middle scan

                                            //Point2D scanshrimp section
                                            LineSegment2D Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                            ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                            ArrayLSection = grayFrameSection.Sample(Lsection);
                                            int max = 0;
                                            int rowGreen = 4;
                                            int countMax = 0;
                                            int sumPixelValue = 0;
                                            byte pixMax = 0;
                                            byte pixMin = 255;
                                            byte bArrayLSection;
                                            int intPixmax = 0;
                                            int intAvgDetectHair = 0;
                                            int intOffsetHair = 0;

                                            for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                            {
                                                bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                                if (bArrayLSection > pixMax)
                                                {
                                                    pixMax = bArrayLSection;
                                                    intPixmax = i + rowGreen / 2;
                                                }

                                                if (bArrayLSection < pixMin)
                                                {
                                                    pixMin = bArrayLSection;
                                                }
                                                sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                            }
                                            intAvgDetectHair = (ArrayLSection[intPixmax + 2, 0] + ArrayLSection[intPixmax - 2, 0]) / 2;
                                            int Avg = sumPixelValue / (intShrimpDiameter - rowGreen + 1) + (pixMax - pixMin) / 8;
                                            if ((intAvgDetectHair < intHairBrightness) && detectHair)
                                            {
                                                //Detect shrimp hair 
                                                msg("Detect Shrimp hair");
                                                if (bSection == 0)
                                                {
                                                    while (intAvgDetectHair < intHairBrightness)
                                                    {
                                                        j = j + 2;

                                                        //Point2D scanshrimp section
                                                        Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                                        ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                                        ArrayLSection = grayFrameSection.Sample(Lsection);
                                                        pixMax = 0;
                                                        pixMin = 255;
                                                        for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                                        {
                                                            bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                                            if (bArrayLSection > pixMax)
                                                            {
                                                                pixMax = bArrayLSection;
                                                                intPixmax = i + rowGreen / 2;
                                                            }

                                                            if (bArrayLSection < pixMin)
                                                            {
                                                                pixMin = bArrayLSection;
                                                            }
                                                            sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                                        }
                                                        intAvgDetectHair = (ArrayLSection[intPixmax + 2, 0] + ArrayLSection[intPixmax - 2, 0]) / 2;

                                                    }
                                                    intOffsetHair = j;
                                                }

                                                if (bSection == byteNumSection)
                                                {
                                                    while (intAvgDetectHair < intHairBrightness)
                                                    {
                                                        j = j - 2;
                                                        intOffsetHair = intOffsetHair - 2;
                                                        //Point2D scanshrimp section
                                                        Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                                        ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                                        ArrayLSection = grayFrameSection.Sample(Lsection);
                                                        pixMax = 0;
                                                        pixMin = 255;
                                                        for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                                        {
                                                            bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                                            if (bArrayLSection > pixMax)
                                                            {
                                                                pixMax = bArrayLSection;
                                                                intPixmax = i + rowGreen / 2;
                                                            }

                                                            if (bArrayLSection < pixMin)
                                                            {
                                                                pixMin = bArrayLSection;
                                                            }
                                                            sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                                        }
                                                        intAvgDetectHair = (ArrayLSection[intPixmax + 2, 0] + ArrayLSection[intPixmax - 2, 0]) / 2;

                                                    }
                                                }


                                            }
                                            else
                                            {
                                                intOffsetHair = 0;
                                            }

                                            bool _recOnce = true;
                                            int[] indexDiameter = new int[200];

                                            for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                            {
                                                if (ArrayLSection[i + rowGreen / 2, 0] > Avg)
                                                {
                                                    //max = ArrayLSection[i + rowGreen/2, 0];
                                                    //record first index
                                                    if (_recOnce)
                                                    {
                                                        index = i;
                                                        _recOnce = false;
                                                    }
                                                    indexDiameter[countMax] = i;
                                                    countMax++;

                                                }
                                            }



                                            //index = index + rowGreen / 2 + countMax / 2;
                                            index = rowGreen / 2 + indexDiameter[countMax / 2];
                                            if (bSection > 0)
                                            {
                                                if (bSection == 1)
                                                {
                                                    LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace + intOffsetHairOld, indexOld), new Point(bSection * iSectionSpace, index));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                    LB = new LineSegment2D(new Point(intOffsetHairOld, 0), new Point(intOffsetHairOld, intShrimpDiameter));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                    dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + (dblShrimpNumeratorLength - intOffsetHairOld) * (dblShrimpNumeratorLength - intOffsetHairOld));
                                                }
                                                else if (bSection == byteNumSection)
                                                {
                                                    LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace + intOffsetHairOld, indexOld), new Point(bSection * iSectionSpace + intOffsetHair, index));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                    LB = new LineSegment2D(new Point(intShrimpLength + intOffsetHair, 0), new Point(intShrimpLength + intOffsetHair, intShrimpDiameter));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                    dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + (dblShrimpNumeratorLength + intOffsetHair) * (dblShrimpNumeratorLength + intOffsetHair));
                                                }
                                                else
                                                {
                                                    LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace, indexOld), new Point(bSection * iSectionSpace, index));
                                                    grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                    dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + iSectionSpace * iSectionSpace);
                                                }
                                                // pythagoras triangular

                                            }

                                            indexOld = index;
                                            intOffsetHairOld = intOffsetHair;

                                        }

                                        //msg("T: " + dblSumSection.ToString("F"));

                                        dblShrimpLengthmm = dblSumSection * dblPix2mm;
                                        dblShrimpLengthmm = dblShrimpLengthmm * dblSlope + dblOffset;
                                        ldblShrimpLength[intShrimpCounter - 1] = dblShrimpLengthmm;
                                        chkListBox.Items.Add(Convert.ToString(intShrimpCounter) + "   " + dblShrimpLengthmm.ToString("F"), true);
                                        lblShrimpLength.Text = dblShrimpLengthmm.ToString("F");


                                        if (bFirstFrame)
                                        {
                                            bFirstFrame = false;
                                            grayFrameC = grayFrameB.Copy();
                                            int iaddHeight = intSizeHeight - grayFrameC.Size.Height;
                                            //int iaddWidth = 250 - grayFrameC.Size.Width;
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateHorizontal(grayFrameD);
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                            imageBox3.Image = grayFrameC;
                                        }
                                        else
                                        {
                                            grayFrameD = grayFrameB.Copy();

                                            grayFrameB = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameD = grayFrameD.ConcateHorizontal(grayFrameB);

                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                            //add more space
                                            int iaddHeight = intSizeHeight - grayFrameD.Size.Height;
                                            if (iaddHeight < 0)
                                            {
                                                msg("Overheight");
                                                intShrimpCounter--;
                                                return;
                                            }
                                            grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                            grayFrameC = grayFrameC.ConcateVertical(grayFrameD);

                                            imageBox3.Image = grayFrameC;
                                            if ((intShrimpCounter % byteRow) == 0)
                                            {
                                                bFirstFrame = true;
                                                if (bFirstImageColumn)
                                                {
                                                    bFirstImageColumn = false;
                                                    grayFrameE = grayFrameC.Copy();
                                                }
                                                else
                                                {
                                                    Image<Bgr, byte> grayFrameF = grayFrameC.Copy();
                                                    grayFrameE = grayFrameE.ConcateHorizontal(grayFrameF);
                                                    imageBox3.Image = grayFrameE;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //imageBox1.Image = cannyFrame;
                        #endregion


                        //imageBox1.Image = Sbin;

                        //if (bFoundShrimp)
                        //{
                        imageBox1.Image = RectangleImage;
                        //imageBox1.Image = S;
                        //}

                        lblShrimpCounter.Text = intShrimpCounter.ToString();

                        //imageBox1.Image = img1;
                        //imageBox1.Image = S;

                        watch.Stop();
                        lblWatch.Text = watch.ElapsedMilliseconds.ToString() + " ms";
                    }
                    else
                    {
                        imageBox1.Image = img1;
                    }
                }
                else
                {
                    imageBox1.Image = img1;

                    if (intSkipFrame == 0)
                    {
                        bOldClearLine = true;
                    }

                    intCountSkipFrame++;
                    if (intCountSkipFrame == intSkipFrame)
                    {
                        bOldClearLine = true;
                        intCountSkipFrame = 0;
                    }
                    //watch.Stop();
                    //lblShrimpCounter.Text = "none";
                    //lblShrimpLength.Text = watch.ElapsedMilliseconds.ToString();
                }
            }

        }

        private int SearchSection(int Xpixel, int Ypixel)
        {
            //grayFrame
            //Find Coarse
            //divide all coners points into 4 quarters
            int i;
            int intColumn = 0;
            int intRow = 0;
            int intResult;

            float[] mmPoint = new float[3];

            //search coarse, 4 quarters
            for (i = 0; i < cornersX; i++)
            {
                if (Xpixel < ChessCorners[i].X)
                {
                    intColumn = i;
                    break;
                }
            }

            for (i = 0; i < cornersY; i++)
            {
                if (Ypixel < ChessCorners[i * cornersX + intColumn].Y)
                {
                    intRow = i;
                    break;
                }
            }
            intResult = intRow * cornersX + intColumn;
            return intResult;

        }

        private void UpdateStatusBar()
        {

            String statusString;

            statusString = String.Format("Image size: {0} x {1}", m_rawImage.Height, m_rawImage.Width);

            //toolStripStatusLabelImageSize.Text = statusString;

            try
            {
                //statusString = String.Format(
                //"Requested frame rate: {0}Hz",                
                //m_camera.GetProperty(PropertyType.FrameRate).absValue);
            }
            catch (SpinnakerException ex)
            {
                statusString = "Requested frame rate: 0.00Hz";
            }


            toolStripStatusLabelFrameRate.Text = statusString;

            //TimeStamp timestamp;
            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            

            lock (this)
            {
                Timestamp = (long)m_rawImage.TimeStamp;
                //timestamp = m_rawImage.timeStamp;
            }

            statusString = Timestamp.ToString();
            //timestamp.cycleOffset);
            toolStripStatusLabelTimestamp.Text = statusString;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Hide();
            LoadXML();
            DoAOI();

            if (rdoCamera.Checked)
            {
                //get image from camera
                ManagedSystem system = new ManagedSystem();
                ManagedCameraList camList = system.GetCameras();


                //CameraSelectionDialog camSlnDlg = new CameraSelectionDialog();
                //bool retVal = camSlnDlg.ShowModal();
                
         

                if (camList.Count == 1)
                {
                    m_camera = camList[0];
                    try
                    {
                        m_camera.Init();

                        // Retrieve GenICam nodemap
                        INodeMap nodeMap = m_camera.GetNodeMap();
                        // Retrieve enumeration node from nodemap
                        IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");

                        // Retrieve entry node from enumeration node
                        IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                        iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;
                        //
                        // Begin acquiring images
                        //
                        // *** NOTES ***
                        // What happens when the camera begins acquiring images depends 
                        // on which acquisition mode has been set. Single frame captures 
                        // only a single image, multi frame catures a set number of 
                        // images, and continuous captures a continuous stream of images.
                        // Because the example calls for the retrieval of 10 images, 
                        // continuous mode has been set for the example.
                        // 
                        // *** LATER ***
                        // Image acquisition must be ended when no more images are needed.
                        //
                        m_camera.BeginAcquisition();

                        m_grabImages = true;

                        StartGrabLoop();


                        LoadPointCorners();
                        DoAOI();
                    }
                    catch (SpinnakerException ex)
                    {
                        Debug.WriteLine("Failed to load form successfully: " + ex.Message);
                        Environment.ExitCode = -1;
                        Application.Exit();
                        return;
                    }

                    toolStripButtonStop.Enabled = true;
                    toolStripButtonStart.Enabled = false;
                    toolStripCalibrate.Enabled = false;
                    toolStripViewCorners.Enabled = false;
                    btnStart.Enabled = false;
                    btnStop.Enabled = false;
                    lblOK.Hide();

                }
                else
                {
                    Environment.ExitCode = -1;
                    Application.Exit();
                    return;
                }
            }
            else
            {
                //get image from file

            }



            bool exists = System.IO.Directory.Exists(@"c:\Pixel2");

            if (!exists)
            {
                // Create the subfolder
                System.IO.Directory.CreateDirectory(@"c:\Pixel2");
            }
            //imageBox3.BackColor = Color.FromArgb(255, 60, 60, 60);
            imageBox3.Visible = false;
            lblOK.Visible = false;
            Show();
            if (WindowState == FormWindowState.Normal)
            {

                WindowState = FormWindowState.Maximized;

            }

        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                toolStripButtonStop_Click(sender, e);
                //m_camera.Disconnect();
                m_camera.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                // Nothing to do here
            }
            catch (NullReferenceException ex)
            {
                // Nothing to do here
            }
        }

        private void StartGrabLoop()
        {
            m_grabThread = new BackgroundWorker();
            m_grabThread.ProgressChanged += new ProgressChangedEventHandler(UpdateUI);
            m_grabThread.DoWork += new DoWorkEventHandler(GrabLoop);
            m_grabThread.WorkerReportsProgress = true;
            m_grabThread.RunWorkerAsync();
        }

        private void StartGrabLoopShrimp()
        {
            m_grabThread = new BackgroundWorker();
            m_grabThread.ProgressChanged += new ProgressChangedEventHandler(UpdateUIshrimp);
            m_grabThread.DoWork += new DoWorkEventHandler(GrabLoopShrimp);
            m_grabThread.WorkerReportsProgress = true;
            m_grabThread.RunWorkerAsync();
        }

        private void GrabLoop(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (m_grabImages)
            {
                try
                {
                    //m_camera.RetrieveBuffer(m_rawImage);
                    //
                    // Retrieve next received image
                    //
                    // *** NOTES ***
                    // Capturing an image houses images on the camera buffer. 
                    // Trying to capture an image that does not exist will 
                    // hang the camera.
                    //
                    // Using-statements help ensure that images are released.
                    // If too many images remain unreleased, the buffer will
                    // fill, causing the camera to hang. Images can also be
                    // released manually by calling Release().
                    // 
                    using (IManagedImage rawImage = m_camera.GetNextImage())
                    {
                        if (rawImage.IsIncomplete)
                        {
                            //Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                        }
                        else
                        {
                            //
                            // Print image information; width and height 
                            // recorded in pixels
                            //
                            // *** NOTES ***
                            // Images have quite a bit of available metadata 
                            // including CRC, image status, and offset 
                            // values to name a few.
                            //
                            uint width = rawImage.Width;
                            uint height = rawImage.Height;

                            //Console.WriteLine("Grabbed image {0}, width = {1}, height = {1}", imageCnt, width, height);

                            //
                            // Convert image to mono 8
                            //
                            // *** NOTES ***
                            // Images can be converted between pixel formats
                            // by using the appropriate enumeration value.
                            // Unlike the original image, the converted one 
                            // does not need to be released as it does not 
                            // affect the camera buffer.
                            // 
                            // Using statements are a great way to ensure code
                            // stays clean and avoids memory leaks.
                            // leaks.
                            //
                            //m_processedImage = rawImage.Convert(PixelFormatEnums.BGR8);                          

                            m_processedImage.Dispose();
                            
                            m_processedImage = rawImage.Convert(PixelFormatEnums.Mono8);
                            intNewImage = 1;
                        }
                    }
                }
                catch (SpinnakerException ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                    continue;
                }

                //lock (this)
               // {
               //     m_rawImage.Convert(convertedImage,)
               //     m_rawImage.Convert(PixelFormatEnums.Mono8, m_processedImage);
                    
                    ////m_rawImage.Convert(PixelFormat.PixelFormatBgr, m_processedImage);
               // }

                worker.ReportProgress(0);
            }

            m_grabThreadExited.Set();
        }

        private void GrabLoopShrimp(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (m_grabShrimp)
            {
                try
                {
                    //m_camera.RetrieveBuffer(m_rawImage);
                    //
                    // Retrieve next received image
                    //
                    // *** NOTES ***
                    // Capturing an image houses images on the camera buffer. 
                    // Trying to capture an image that does not exist will 
                    // hang the camera.
                    //
                    // Using-statements help ensure that images are released.
                    // If too many images remain unreleased, the buffer will
                    // fill, causing the camera to hang. Images can also be
                    // released manually by calling Release().
                    // 
                    using (IManagedImage rawImage = m_camera.GetNextImage())
                    {
                        if (rawImage.IsIncomplete)
                        {
                            //Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                        }
                        else
                        {
                            //
                            // Print image information; width and height 
                            // recorded in pixels
                            //
                            // *** NOTES ***
                            // Images have quite a bit of available metadata 
                            // including CRC, image status, and offset 
                            // values to name a few.
                            //
                            uint width = rawImage.Width;
                            uint height = rawImage.Height;

                            //Console.WriteLine("Grabbed image {0}, width = {1}, height = {1}", imageCnt, width, height);

                            //
                            // Convert image to mono 8
                            //
                            // *** NOTES ***
                            // Images can be converted between pixel formats
                            // by using the appropriate enumeration value.
                            // Unlike the original image, the converted one 
                            // does not need to be released as it does not 
                            // affect the camera buffer.
                            // 
                            // Using statements are a great way to ensure code
                            // stays clean and avoids memory leaks.
                            // leaks.
                            //
                            m_processedImage.Dispose();
                            m_processedImage = rawImage.Convert(PixelFormatEnums.Mono8);

                        }

                    }
                }
                catch (SpinnakerException ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                    continue;
                }

                //lock (this)
                // {
                //     m_rawImage.Convert(convertedImage,)
                //     m_rawImage.Convert(PixelFormatEnums.Mono8, m_processedImage);

                ////m_rawImage.Convert(PixelFormat.PixelFormatBgr, m_processedImage);

                // }

                worker.ReportProgress(0);
            }
            m_grabThreadExited.Set();
        }

        private void msg(string s)
        {
            //DateTime Current = DateTime.Now;
            DateTime Current = DateTime.Now;
            txtMessage.AppendText(Current.ToString("T") + " : " + s + "\r\n");
        }

        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {

            //m_camera.StartCapture();
            m_camera.BeginAcquisition();
            m_grabImages = true;
            m_grabShrimp = false;

            StartGrabLoop();

            toolStripButtonStop.Enabled = true;

            btnStop.Enabled = false;
            toolStripButtonStart.Enabled = false;
            toolStripCalibrate.Enabled = false;
            toolStripViewCorners.Enabled = false;
            btnStart.Enabled = false;

        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            m_grabImages = false;
            m_grabShrimp = false;

            try
            {
                //m_camera.StopCapture();
                m_camera.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Failed to stop camera: " + ex.Message);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Camera is null");
            }

            toolStripButtonStop.Enabled = false;
            btnStop.Enabled = false;

            toolStripButtonStart.Enabled = true;
            toolStripCalibrate.Enabled = true;
            toolStripViewCorners.Enabled = true;
            btnStart.Enabled = true;
        }

        private void toolStripButtonCameraControl_Click(object sender, EventArgs e)
        {
            /*
            if (m_camCtlDlg.IsVisible())
            {
                m_camCtlDlg.Hide();
                toolStripButtonCameraControl.Checked = false;
            }
            else
            {
                m_camCtlDlg.Show();
                toolStripButtonCameraControl.Checked = true;
            }
            */
        }

        /*
        private void OnNewCameraClick(object sender, EventArgs e)
        {
            if (m_grabImages == true)
            {
                toolStripButtonStop_Click(sender, e);
                m_camCtlDlg.Hide();
                m_camCtlDlg.Disconnect();
                m_camera.Disconnect();
            }

            Form1_Load(sender, e);
        }         */

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveXML();
            DoAOI();
        }

        private void SaveXML()
        {
            // Get the current directory.
            string path = Directory.GetCurrentDirectory();
            XmlTextWriter textWriter = new XmlTextWriter(path + "\\initial.xml", null);

            textWriter.WriteStartDocument();
            // Write comments
            textWriter.WriteComment("XmlText to keep initial configuration");
            textWriter.WriteComment("initial.xml in root dir");
            // Write first element
            textWriter.WriteStartElement("Camera");
            textWriter.WriteStartElement("r", "RECORD", "urn:record");

            // Write one more element
            textWriter.WriteStartElement("Y1", "");
            textWriter.WriteString(txtY1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Y2", "");
            textWriter.WriteString(txtY2.Text);
            textWriter.WriteEndElement();

            // Write next element

            textWriter.WriteStartElement("S_LX1", "");
            textWriter.WriteString(txtS_LX1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("M_LX1", "");
            textWriter.WriteString(txtM_LX1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("L_LX1", "");
            textWriter.WriteString(txtL_LX1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("C_LX1", "");
            textWriter.WriteString(txtC_LX1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("S_LX2", "");
            textWriter.WriteString(txtS_LX2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("M_LX2", "");
            textWriter.WriteString(txtM_LX2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("L_LX2", "");
            textWriter.WriteString(txtL_LX2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("C_LX2", "");
            textWriter.WriteString(txtC_LX2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("SThreshold", "");
            textWriter.WriteString(txtSThreshold.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MThreshold", "");
            textWriter.WriteString(txtMThreshold.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("LThreshold", "");
            textWriter.WriteString(txtLThreshold.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("CThreshold", "");
            textWriter.WriteString(txtCThreshold.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("LineThreshold", "");
            textWriter.WriteString(txtLineThres.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Pix2mm", "");
            textWriter.WriteString(txtPix2mm.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Slope", "");
            textWriter.WriteString(txtSlope.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Offset", "");
            textWriter.WriteString(txtOffset.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("OffsetHuman", "");
            textWriter.WriteString(txtOffsetHuman.Text);
            textWriter.WriteEndElement();
            
            textWriter.WriteStartElement("SkipFrame", "");
            textWriter.WriteString(txtSkipFrame.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("ShrimpHair_mm", "");
            textWriter.WriteString(txtShrimpHair_mm.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("HairBrightness", "");
            textWriter.WriteString(txtHairBrightness.Text);
            textWriter.WriteEndElement();

            
            textWriter.WriteStartElement("SerialNo", "");
            textWriter.WriteString(txtSerialNo.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Password", "");
            textWriter.WriteString(txtPasswd.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("BoxX1", "");
            textWriter.WriteString(txtBoxX1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("BoxX2", "");
            textWriter.WriteString(txtBoxX2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("BoxY1", "");
            textWriter.WriteString(txtBoxY1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("BoxY2", "");
            textWriter.WriteString(txtBoxY2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("CoarsePixel", "");
            textWriter.WriteString(txtCoarsePixel.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("FinePixel", "");
            textWriter.WriteString(txtFinePixel.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("BubbleSize", "");
            textWriter.WriteString(txtBubble.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("NumberOfSectionS", "");
            textWriter.WriteString(txtSectionS.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("SWidth", "");
            textWriter.WriteString(txtSWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("SHeight", "");
            textWriter.WriteString(txtSHeight.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("SRow", "");
            textWriter.WriteString(txtSRow.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("S_MaxRatio", "");
            textWriter.WriteString(txtS_MaxRatio.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("NumberOfSectionM", "");
            textWriter.WriteString(txtSectionM.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MWidth", "");
            textWriter.WriteString(txtMWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MHeight", "");
            textWriter.WriteString(txtMHeight.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MRow", "");
            textWriter.WriteString(txtMRow.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("M_MaxRatio", "");
            textWriter.WriteString(txtM_MaxRatio.Text);
            textWriter.WriteEndElement();


            textWriter.WriteStartElement("NumberOfSectionL", "");
            textWriter.WriteString(txtSectionL.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("LWidth", "");
            textWriter.WriteString(txtLWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("LHeight", "");
            textWriter.WriteString(txtLHeight.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("LRow", "");
            textWriter.WriteString(txtLRow.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("L_MaxRatio", "");
            textWriter.WriteString(txtL_MaxRatio.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("L_Slope", "");
            textWriter.WriteString(txtL_SlopeThreshold.Text);
            textWriter.WriteEndElement();



            textWriter.WriteStartElement("NumberOfSectionC", "");
            textWriter.WriteString(txtSectionC.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("CWidth", "");
            textWriter.WriteString(txtCWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("CHeight", "");
            textWriter.WriteString(txtCHeight.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("CRow", "");
            textWriter.WriteString(txtCRow.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("C_MaxRatio", "");
            textWriter.WriteString(txtC_MaxRatio.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("ShrimpSize", "");
            if (rdoSSize.Checked)
            {
                textWriter.WriteString("S"); //source from camera
            }
            else if (rdoMSize.Checked)
            {
                textWriter.WriteString("M");
            }
            else if (rdoLSize.Checked)
            {
                textWriter.WriteString("L");
            }
            else if (rdoAllSize.Checked)
            {
                textWriter.WriteString("A");
            }
            else
            {
                textWriter.WriteString("C");// C is calibration
            }

            textWriter.WriteEndElement();

            textWriter.WriteStartElement("Restart", "");
            if (chkRestart.Checked)
            {
                textWriter.WriteString("1"); //source from camera
            }
            else
            {
                textWriter.WriteString("0"); //source from picture file
            }
            textWriter.WriteEndElement();

            
            textWriter.WriteStartElement("ImageSource", "");
            if (rdoCamera.Checked)
            {
                textWriter.WriteString("1"); //source from camera
            }
            else
            {
                textWriter.WriteString("0"); //source from picture file
            }
            textWriter.WriteEndElement();


            textWriter.WriteStartElement("ImageSourceFile", "");
            textWriter.WriteString(txtImagePath.Text); //source from camera
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MaxWidth", "");
            textWriter.WriteString(txtMaxWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MinWidth", "");
            textWriter.WriteString(txtMinWidth.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MinHeight", "");
            textWriter.WriteString(txtMinHeight.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("MaxRatio", "");
            textWriter.WriteString(txtS_MaxRatio.Text);
            textWriter.WriteEndElement();


            textWriter.WriteStartElement("SampleSize", "");
            textWriter.WriteString(txtSampleSize.Text);
            textWriter.WriteEndElement();


            textWriter.WriteStartElement("BackColor", "");
            textWriter.WriteString(txtBackColor.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("TL1", "");
            textWriter.WriteString(txtTL1.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("TL2", "");
            textWriter.WriteString(txtTL2.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("TL3", "");
            textWriter.WriteString(txtTL3.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("TL4", "");
            textWriter.WriteString(txtTL4.Text);
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("TL5", "");
            textWriter.WriteString(txtTL5.Text);
            textWriter.WriteEndElement();

            // WriteChars
            char[] ch = new char[3];
            ch[0] = 'a';
            ch[1] = 'r';
            ch[2] = 'c';
            textWriter.WriteStartElement("Char");
            textWriter.WriteChars(ch, 0, ch.Length);
            textWriter.WriteEndElement();
            // Ends the document.
            textWriter.WriteEndDocument();
            // close writer
            textWriter.Close();

        }

        private void toolStripCalibrate_Click(object sender, EventArgs e)
        {
            /*
            int i, j;
            txtMessage.Clear();
            msg("Detecting Chessboard");
            //this.Refresh();
            m_camera.StartCapture();

            // Retrieve an image
            try
            {
                m_camera.RetrieveBuffer(m_rawImage);
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                //continue;
            }

            lock (this)
            {
                m_rawImage.Convert(PixelFormat.PixelFormatBgr, m_processedImage);
            }

            // Get the Bitmap object. Bitmaps are only valid if the
            // pixel format of the ManagedImage is RGB or RGBU.
            System.Drawing.Bitmap Processed_Bitmap = m_processedImage.bitmap;

            try
            {
                m_camera.StopCapture();
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Failed to stop camera: " + ex.Message);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Camera is null");
            }

            //Image<Bgr, Byte> frame = new Image<Bgr, Byte>(Processed_Bitmap);
            Image<Gray, Byte> grayFrame = new Image<Gray, Byte>(Processed_Bitmap);
            Size patternSize = new Size(cornersX, cornersY);

            corners = CameraCalibration.FindChessboardCorners(grayFrame, patternSize,
                   Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH | Emgu.CV.CvEnum.CALIB_CB_TYPE.NORMALIZE_IMAGE | Emgu.CV.CvEnum.CALIB_CB_TYPE.FILTER_QUADS);


            //corners = CameraCalibration.FindChessboardCorners(grayFrame, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.DEFAULT);

            if (corners != null)
            {
                if (corners[0].Y < corners[cornersX].Y)
                {
                    msg("Detect Z PATTERN OK");
                    CameraCalibration.DrawChessboardCorners(grayFrame, patternSize, corners);


                    //Save Position
                    // Get the current directory.
                    string path = Directory.GetCurrentDirectory();
                    XmlTextWriter textWriter = new XmlTextWriter(path + "\\Calibrate.xml", null);

                    textWriter.WriteStartDocument();
                    // Write comments
                    textWriter.WriteComment("XmlText to keep initial configuration");
                    textWriter.WriteComment("initial.xml in root dir");
                    // Write first element
                    textWriter.WriteStartElement("Calibrate");
                    textWriter.WriteStartElement("r", "RECORD", "urn:record");
                    i = cornersX * cornersY;
                    j = 0;
                    for (j = 0; j < i; j++)  // <-- This is new
                    {
                        textWriter.WriteStartElement("PointXY"); // <-- Write employee element
                        textWriter.WriteString(corners[j].X.ToString() + "," + corners[j].Y.ToString());
                        textWriter.WriteEndElement();
                    }

                    // Ends the document.
                    textWriter.WriteEndDocument();
                    // close writer
                    textWriter.Close();

                    LoadPointCorners();
                    //bDrawFirstCorner = true;
                }
                else
                {
                    msg("No, Detect N PATTERN");
                    CameraCalibration.DrawChessboardCorners(grayFrame, patternSize, corners);
                }
            }
            else
            {
                msg("Calibration Failed");
            }
            imageBox1.Image = grayFrame;
            imageBox1.Invalidate();
            */
        }

        private void LoadXML()
        {
            string path = Directory.GetCurrentDirectory();
            XmlTextReader textReader = new XmlTextReader(path + "\\initial.xml");
            textReader.Read();

            // If the node has value
            while (textReader.Read())
            {
                switch (textReader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (textReader.Name)
                        {
                            case "Y1":
                                txtY1.Text = textReader.ReadString();
                                break;

                            case "Y2":
                                txtY2.Text = textReader.ReadString();
                                break;

                            case "S_LX1":
                                txtS_LX1.Text = textReader.ReadString();
                                break;

                            case "M_LX1":
                                txtM_LX1.Text = textReader.ReadString();
                                break;

                            case "L_LX1":
                                txtL_LX1.Text = textReader.ReadString();
                                break;

                            case "C_LX1":
                                txtC_LX1.Text = textReader.ReadString();
                                break;


                            case "SThreshold":
                                txtSThreshold.Text = textReader.ReadString();
                                break;

                            case "MThreshold":
                                txtMThreshold.Text = textReader.ReadString();
                                break;

                            case "LThreshold":
                                txtLThreshold.Text = textReader.ReadString();
                                break;

                            case "CThreshold":
                                txtCThreshold.Text = textReader.ReadString();
                                break;



                            case "LineThreshold":
                                txtLineThres.Text = textReader.ReadString();
                                break;

                            case "CoarsePixel":
                                txtCoarsePixel.Text = textReader.ReadString();
                                break;

                            case "FinePixel":
                                txtFinePixel.Text = textReader.ReadString();
                                break;

                            case "BoxX1":
                                txtBoxX1.Text = textReader.ReadString();
                                break;

                            case "BoxX2":
                                txtBoxX2.Text = textReader.ReadString();
                                break;

                            case "BoxY1":
                                txtBoxY1.Text = textReader.ReadString();
                                break;

                            case "BoxY2":
                                txtBoxY2.Text = textReader.ReadString();
                                break;

                            case "NumberOfSectionS":
                                txtSectionS.Text = textReader.ReadString();
                                break;

                            case "NumberOfSectionM":
                                txtSectionM.Text = textReader.ReadString();
                                break;

                            case "NumberOfSectionL":
                                txtSectionL.Text = textReader.ReadString();
                                break;

                            case "NumberOfSectionC":
                                txtSectionC.Text = textReader.ReadString();
                                break;

                            case "BubbleSize":
                                txtBubble.Text = textReader.ReadString();
                                break;


                            case "Pix2mm":
                                txtPix2mm.Text = textReader.ReadString();
                                break;

                            case "Slope":
                                txtSlope.Text = textReader.ReadString();
                                break;

                            case "Offset":
                                txtOffset.Text = textReader.ReadString();
                                break;

                            case "OffsetHuman":
                                txtOffsetHuman.Text = textReader.ReadString();
                                break;

                            case "SkipFrame":
                                txtSkipFrame.Text = textReader.ReadString();
                                break;

                            case "ShrimpHair_mm":
                                txtShrimpHair_mm.Text = textReader.ReadString();
                                break;

                            case "HairBrightness":
                                txtHairBrightness.Text = textReader.ReadString();
                                break;

                            case "SerialNo":
                                txtSerialNo.Text = textReader.ReadString();
                                break;

                            case "Password":
                                txtPasswd.Text = textReader.ReadString();
                                break;



                            case "ImageSource":
                                if (textReader.ReadString() == "1")
                                {
                                    rdoCamera.Checked = true;
                                    rdoSimImage.Checked = false;
                                }
                                else
                                {
                                    rdoCamera.Checked = false;
                                    rdoSimImage.Checked = true;
                                }
                                break;

                            case "Restart":
                                if (textReader.ReadString() == "1")
                                {
                                    chkRestart.Checked = true;
                                }
                                else
                                {
                                    chkRestart.Checked = false;
                                }
                                break;



                            case "ShrimpSize":
                                switch (textReader.ReadString())
                                {
                                    case "S":
                                        rdoSSize.Checked = true;
                                        break;
                                    case "M":
                                        rdoMSize.Checked = true;
                                        break;
                                    case "L":
                                        rdoLSize.Checked = true;
                                        break;
                                    case "A":
                                        rdoAllSize.Checked = true;
                                        break;

                                }
                                break;

                            case "S_LX2":
                                txtS_LX2.Text = textReader.ReadString();
                                break;

                            case "M_LX2":
                                txtM_LX2.Text = textReader.ReadString();
                                break;

                            case "L_LX2":
                                txtL_LX2.Text = textReader.ReadString();
                                break;

                            case "C_LX2":
                                txtC_LX2.Text = textReader.ReadString();
                                break;


                            case "S_MaxRatio":
                                txtS_MaxRatio.Text = textReader.ReadString();
                                break;

                            case "M_MaxRatio":
                                txtM_MaxRatio.Text = textReader.ReadString();
                                break;

                            case "L_MaxRatio":
                                txtL_MaxRatio.Text = textReader.ReadString();
                                break;

                            case "L_Slope":
                                txtL_SlopeThreshold.Text = textReader.ReadString();
                                break;

                            case "C_MaxRatio":
                                txtC_MaxRatio.Text = textReader.ReadString();
                                break;

                            case "ImageSourceFile":
                                txtImagePath.Text = textReader.ReadString();
                                break;

                            case "SampleSize":
                                txtSampleSize.Text = textReader.ReadString();
                                break;

                            case "MaxWidth":
                                txtMaxWidth.Text = textReader.ReadString();
                                break;

                            case "MinWidth":
                                txtMinWidth.Text = textReader.ReadString();
                                break;

                            case "MinHeight":
                                txtMinHeight.Text = textReader.ReadString();
                                break;

                            case "MaxRatio":
                                txtS_MaxRatio.Text = textReader.ReadString();
                                break;

                            case "SWidth":
                                txtSWidth.Text = textReader.ReadString();
                                break;

                            case "SHeight":
                                txtSHeight.Text = textReader.ReadString();
                                break;

                            case "SRow":
                                txtSRow.Text = textReader.ReadString();
                                break;

                            case "MWidth":
                                txtMWidth.Text = textReader.ReadString();
                                break;

                            case "MHeight":
                                txtMHeight.Text = textReader.ReadString();
                                break;

                            case "MRow":
                                txtMRow.Text = textReader.ReadString();
                                break;

                            case "LWidth":
                                txtLWidth.Text = textReader.ReadString();
                                break;

                            case "LHeight":
                                txtLHeight.Text = textReader.ReadString();
                                break;

                            case "LRow":
                                txtLRow.Text = textReader.ReadString();
                                break;

                            case "CWidth":
                                txtCWidth.Text = textReader.ReadString();
                                break;

                            case "CHeight":
                                txtCHeight.Text = textReader.ReadString();
                                break;

                            case "CRow":
                                txtCRow.Text = textReader.ReadString();
                                break;

                            case "BackColor":
                                txtBackColor.Text = textReader.ReadString();
                                break;

                            case "TL1":
                                txtTL1.Text = textReader.ReadString();
                                break;

                            case "TL2":
                                txtTL2.Text = textReader.ReadString();
                                break;

                            case "TL3":
                                txtTL3.Text = textReader.ReadString();
                                break;

                            case "TL4":
                                txtTL4.Text = textReader.ReadString();
                                break;

                            case "TL5":
                                txtTL5.Text = textReader.ReadString();
                                break;
                        }
                        break;
                }
            }
            textReader.Close();
            DoAOI();
        }

        private void LoadPointCorners()
        {
            int i;
            string path = Directory.GetCurrentDirectory();
            XmlTextReader textReader = new XmlTextReader(path + "\\initial.xml");

            //load Corner PointF
            textReader = new XmlTextReader(path + "\\Calibrate.xml");
            textReader.Read();
            i = 0;
            string strTemp;

            while (textReader.Read())
            {
                switch (textReader.NodeType)
                {
                    case XmlNodeType.Element:

                        if (textReader.Name == "PointXY")
                        {
                            strTemp = textReader.ReadString();
                            string[] split = strTemp.Split(new Char[] { ',' });
                            ChessCorners[i].X = Convert.ToSingle(split[0]);
                            ChessCorners[i].Y = Convert.ToSingle(split[1]);
                            i++;
                        }
                        break;
                }
            }
            textReader.Close();
        }

        private void toolStripViewCorners_Click(object sender, EventArgs e)
        {
            /*
             * txtMessage.Clear();
            msg("Draw Corners");

            m_camera.StartCapture();
            // Retrieve an image
            try
            {
                m_camera.RetrieveBuffer(m_rawImage);
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                //continue;
            }

            lock (this)
            {
                m_rawImage.Convert(PixelFormat.PixelFormatBgr, m_processedImage);
            }

            // Get the Bitmap object. Bitmaps are only valid if the
            // pixel format of the ManagedImage is RGB or RGBU.
            System.Drawing.Bitmap Processed_Bitmap = m_processedImage.bitmap;
            Image<Gray, Byte> grayFrame = new Image<Gray, Byte>(Processed_Bitmap);

            try
            {
                m_camera.StopCapture();
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Failed to stop camera: " + ex.Message);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Camera is null");
            }


            Size patternSize = new Size(cornersX, cornersY);
            CameraCalibration.DrawChessboardCorners(grayFrame, patternSize, ChessCorners);
            imageBox1.Image = grayFrame;
            imageBox1.Invalidate();
            */
        }

        private void imageBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen greenPen = new Pen(Color.Lime, 1);
            Pen greenPen2 = new Pen(Color.Lime, 2);
            Pen redPen = new Pen(Color.Red, 2);
            Pen MagentaPen2 = new Pen(Color.Magenta, 2);
            Pen whitePen = new Pen(Color.White, 1);
            Pen whitePen2 = new Pen(Color.White, 2);
            Pen MagentaPen = new Pen(Color.Magenta, 1);
            int iSection;


            if (bDrawLine)
            {
                // Create coordinates of points that define line.
                float x1 = intX1;
                float y1 = intY1;
                float x2 = intX1;
                float y2 = intY2;
                //// Draw line1 to screen.
                g.DrawLine(greenPen, x1, y1, x2, y2);

                x1 = intX2;
                y1 = intY1;
                x2 = intX2;
                y2 = intY2;
                //// Draw line2 to screen.
                g.DrawLine(whitePen, x1, y1, x2, y2);

                g.DrawLine(MagentaPen, intBoxX1, intBoxY1, intBoxX1, intBoxY2);
                g.DrawLine(MagentaPen, intBoxX1, intBoxY1, intBoxX2, intBoxY1);
                g.DrawLine(MagentaPen, intBoxX2, intBoxY1, intBoxX2, intBoxY2);
                g.DrawLine(MagentaPen, intBoxX1, intBoxY2, intBoxX2, intBoxY2);

            }

            if (bDrawShrimpCircleUp)
            {
                if ((intLineMaxY - intLineMinY) > (intLineMaxX - intLineMinX))
                //shrimp in Vertical position, therefore scan X
                {
                    //Divide Shrimp into section
                    iSectionSpace = (intLineMaxY - intLineMinY) / byteNumSection;
                    for (iSection = 0; iSection < byteNumSection; iSection++)
                    {
                        g.DrawLine(greenPen2, iMedianDot[iSection], intLineMinY + iSectionSpace * iSection, iMedianDot[iSection + 1], intLineMinY + iSectionSpace * (iSection + 1));
                    }
                    g.DrawLine(greenPen2, intLineMinX, intLineMinY, intLineMaxX, intLineMinY);
                    g.DrawLine(greenPen2, intLineMinX, intLineMaxY, intLineMaxX, intLineMaxY);
                }
                else //shrimp in Horizontal Position
                {
                    //Divide Shrimp into section
                    iSectionSpace = (intLineMaxX - intLineMinX) / byteNumSection;
                    for (iSection = 0; iSection < byteNumSection; iSection++)
                    {
                        g.DrawLine(greenPen2, intLineMinX + iSectionSpace * iSection, iMedianDot[iSection], intLineMinX + iSectionSpace * (iSection + 1), iMedianDot[iSection + 1]);
                    }

                    g.DrawLine(greenPen2, intLineMinX, intLineMinY, intLineMinX, intLineMaxY);
                    g.DrawLine(greenPen2, intLineMaxX, intLineMinY, intLineMaxX, intLineMaxY);

                }
            }

            int i;
            for (i = 0; i < EdgeCount - 1; i++)
            {
                g.DrawLine(whitePen, intEdgeX[i], intEdgeY[i], intEdgeX[i + 1], intEdgeY[i + 1]);
            }

            if (bDrawShrimpCircleDown)
            {

                if ((intLineMaxY - intLineMinY) > (intLineMaxX - intLineMinX))
                //shrimp in Vertical position, therefore scan X
                {
                    //Divide Shrimp into section
                    iSectionSpace = (intLineMaxY - intLineMinY) / byteNumSection;
                    for (iSection = 0; iSection < byteNumSection; iSection++)
                    {
                        g.DrawLine(greenPen2, iMedianDot[iSection], intLineMinY + iSectionSpace * iSection, iMedianDot[iSection + 1], intLineMinY + iSectionSpace * (iSection + 1));
                    }
                    g.DrawLine(whitePen2, intLineMinX, intLineMinY, intLineMaxX, intLineMinY);
                    g.DrawLine(whitePen2, intLineMinX, intLineMaxY, intLineMaxX, intLineMaxY);
                }
                else //shrimp in Horizontal Position
                {
                    iSectionSpace = (intLineMaxX - intLineMinX) / byteNumSection;
                    for (iSection = 0; iSection < byteNumSection; iSection++)
                    {
                        g.DrawLine(greenPen2, intLineMinX + iSectionSpace * iSection, iMedianDot[iSection], intLineMinX + iSectionSpace * (iSection + 1), iMedianDot[iSection + 1]);
                    }

                    g.DrawLine(whitePen2, intLineMinX, intLineMinY, intLineMinX, intLineMaxY);
                    g.DrawLine(whitePen2, intLineMaxX, intLineMinY, intLineMaxX, intLineMaxY);
                }
            }
        }

        private void cboxViewArea_CheckStateChanged(object sender, EventArgs e)
        {
            if (cboxViewArea.Checked)
            {
                DoAOI();
            }
            else
            {
                bDrawLine = false;
                DoAOI();
            }
            SaveXML();
        }


        private void txtBoxX1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }


        private void txtBoxY1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtBoxX2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtBoxY2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtX1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtX2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtLY1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }

        private void txtLY2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoAOI();
            }
        }


        private void DoAOI()
        {

            if (cboxViewArea.Checked)
            {
                bDrawLine = true;
            }
            else
            {
                bDrawLine = false;
            }


            ByteLineThreshold = Convert.ToByte(txtLineThres.Text);
            ByteBubble = Convert.ToByte(txtBubble.Text);
            byteFinePixel = Convert.ToByte(txtFinePixel.Text);
            intY1 = Convert.ToInt16(txtY1.Text);
            intY2 = Convert.ToInt16(txtY2.Text);

            intCoarsePixel = Convert.ToInt16(txtCoarsePixel.Text);
            intBoxX1 = Convert.ToInt16(txtBoxX1.Text);
            intBoxX2 = Convert.ToInt16(txtBoxX2.Text);
            intBoxY1 = Convert.ToInt16(txtBoxY1.Text);
            intBoxY2 = Convert.ToInt16(txtBoxY2.Text);

            intSampleSize = Convert.ToInt16(txtSampleSize.Text);

            byteBackColor = Convert.ToByte(txtBackColor.Text);
            dblMaxWidth = Convert.ToDouble(txtMaxWidth.Text);
            dblMinWidth = Convert.ToDouble(txtMinWidth.Text);
            dblMinHeight = Convert.ToDouble(txtMinHeight.Text);
            dblTL1 = Convert.ToDouble(txtTL1.Text);
            dblTL2 = Convert.ToDouble(txtTL2.Text);
            dblTL3 = Convert.ToDouble(txtTL3.Text);
            dblTL4 = Convert.ToDouble(txtTL4.Text);
            dblTL5 = Convert.ToDouble(txtTL5.Text);
            lblTL1.Text = "%TL>=" + Convert.ToString(dblTL1);
            lblTL2.Text = "%TL>=" + Convert.ToString(dblTL2);
            lblTL3.Text = "%TL>=" + Convert.ToString(dblTL3);
            lblTL4.Text = "%TL>=" + Convert.ToString(dblTL4);
            lblTL5.Text = "%TL>=" + Convert.ToString(dblTL5);
            //intColWidth =

            dblPix2mm = Convert.ToDouble(txtPix2mm.Text);
            dblSlope = Convert.ToDouble(txtSlope.Text);
            dblOffset = Convert.ToDouble(txtOffset.Text);
            dblOffsetHuman = Convert.ToDouble(txtOffsetHuman.Text);
            intMaxWidth = Convert.ToInt16(dblMaxWidth / dblPix2mm);
            intMinWidth = Convert.ToInt16(dblMinWidth / dblPix2mm);
            intMinHeight = Convert.ToInt16(dblMinHeight / dblPix2mm);
            intBmpWidth = intBoxX2;
            intBmpHeight = intBoxY2;
            intCountSkipFrame = 0;
            //initial variable
            arrayBitmap0 = new byte[intBmpWidth, intBmpHeight];
            imageBox1.Invalidate();


            bytePixel10mm = Convert.ToByte(10 / dblPix2mm);
            bytePixel7mm = Convert.ToByte(7 / dblPix2mm);
            bytePixel4mm = Convert.ToByte(4 / dblPix2mm);

            intArrLineMaxX = new int[intMaxShrimp];
            intArrLineMinX = new int[intMaxShrimp];
            intArrLineMaxY = new int[intMaxShrimp];
            intArrLineMinY = new int[intMaxShrimp];
            ldblShrimpLength = new double[500];  //Maximum number of Length data

            SlopeLargeThreshold = Convert.ToDouble(txtL_SlopeThreshold.Text);

            intSkipFrame = Convert.ToInt16(txtSkipFrame.Text);

            intHairBrightness = Convert.ToInt16(txtHairBrightness.Text);

            switch (chrSize)
            {
                case 'S':
                    intX1 = Convert.ToInt16(txtS_LX1.Text);
                    intX2 = intX1 + Convert.ToInt16(txtS_LX2.Text);
                    intSizeWidth = Convert.ToInt16(txtSWidth.Text);
                    intSizeHeight = Convert.ToInt16(txtSHeight.Text);
                    ByteThreshold = Convert.ToByte(txtSThreshold.Text);
                    dblMaxRatio = Convert.ToDouble(txtS_MaxRatio.Text);
                    byteRow = Convert.ToByte(txtSRow.Text);
                    byteNumSection = Convert.ToByte(txtSectionS.Text);
                    break;

                case 'M':
                    intX1 = Convert.ToInt16(txtM_LX1.Text);
                    intX2 = intX1 + Convert.ToInt16(txtM_LX2.Text);
                    intSizeWidth = Convert.ToInt16(txtMWidth.Text);
                    intSizeHeight = Convert.ToInt16(txtMHeight.Text);
                    ByteThreshold = Convert.ToByte(txtMThreshold.Text);
                    dblMaxRatio = Convert.ToDouble(txtM_MaxRatio.Text);
                    byteRow = Convert.ToByte(txtMRow.Text);
                    byteNumSection = Convert.ToByte(txtSectionM.Text);
                    break;

                case 'L':
                    intX1 = Convert.ToInt16(txtL_LX1.Text);
                    intX2 = intX1 + Convert.ToInt16(txtL_LX2.Text);
                    intSizeWidth = Convert.ToInt16(txtLWidth.Text);
                    intSizeHeight = Convert.ToInt16(txtLHeight.Text);
                    ByteThreshold = Convert.ToByte(txtLThreshold.Text);
                    dblMaxRatio = Convert.ToDouble(txtL_MaxRatio.Text);
                    byteRow = Convert.ToByte(txtLRow.Text);
                    byteNumSection = Convert.ToByte(txtSectionL.Text);
                    break;

                case 'A':
                    intX1 = Convert.ToInt16(txtS_LX1.Text);
                    intX2 = intX1 + Convert.ToInt16(txtS_LX2.Text);
                    intSizeWidth = Convert.ToInt16(txtSWidth.Text);
                    intSizeHeight = Convert.ToInt16(txtSHeight.Text);
                    ByteThresholdS = Convert.ToByte(txtSThreshold.Text);
                    ByteThreshold = Convert.ToByte(txtMThreshold.Text);
                    ByteThresholdL = Convert.ToByte(txtLThreshold.Text);
                    dblMaxRatio = Convert.ToDouble(txtS_MaxRatio.Text);
                    byteRow = Convert.ToByte(txtSRow.Text);
                    byteNumSection = Convert.ToByte(txtSectionS.Text);



                    break;

                case 'C':
                    intX1 = Convert.ToInt16(txtC_LX1.Text);
                    intX2 = intX1 + Convert.ToInt16(txtC_LX2.Text);
                    intSizeWidth = Convert.ToInt16(txtCWidth.Text);
                    intSizeHeight = Convert.ToInt16(txtCHeight.Text);
                    ByteThreshold = Convert.ToByte(txtCThreshold.Text);
                    dblMaxRatio = Convert.ToDouble(txtC_MaxRatio.Text);
                    byteRow = Convert.ToByte(txtCRow.Text);
                    byteNumSection = Convert.ToByte(txtSectionC.Text);
                    break;
            }
            ShrimpSection = new PointF[byteNumSection + 1];
            iMedianDot = new int[byteNumSection + 1];
            iMedianDotShrimp = new int[intMaxShrimp, byteNumSection + 1];
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Int32 intSerialNumber;
            Int64 IntPassword;

            //DoAOI();
            //CameraInfo camInfo = m_camera.GetCameraInfo();
            //intSerialNumber = Convert.ToInt32(txtSerialNo.Text);
            //if (camInfo.serialNumber != intSerialNumber)
            //{
            //    msg("Error camera serial not match");
            //    return;
            //}



            /* START PASSWORD CHECK*/
            //12011104
            //154856347742817
            int serial_cam;
            int i;
            double intSerial;
            string strSerial;
            string strPassword;
            double lPassword;
            strSerial = txtSerialNo.Text;
            lPassword = 0;

            for (i = 0; i < 8; i++)
            {
                intSerial = Convert.ToByte(strSerial[i]);
                intSerial = Math.Pow(intSerial, 3) * (i + 1) + 78712 * (i + 1);
                lPassword = intSerial + lPassword;
            }
            lPassword = lPassword * 2;
            strPassword = lPassword.ToString();
            lPassword = lPassword / 2;
            strPassword = strPassword + lPassword.ToString();


            //if (txtPasswd.Text != strPassword)
            //{
            //    msg("Access Denied");
            //    return;
            //}
            /* END PASSWORD CHECK*/


            msg("Detect Shrimp");

            //m_camera.StartCapture();
            m_camera.BeginAcquisition();
            m_grabImages = false;

            m_grabShrimp = true;

            boolOneShot = true;
            bFirstImageColumn = true;
            bFirstFrame = true;
            _once = false;

            PixelState = 0;
            intShrimpCounter = 0;
            StartGrabLoopShrimp();

            btnStop.Enabled = true;
            toolStripButtonStop.Enabled = false;
            toolStripButtonStart.Enabled = false;
            toolStripCalibrate.Enabled = false;
            toolStripViewCorners.Enabled = false;
            btnStart.Enabled = false;

            txtMessage.Clear();
            chkListBox.Items.Clear();

            txtTank2.Text = "";

            string activeDir = @"c:\Pixel2";
            lblOK.Visible = false;
            lblShrimpCounter.Text = intShrimpCounter.ToString();
            lblShrimpLength.Text = "0.00";
            lblShrimpCounter.Refresh();

            //DateTime dt = DateTime.Now;
            //string custom = String.Format("{0:yy_MM_dd}", dt);
            //Create a new subfolder under the current active folder
            //string newPath = System.IO.Path.Combine(activeDir, custom);
            //string strPondTank = txtPond.Text + "_" + txtTank.Text;


            // Delete a directory and all subdirectories with Directory static method...
            if (System.IO.Directory.Exists(@"c:\Pixel2\Temp"))
            {
                try
                {
                    System.IO.Directory.Delete(@"c:\Pixel2\Temp", true);
                }

                catch (Exception ex)
                {
                    msg(ex.Message);
                }
            }
            subNewPath = System.IO.Path.Combine(activeDir, "Temp");
            System.IO.Directory.CreateDirectory(subNewPath);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_grabImages = false;
            m_grabShrimp = false;

            try
            {
                //m_camera.StopCapture();
                m_camera.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Failed to stop camera: " + ex.Message);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Camera is null");
            }


            msg("End Detection");

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            toolStripButtonStop.Enabled = false;
            toolStripButtonStart.Enabled = true;
            toolStripCalibrate.Enabled = true;
            toolStripViewCorners.Enabled = true;

            if (intShrimpCounter > byteRow)
            {
                if ((intShrimpCounter % byteRow) > 0)
                {
                    Image<Bgr, byte> grayFrameF = grayFrameC.Copy();
                    grayFrameE = grayFrameE.ConcateHorizontal(grayFrameF);
                }
                // add blank frame

                Image<Bgr, byte> grayFrameG = new Image<Bgr, byte>(300, 10, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                grayFrameE = grayFrameE.ConcateHorizontal(grayFrameG);

                imageBox3.Image = grayFrameE;

            }

            _once = true;
            CalculateStat();
            CreateGraph(zg1);
            zg1.Refresh();
        }

        //private void button1_Click(object sender, EventArgs e)
        //{                        
        //    int intCountList;
        //    //BlobTrack(251, 165);
        //    intCountList = chkListBox.Items.Count + 1;
        //    chkListBox.Items.Add("8.88" + "   " + Convert.ToString(intCountList), true);
        //    //BlobTrack(258, 89);
        //    //using (Image<Bgr, Byte> img = new Image<Bgr, Byte>(@"C:\Pixel2\Temp2\a.bmp"))
        //    //{
        //    //    PointF[][] pts = img.GoodFeaturesToTrack(20, 0.1, 10, 5);
        //    //    img.FindCornerSubPix(pts, new Size(21, 21), new Size(-1, -1), new MCvTermCriteria(20, 0.00001));
        //    //    foreach (PointF p in pts[0])
        //    //        img.Draw(new CircleF(p, 3.0f), new Bgr(0, 255, 0), 1);
        //    //    imageBox1.Image = img;
        //    //}
        //}

        private void Pixel2Millimeter(byte byteIndex, int xP, int yP)
        {
            int intSection;
            double dblAD, dblCD, dblAC, dblBD, dblBC;
            int intA, intB, intC, intD;
            //int xP = 248;
            //int yP = 190;
            //for (xP = 190; xP < 290; xP++)

            intSection = SearchSection(xP, yP);
            //calculate coordinate
            //AD = sqrt((xA-xD)^2 + (yA-yD)^2) A -> x - 1 - cornersX
            //CD = sqrt((xC-xD)^2 + (yC-yD)^2) B -> x - cornersX
            //AC = sqrt((xA-xC)^2 + (yA-yC)^2) C -> x
            //BD = sqrt((xB-xD)^2 + (yB-yD)^2) D -> x - 1
            //BC = sqrt((xB-xC)^2 + (yB-yC)^2

            //thetaD = arccos((AD^2 + CD^2 - AC^2) / (2*AD*CD))
            //thetaC = arccos((BC^2 + CD^2 - BD^2) / (2*BC*CD))
            intA = intSection - 1 - cornersX;
            intB = intSection - cornersX;
            intC = intSection;
            intD = intSection - 1;
            dblAD = Math.Sqrt(Math.Pow((ChessCorners[intA].X - ChessCorners[intD].X), 2) +
                Math.Pow((ChessCorners[intA].Y - ChessCorners[intD].Y), 2));
            dblCD = Math.Sqrt(Math.Pow((ChessCorners[intC].X - ChessCorners[intD].X), 2) +
                Math.Pow((ChessCorners[intC].Y - ChessCorners[intD].Y), 2));
            dblAC = Math.Sqrt(Math.Pow((ChessCorners[intA].X - ChessCorners[intC].X), 2) +
                Math.Pow((ChessCorners[intA].Y - ChessCorners[intC].Y), 2));
            dblBD = Math.Sqrt(Math.Pow((ChessCorners[intB].X - ChessCorners[intD].X), 2) +
                Math.Pow((ChessCorners[intB].Y - ChessCorners[intD].Y), 2));
            dblBC = Math.Sqrt(Math.Pow((ChessCorners[intB].X - ChessCorners[intC].X), 2) +
                Math.Pow((ChessCorners[intB].Y - ChessCorners[intC].Y), 2));

            if ((dblAD * dblCD == 0) || (dblBC * dblCD == 0))
            {
                msg("ErrorDetect");
            }

            double thetaD = Math.Acos((dblAD * dblAD + dblCD * dblCD - dblAC * dblAC) / (2 * dblAD * dblCD));
            double thetaC = Math.Acos((dblBC * dblBC + dblCD * dblCD - dblBD * dblBD) / (2 * dblBC * dblCD));

            //Find the distance DP:
            //DP = sqrt((xP-xD)^2 + (yP-yD)^2)
            double dblDP = Math.Sqrt(Math.Pow(xP - ChessCorners[intD].X, 2) + Math.Pow(yP - ChessCorners[intD].Y, 2));

            //Find the distance CP:
            //CP = sqrt((xP-xC)^2 + (yP-yC)^2)
            double dblCP = Math.Sqrt(Math.Pow(xP - ChessCorners[intC].X, 2) + Math.Pow(yP - ChessCorners[intC].Y, 2));

            //Find the angle thetaP1 between CD and DP:
            //thetaP1 = arccos((DP^2 + CD^2 - CP^2) / (2*DP*CD))
            double thetaP1 = Math.Acos((dblDP * dblDP + dblCD * dblCD - dblCP * dblCP) / (2 * dblDP * dblCD));

            //Find the angle thetaP2 between CD and CP:
            //thetaP2 = arccos((CP^2 + CD^2 - DP^2) / (2*CP*CD))
            double thetaP2 = Math.Acos((dblCP * dblCP + dblCD * dblCD - dblDP * dblDP) / (2 * dblCP * dblCD));

            //The ratio of thetaP1 to thetaD should be the ratio of thetaQ1 to 90. Therefore, calculate thetaQ1:
            //thetaQ1 = thetaP1 * 90 / thetaD
            double thetaQ1 = thetaP1 * Math.PI / thetaD / 2;

            //Similarly, calculate thetaQ2:
            double thetaQ2 = thetaP2 * Math.PI / thetaC / 2;

            //Find the distance HQ:
            //HQ = m * sin(thetaQ2) / sin(180-thetaQ1-thetaQ2)
            const double m = 6; // chessboard width is 6 millimeter.
            double HQ = m * Math.Sin(thetaQ2) / Math.Sin(Math.PI - thetaQ1 - thetaQ2);

            //Finally, the x and y position of Q relative to the bottom-left corner of EFGH is:
            //x = HQ * cos(thetaQ1)
            //y = HQ * sin(thetaQ1)
            ShrimpSection[byteIndex].X = (float)((intSection % cornersX) * m + HQ * Math.Cos(thetaQ1));
            ShrimpSection[byteIndex].Y = (float)(Math.Floor((double)(intSection / cornersX)) * m - HQ * Math.Sin(thetaQ1));

        }


        private void btnExport_Click(object sender, EventArgs e)
        {
            int i;
            int intCalSampleSize = 0;
            string lines;
            string strRow = "";
            string strLength = "";
            DateTime theDate = DateTime.Now;
            string custom = theDate.ToString("dd/MM/yyyy HH:mm:ss");
            string customDate = theDate.ToString("yyyy_MM_dd_");
            //txtData.AppendText(custom + ": " + Environment.NewLine);
            string path = "c:\\Pixel2\\Report2.csv";

            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
					//for Thailand
                    lines = "DateTime, Unit, Tank, Avg, %CV, >=" + txtTL1.Text + ", >=" + txtTL2.Text + ", >=" + txtTL3.Text + ", >=" + txtTL4.Text + ", >=" + txtTL5.Text + ", Count, Max, Min, Stdev, Note";
                    //for vietnam
                    if (dblTL4 < 1) {
                        lines = "DateTime, Unit, Tank, Avg, AvgHuman, %CV, >=" + txtTL1.Text + ", >=" + txtTL2.Text + ", >=" + txtTL3.Text + ", Count, Max, Min, Stdev, Note";
                    }


                    for (i = 1; i < intSampleSize + 1; i = i + 1)
                    {
                        strRow = strRow + "," + i.ToString();
                    }
                    lines = lines + strRow;
                    sw.WriteLine(lines);
                }
            }

            if ((txtUnit2.Text != "") && (txtTank2.Text != "") && (txtCV.Text != ""))
            {
                //for vietnam
                if (dblTL4 < 1)
                {
                    //lines = custom + "," + txtUnit2.Text + "," + txtTank2.Text + "," + txtMean.Text + "," + txtMeanHuman.Text + ","
                    lines = custom + "," + txtUnit2.Text + "," + txtTank2.Text + "," + txtMean.Text + "," + txtMean.Text + ","
                            + txtCV.Text + "," + txt85.Text + "," + txt80.Text + "," + txt7p75.Text + ","
                            + txtCount.Text + ","
                            + txtMax.Text + "," + txtMin.Text + "," + txtStdev.Text + ", Rev3";
                }
                else {
                    //for Thailand
                    lines = custom + "," + txtUnit2.Text + "," + txtTank2.Text + "," + txtMean.Text + ","
                        + txtCV.Text + "," + txt85.Text + "," + txt80.Text + "," + txt7p75.Text + ","
                        + txtDataTL4.Text + "," + txtDataTL5.Text + "," + txtCount.Text + ","
                        + txtMax.Text + "," + txtMin.Text + "," + txtStdev.Text + ", Rev3";

                }



                try
                {
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        int intItemsCount = chkListBox.Items.Count;
                        for (i = 0; i < intItemsCount; i++)
                        {
                            if (chkListBox.GetItemChecked(i))
                            {
                                strLength = strLength + "," + ldblShrimpLength[i].ToString("#.00");
                                intCalSampleSize++;
                            }

                            if (intSampleSize == intCalSampleSize)
                            {
                                break;
                            }
                        }
                        lines = lines + strLength;
                        sw.WriteLine(lines);
                    }
                    lblOK.Visible = true;

                }
                catch (System.Exception excep)
                {
                    MessageBox.Show(excep.Message);
                }

                Image<Bgr, Byte> SaveImage;
                SaveImage = (Image<Bgr, Byte>)imageBox3.Image;
                SaveImage.Save("c:\\Pixel2\\" + customDate + txtUnit2.Text + "_" + txtTank2.Text + ".jpg");


                if (chkRestart.Checked)
                {
                    Application.Restart();
                    Application.Exit();                
                }
            }
        }


        private void chkListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int intPhotoIndex;
            string strPhotoIndex;
            byte bSection;

            intPhotoIndex = chkListBox.SelectedIndex + 1;
            strPhotoIndex = intPhotoIndex.ToString();
            lblShrimpCounter.Text = strPhotoIndex;
            //lblShrimpLength.Text = ldblShrimpLength[intPhotoIndex - 1].ToString();
            //lblShrimpLength.Text = chkListBox.Items[intPhotoIndex-1].ToString();

            //for (bSection = 0; bSection < byteNumSection + 1; bSection++)
            //{
            //    iMedianDot[bSection] = iMedianDotShrimp[intPhotoIndex, bSection];
            //}
            //intLineMaxX = intArrLineMaxX[intPhotoIndex];
            //intLineMinX = intArrLineMinX[intPhotoIndex];
            //intLineMaxY = intArrLineMaxY[intPhotoIndex];
            //intLineMinY = intArrLineMinY[intPhotoIndex];

            bDrawShrimpCircleUp = true;
            _once = true;
            imageBox3.Refresh();
            CreateGraph(zg1);
            zg1.Refresh();
            //Image<Bgr, Byte> tempImage = new Image<Bgr, byte>(@"C:\Pixel2\Temp\" + strPhotoIndex + ".bmp");

            //imageBox1.Image = tempImage;
            //imageBox1.Invalidate();

        }

        private void BlobTrack(int xP, int yP)
        {
            byte bytePixel;
            int i, j;
            byte bOrder = 0;
            byte byteClockWise = 0;
            int iOldBlobX = 0;
            int iOldBlobY = 0;
            int iNewBlobX = 0;
            int iNewBlobY = 0;
            bool bSearchBlob = true;
            intEdgeX = new int[iMaxEdge];
            intEdgeY = new int[iMaxEdge];

            //--------------------   1. Find an Entry Point -> Find North White Point   ----------------------------//
            EdgeCount = 0;
            //start test code
            //grayFrame = new Image<Gray, Byte>(@"C:\Pixel2\Temp2\9.bmp");
            //imageBox1.Image = grayFrame;
            //imageBox1.Invalidate();
            //end test code

            i = xP;
            j = yP;



            intLineMaxX = 0;
            intLineMinX = 0;
            intLineMaxY = 0;
            intLineMinY = 0;

            for (j = yP; j > intBoxY1; j = j - byteFinePixel)
            {
                bytePixel = grayFrame.Data[j, xP, 0];
                if (bytePixel - arrayBitmap0[i, j] < ByteThreshold)
                //if (bytePixel < ByteThreshold)
                {
                    bOrder = 1;
                    intEdgeX[0] = xP;
                    intEdgeY[0] = j + 1;
                    iOldBlobX = xP;
                    iOldBlobY = j + 1;
                    intLineMaxX = xP;
                    intLineMinX = xP;
                    intLineMaxY = j + 1;
                    intLineMinY = j + 1;
                    break;
                }
            }

            if (intLineMaxX == 0)
            {
                intLineMaxX = 1;
            }

            //---------------   2. Find a neighbour with a brightness above/below the threshold  --------------------//
            //order   1             2           3           4                                     4
            //      6 7 8         4 5 6       2 3 4       8 1 2                                 8 1 2
            //      5 @ 1         3 @ 7       1 @ 5       7 @ 3  --> we use fourth order style  7 @ 3
            //      4 3 2         2 1 8       8 7 6       6 5 4                                 6 5 4


            while (bSearchBlob)
            {
                switch (bOrder)
                {
                    case 1:
                        iNewBlobX = iOldBlobX;
                        iNewBlobY = iOldBlobY - 1;
                        bOrder++;
                        break;

                    case 2:
                        iNewBlobX = iOldBlobX + 1;
                        iNewBlobY = iOldBlobY - 1;
                        bOrder++;
                        break;

                    case 3:
                        iNewBlobX = iOldBlobX + 1;
                        iNewBlobY = iOldBlobY;
                        bOrder++;
                        break;

                    case 4:
                        iNewBlobX = iOldBlobX + 1;
                        iNewBlobY = iOldBlobY + 1;
                        bOrder++;
                        break;

                    case 5:
                        iNewBlobX = iOldBlobX;
                        iNewBlobY = iOldBlobY + 1;
                        bOrder++;
                        break;

                    case 6:
                        iNewBlobX = iOldBlobX - 1;
                        iNewBlobY = iOldBlobY + 1;
                        bOrder++;
                        break;

                    case 7:
                        iNewBlobX = iOldBlobX - 1;
                        iNewBlobY = iOldBlobY;
                        bOrder++;
                        break;

                    case 8:
                        iNewBlobX = iOldBlobX - 1;
                        iNewBlobY = iOldBlobY - 1;
                        bOrder = 1;
                        break;
                }

                bytePixel = grayFrame.Data[iNewBlobY, iNewBlobX, 0];
                //if (bytePixel > ByteThreshold)
                if (bytePixel - arrayBitmap0[iNewBlobX, iNewBlobY] > ByteThreshold)
                {
                    byteClockWise = 0;
                    EdgeCount++;
                    iOldBlobX = iNewBlobX;
                    iOldBlobY = iNewBlobY;
                    if (iNewBlobX > intLineMaxX)
                    {
                        intLineMaxX = iNewBlobX;
                    }

                    if (iNewBlobX < intLineMinX)
                    {
                        intLineMinX = iNewBlobX;
                    }

                    if (iNewBlobY > intLineMaxY)
                    {
                        intLineMaxY = iNewBlobY;
                    }

                    if (iNewBlobY < intLineMinY)
                    {
                        intLineMinY = iNewBlobY;
                    }

                    if ((iNewBlobX == intEdgeX[0]) && (iNewBlobY == intEdgeY[0]))
                    {
                        bSearchBlob = false;
                        msg("EdgeCount " + EdgeCount.ToString());
                    }
                    intEdgeX[EdgeCount] = iNewBlobX;
                    intEdgeY[EdgeCount] = iNewBlobY;


                    if (EdgeCount > 900)
                    {
                        bBombEdgeCount = true;
                        bSearchBlob = false;
                    }


                    switch (bOrder)
                    {
                        case 2:
                        case 3:
                            bOrder = 7;
                            break;

                        case 4:
                        case 5:
                            bOrder = 1;
                            break;

                        case 6:
                        case 7:
                            bOrder = 3;
                            break;

                        case 1:
                        case 8:
                            bOrder = 5;
                            break;

                    }
                }
                else
                {

                    byteClockWise++;
                    if (byteClockWise > 10)
                    {
                        bBombCW = true;
                        bSearchBlob = false;
                    }
                }

            }
        }
        void CalculateStat()
        {
            int i;
            int intItemsCount;
            int intCalSampleSize;
            double dblDataTL4 = 0;
            double dblDataTL5 = 0;
            double dblTL7p75 = 0;
            double dblTL80 = 0;
            double dblTL85 = 0;
            double dblLengthValue;
            double dblMean;
            double sumMean = 0;
            double dblBigSum;
            double dblStdev, dblCv;
            double dblMax, dblMin;
            double[] LengthValues;

            intItemsCount = chkListBox.Items.Count;

            if (intSampleSize == 0)
                LengthValues = new double[intItemsCount];
            else
                LengthValues = new double[intSampleSize];



            intCalSampleSize = 0;
            dblMax = 0;
            dblMin = 100;
            for (i = 0; i < intItemsCount; i++)
            {
                if (chkListBox.GetItemChecked(i))
                {
                    dblLengthValue = ldblShrimpLength[i];
                    sumMean = sumMean + dblLengthValue;
                    if (dblLengthValue > dblTL5)
                    {
                        dblDataTL5 = dblDataTL5 + 1;
                    }
                    if (dblLengthValue > dblTL4)
                    {
                        dblDataTL4 = dblDataTL4 + 1;
                    }
                    if (dblLengthValue > dblTL3)
                    {
                        dblTL7p75 = dblTL7p75 + 1;
                    }
                    if (dblLengthValue > dblTL2)
                    {
                        dblTL80 = dblTL80 + 1;
                    }
                    if (dblLengthValue > dblTL1)
                    {
                        dblTL85 = dblTL85 + 1;
                    }
                    if (dblLengthValue > dblMax)
                    {
                        dblMax = dblLengthValue;
                    }
                    if (dblLengthValue < dblMin)
                    {
                        dblMin = dblLengthValue;
                    }

                    LengthValues[intCalSampleSize] = dblLengthValue;
                    intCalSampleSize++;
                }



                if (intSampleSize != 0)
                {
                    if (intSampleSize == intCalSampleSize)
                    {
                        break;
                    }
                }

            }

            // Calculate the mean
            dblMean = sumMean / intCalSampleSize;
            dblBigSum = 0;


            // Calculate the total for the standard deviation
            for (i = 0; i < intCalSampleSize; i++)
            {
                dblBigSum += Math.Pow(LengthValues[i] - dblMean, 2);
            }

            // Now we can calculate the standard deviation
            dblStdev = Math.Sqrt(dblBigSum / (intCalSampleSize - 1));
            dblCv = dblStdev / dblMean * 100;
            // Display the values(standard deviationand stuff)

#if Decimal2
            txtMean.Text = dblMean.ToString("#.00");
            txtDataTL5.Text = (dblMean + dblOffsetHuman).ToString("#.00");
            txtStdev.Text = dblStdev.ToString("#.00");
            dblTL7p75 = dblTL7p75 / intCalSampleSize * 100;
            txt7p75.Text = dblTL7p75.ToString("#.00");
            dblTL80 = dblTL80 / intCalSampleSize * 100;
            txt80.Text = dblTL80.ToString("#.00");
            dblTL85 = dblTL85 / intCalSampleSize * 100;
            txt85.Text = dblTL85.ToString("#.00");
            dblDataTL4 = dblDataTL4 / intCalSampleSize * 100;
            txtDataTL4.Text = dblDataTL4.ToString("#.00");
            dblDataTL5 = dblDataTL5 / intCalSampleSize * 100;
            txtDataTL5.Text = dblDataTL5.ToString("#.00");
            txtCV.Text = dblCv.ToString("#.00");
            txtMax.Text = dblMax.ToString("#.00");
            txtMin.Text = dblMin.ToString("#.00");
            txtCount.Text = intCalSampleSize.ToString();
#else
            txtMean.Text = dblMean.ToString("#.000");
            txtDataTL5.Text = (dblMean + dblOffsetHuman).ToString("#.000");
            txtStdev.Text = dblStdev.ToString("#.000");
            dblTL7p75 = dblTL7p75 / intCalSampleSize * 100;
            txt7p75.Text = dblTL7p75.ToString("#.000");
            dblTL80 = dblTL80 / intCalSampleSize * 100;
            txt80.Text = dblTL80.ToString("#.000");
            dblTL85 = dblTL85 / intCalSampleSize * 100;
            txt85.Text = dblTL85.ToString("#.000");
            dblDataTL4 = dblDataTL4 / intCalSampleSize * 100;
            txtDataTL4.Text = dblDataTL4.ToString("#.000");
            dblDataTL5 = dblDataTL5 / intCalSampleSize * 100;
            txtDataTL5.Text = dblDataTL5.ToString("#.000");
            txtCV.Text = dblCv.ToString("#.000");
            txtMax.Text = dblMax.ToString("#.000");
            txtMin.Text = dblMin.ToString("#.000");
            txtCount.Text = intCalSampleSize.ToString();
#endif


        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            CalculateStat();

        }

        private void CreateGraph(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;
            int[] saveChkIndex;

            saveChkIndex = new int[500];


            zg1.GraphPane.CurveList.Clear();

            // Set the titles and axis labels
            myPane.Title.Text = "Shrimp Length";
            myPane.XAxis.Title.Text = "Count";
            myPane.YAxis.Title.Text = "Length (mm)";

            // Make up some data points from the Sine function
            PointPairList list = new PointPairList();
            PointPairList Avglist = new PointPairList();
            PointPairList StdevUplist = new PointPairList();
            PointPairList StdevDownlist = new PointPairList();

            double sumMean = 0;
            int intCalSampleSize = 0;
            int intItemsCount = chkListBox.Items.Count;


            //double y = x * 2 + 5;
            //list.Add(x, y);
            for (int x = 0; x < intItemsCount; x++)
            {
                if (chkListBox.GetItemChecked(x))
                {
                    double y = ldblShrimpLength[x];
                    sumMean = sumMean + y;
                    list.Add(intCalSampleSize + 1, y);
                    intCalSampleSize++;
                }
                if (intSampleSize != 0)
                {
                    if (intSampleSize == intCalSampleSize)
                    {
                        break;
                    }
                }
            }

            double Mean = sumMean / intCalSampleSize;
            double Limit = Mean * 0.12;
            for (int x = 0; x < intCalSampleSize; x++)
            {
                Avglist.Add(x + 1, Mean);
                StdevUplist.Add(x + 1, Mean + Limit);
                StdevDownlist.Add(x + 1, Mean - Limit);
            }

            // Generate a blue curve with circle symbols, and "My Curve 2" in the legend
            LineItem myCurve = myPane.AddCurve("Length", list, Color.Blue, SymbolType.Circle);
            LineItem AvgCurve = myPane.AddCurve("Average", Avglist, Color.Red, SymbolType.None);
            LineItem StdevUpCurve = myPane.AddCurve("Upper CV", StdevUplist, Color.Green, SymbolType.None);
            LineItem StdevDownCurve = myPane.AddCurve("Lower CV", StdevDownlist, Color.Green, SymbolType.None);
            //// Fill the area under the curve with a white-red gradient at 45 degrees
            //myCurve.Line.Fill = new Fill(Color.White, Color.Red, 45F);

            //// Make the symbols opaque by filling them with white
            myCurve.Symbol.Fill = new Fill(Color.White);

            //// Fill the axis background with a color gradient
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45F);

            //// Fill the pane background with a color gradient
            myPane.Fill = new Fill(Color.White, Color.FromArgb(220, 220, 255), 45F);

            //// Calculate the Axis Scale Ranges
            zgc.AxisChange();

            CalculateStat();
        }




        private Image<Gray, Byte> GetShrimpImage(Image<Gray, byte> GetImage)
        {
            using (Image<Lab, Byte> lab = GetImage.Convert<Lab, Byte>())
            {
                Image<Gray, byte> result = lab[2];
                result._ThresholdBinary(new Gray(135), new Gray(255));
                return result;
            }
        }

        private void btnImgFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtImagePath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        static int intPictureFile = 1;
        static bool bFirstFrame = true;
        static bool bFirstImageColumn = true;
        static bool bOldClearLine = true;
        Image<Bgr, Byte> grayFrameC;
        Image<Bgr, Byte> grayFrameD;
        Image<Bgr, Byte> grayFrameE;
        byte byteBackColor;

        private void toolStripOpen_Click(object sender, EventArgs e)
        {
            imageBox1.BackColor = Color.Gray;
            string strChosenFile, strFrame0File;
            strFrame0File = txtImagePath.Text + "/" + "0.bmp";
            strChosenFile = txtImagePath.Text + "/" + intPictureFile.ToString() + ".bmp";

            Stopwatch watch;
            watch = Stopwatch.StartNew();
            intPictureFile++;

            //lblMsg.Text = strChosenFile;
            Image<Bgr, Byte> img0 = new Image<Bgr, Byte>(strFrame0File);
            Image<Gray, Byte> gray0 = img0.Convert<Gray, Byte>();

            Image<Bgr, Byte> img1 = new Image<Bgr, Byte>(strChosenFile);
            Image<Gray, Byte> grayFrame = img1.Convert<Gray, Byte>();

            bool bDetectShrimpX = false;

            ////Point2D
            //LineSegment2D LsX1 = new LineSegment2D( new Point(intX1, intY1), new Point(intX1, intY2));
            //LineSegment2D LsX2 = new LineSegment2D(new Point(intX2, intY1), new Point(intX2, intY2));
            //int iLineWidth = intY2 - intY1;
            //byte[,] ArrayLX1, ArrayLX2;
            //ArrayLX1 = new byte[iLineWidth + 1, 1];
            //ArrayLX1 = grayFrame.Sample(LsX1);
            //ArrayLX2 = new byte[iLineWidth + 1, 1];
            //ArrayLX2 = grayFrame.Sample(LsX2);
            //int i;
            //for (i = 0; i < iLineWidth; i++)
            //{
            //    if((ArrayLX1[i,0] > ByteThreshold) || (ArrayLX2[i,0] > ByteThreshold))
            //    {
            //        //Detect Shrimp
            //        bDetectShrimpX = true;
            //        break;
            //    }
            //}

            int intROIWidth, intROIHeight;
            intROIHeight = intBoxY2 - intBoxY1;
            intROIWidth = intBoxX2 - intBoxX1;

            grayFrame.ROI = new Rectangle(intBoxX1, intBoxY1, intROIWidth, intROIHeight);
            gray0.ROI = new Rectangle(intBoxX1, intBoxY1, intROIWidth, intROIHeight);

            Image<Gray, Byte> S = grayFrame.Sub(gray0);

            //Point2D
            LineSegment2D LsX1 = new LineSegment2D(new Point(intX1 - intBoxX1, intY1 - intBoxY1), new Point(intX1 - intBoxX1, intY2 - intBoxY1));
            LineSegment2D LsX2 = new LineSegment2D(new Point(intX2 - intBoxX1, intY1 - intBoxY1), new Point(intX2 - intBoxX1, intY2 - intBoxY1));

            if (Math.Abs(gray0.Data[320, 240, 0] - gray0.Data[320, 241, 0]) < 5)
            {
                byteBackColor = gray0.Data[320, 240, 0];
            }


            int iLineWidth = intY2 - intY1;
            byte[,] ArrayLX1, ArrayLX2;

            ArrayLX1 = new byte[iLineWidth + 1, 1];
            ArrayLX1 = S.Sample(LsX1);
            ArrayLX2 = new byte[iLineWidth + 1, 1];
            ArrayLX2 = S.Sample(LsX2);

            int i;

            double OffsetLargeThreshold;

            byte ByteOffsetLargeThreshold = 0;



            for (i = 0; i < iLineWidth; i++)
            {
                if ((ArrayLX1[i, 0] > ByteLineThreshold) || (ArrayLX2[i, 0] > ByteLineThreshold))
                {
                    //Detect Shrimp
                    bDetectShrimpX = true;
                    break;
                }
            }



            if (bDetectShrimpX)
            {
                if (bOldClearLine)
                {

                    Image<Gray, Byte> Sbin = S.ThresholdBinary(new Gray(ByteThreshold), new Gray(255));
                    //Image<Gray, Byte> S = grayFrame.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY_INV, 3, new Gray(4));

                    #region Find triangles and rectangles

                    List<Triangle2DF> triangleList = new List<Triangle2DF>();
                    List<MCvBox2D> boxList = new List<MCvBox2D>();


                    bool bDetectLarge = false;
                    bool bDetectSmall = false;
                    using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                        for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                        {
                            //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                            if (contours.Area > 10) //only consider contours with area greater than 250
                            {
                                boxList.Add(contours.GetMinAreaRect());
                                if (boxList[(boxList.Count) - 1].size.Height > bytePixel10mm)
                                {
                                    bDetectLarge = true;

                                    OffsetLargeThreshold = boxList[(boxList.Count) - 1].size.Height - bytePixel10mm;
                                    ByteOffsetLargeThreshold = Convert.ToByte(SlopeLargeThreshold * OffsetLargeThreshold);


                                    boxList.Clear();
                                    break;
                                }
                                if (boxList[(boxList.Count) - 1].size.Width > bytePixel10mm)
                                {
                                    bDetectLarge = true;

                                    OffsetLargeThreshold = boxList[(boxList.Count) - 1].size.Width - bytePixel10mm;
                                    ByteOffsetLargeThreshold = Convert.ToByte(SlopeLargeThreshold * OffsetLargeThreshold);

                                    boxList.Clear();

                                    break;
                                }
                                if ((boxList[(boxList.Count) - 1].size.Height < bytePixel7mm) && (boxList[(boxList.Count) - 1].size.Height > bytePixel4mm))
                                {
                                    bDetectSmall = true;
                                    boxList.Clear();
                                    break;
                                }
                                if ((boxList[(boxList.Count) - 1].size.Width < bytePixel7mm) && (boxList[(boxList.Count) - 1].size.Width > bytePixel4mm))
                                {
                                    bDetectSmall = true;
                                    boxList.Clear();
                                    break;
                                }
                            }
                        }

                    #endregion

                    if (bDetectLarge)
                    {
                        Sbin = S.ThresholdBinary(new Gray(ByteThresholdL + ByteOffsetLargeThreshold), new Gray(255));

                        #region Find triangles and rectangles Large Size

                        boxList = new List<MCvBox2D>();


                        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                            for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                            {
                                //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                if (contours.Area > 10) //only consider contours with area greater than 250
                                {
                                    boxList.Add(contours.GetMinAreaRect());
                                }
                            }

                        #endregion

                    }
                    if (bDetectSmall)
                    {
                        Sbin = S.ThresholdBinary(new Gray(ByteThresholdS), new Gray(255));

                        #region Find triangles and rectangles Large Size

                        boxList = new List<MCvBox2D>();


                        using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                            for (Contour<Point> contours = Sbin.FindContours(); contours != null; contours = contours.HNext)
                            {
                                //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                                if (contours.Area > 10) //only consider contours with area greater than 250
                                {
                                    boxList.Add(contours.GetMinAreaRect());
                                }
                            }

                        #endregion

                    }


                    img1.ROI = new Rectangle(intBoxX1, intBoxY1, intBoxX2 - intBoxX1, intBoxY2 - intBoxY1);

                    #region draw triangles and rectangles
                    Image<Bgr, Byte> RectangleImage = img1.Copy();
                    //Image<Gray, Byte> RectangleImage = S.Copy();

                    //intShrimpCounter = 0;
                    bool bFoundShrimp = false;
                    foreach (MCvBox2D box in boxList)
                    //cannyFrame.Draw(box, new Gray(255), 1);
                    {

                        if ((box.size.Width > ByteBubble) || (box.size.Height > ByteBubble))
                        {

                            // x' = xcos@ + ysin@
                            // y' = -xsin@ + ycos@
                            double dblbH = box.size.Height; //y
                            double dblbW = box.size.Width;  //x
                            double dblbA = box.angle;
                            double angle = Math.PI * dblbA / 180.0;
                            double sinA = Math.Sin(angle);
                            double cosA = Math.Cos(angle);
                            double centerX = box.center.X;
                            double centerY = box.center.Y;
                            double dblShrimpLength;
                            double dblShrimpDiameter;
                            bool bRotatePicture = false;

                            if (dblbW > dblbH)
                            {
                                dblShrimpLength = dblbW;
                                dblShrimpDiameter = dblbH;
                            }
                            else
                            {
                                dblShrimpLength = dblbH;
                                dblShrimpDiameter = dblbW;
                                bRotatePicture = true;
                            }
                            double dblShrimpLengthmm = dblPix2mm * dblShrimpLength;


                            if (dblShrimpLengthmm > 10)
                            {
                                Sbin = S.ThresholdBinary(new Gray(50), new Gray(255));
                                //recalculate
                            }

                            if (dblShrimpLength > intMaxWidth)
                            {
                                RectangleImage.Draw(box, new Bgr(Color.AliceBlue), 1);
                            }
                            else if (dblShrimpLength < intMinWidth)
                            {
                                RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                            }
                            else if (dblShrimpDiameter < intMinHeight)
                            {
                                RectangleImage.Draw(box, new Bgr(Color.Wheat), 1);
                            }
                            else if (dblShrimpLength / dblShrimpDiameter < dblMaxRatio)
                            {
                                RectangleImage.Draw(box, new Bgr(Color.BlueViolet), 1);
                            }
                            else
                            {
                                // check leftframe
                                bFoundShrimp = true;
                                double dblWidth = dblbW * cosA - dblbH * sinA;
                                double dblHeight = dblbH * cosA - dblbW * sinA;
                                //if (dblHeight > dblWidth)
                                //{
                                //    double dblTemp;
                                //    dblTemp = dblHeight;
                                //    dblHeight = dblWidth;
                                //    dblWidth = dblTemp;

                                //}
                                if (centerX < dblWidth / 2)
                                {
                                    // check leftframe
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if ((centerX + (dblWidth) / 2) + 2 > (intBoxX2 - intBoxX1)) //+ 2 to prevent error
                                {
                                    // check rightframe
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if (centerY < (dblHeight) / 2)
                                {
                                    // check topframe
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if ((centerY + (dblHeight) / 2) > (intBoxY2 - intBoxY1))
                                {
                                    // check bottomframe
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if (dblShrimpLengthmm > dblMaxWidth)
                                {

                                }
                                else if (intBoxY2 - intBoxY1 - dblShrimpDiameter / 2 - centerY < 5)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if (centerY - dblShrimpDiameter / 2 < 5)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Red), 1);
                                }
                                else if (centerX - dblShrimpLength > intX2)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                }
                                else if (centerX + dblShrimpLength < intX1)
                                {
                                    RectangleImage.Draw(box, new Bgr(Color.Pink), 1);
                                }
                                else
                                {
                                    bOldClearLine = false;
                                    //RectangleImage.Draw(box, new Bgr(Color.Lime), 1);

                                    msg(dblShrimpLength.ToString("F"));
                                    //msg(box.angle.ToString("F") + "," + box.size.Height.ToString("F") + "," + box.size.Width.ToString("F"));

                                    intShrimpCounter++;

                                    int intULX = Convert.ToInt16(centerX - (dblbW * cosA - dblbH * sinA) / 2);
                                    int intULY = Convert.ToInt16(centerY - (dblbH * cosA - dblbW * sinA) / 2);
                                    Image<Bgr, Byte> imgA = RectangleImage.Copy();
                                    //Image<Gray, Byte> imgGA = imgA.Convert<Gray, Byte>();
                                    int intWidth = Convert.ToInt16(dblWidth);
                                    int intHeight = Convert.ToInt16(dblHeight);

                                    imgA.ROI = new Rectangle(intULX, intULY, intWidth, intHeight);
                                    Image<Bgr, Byte> grayFrameA = imgA.Copy();
                                    Image<Bgr, Byte> grayFrameB;
                                    if (bRotatePicture)
                                    {
                                        grayFrameB = grayFrameA.Rotate(90 - dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false); ;
                                    }
                                    else
                                    {
                                        grayFrameB = grayFrameA.Rotate(-dblbA, new Bgr(byteBackColor, byteBackColor, byteBackColor), false);
                                    }


                                    int intShrimpLength = Convert.ToInt16(dblShrimpLength + 1);
                                    int intShrimpDiameter = Convert.ToInt16(dblShrimpDiameter + 1);
                                    intULX = (grayFrameB.Size.Width - intShrimpLength) / 2;
                                    intULY = (grayFrameB.Size.Height - intShrimpDiameter) / 2;

                                    grayFrameB.ROI = new Rectangle(intULX, intULY, intShrimpLength + 5, intShrimpDiameter + 5);
                                    imageBox2.Image = grayFrameB;
                                    Image<Gray, Byte> grayFrameSection = grayFrameB.Convert<Gray, Byte>();

                                    //if ShrimpLength is more than 11 mm detect hair is true
                                    bool detectHair = false;
                                    dblShrimpHair_mm = Convert.ToDouble(txtShrimpHair_mm.Text);
                                    if (dblShrimpLengthmm > dblShrimpHair_mm)
                                    {
                                        detectHair = true;
                                    }


                                    byte bSection;
                                    int j;
                                    byte[,] ArrayLSection;
                                    int index = 0;
                                    int indexOld = 0;
                                    double dblSumSection = 0;
                                    //add code of Divide Shrimp into section
                                    iSectionSpace = intShrimpLength / byteNumSection;
                                    double dblShrimpNumeratorLength = dblShrimpLength - iSectionSpace * (byteNumSection - 1);
                                    int intOffsetHairOld = 0;



                                    for (bSection = 0; bSection < byteNumSection + 1; bSection++)
                                    {
                                        if (bSection == 0)
                                            j = 5;                              //start scan first line
                                        else if (bSection == byteNumSection)
                                            j = intShrimpLength - 3;            //end scan
                                        else
                                            j = iSectionSpace * bSection + 2;   //middle scan


                                        //Point2D scanshrimp section
                                        LineSegment2D Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                        ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                        ArrayLSection = grayFrameSection.Sample(Lsection);
                                        int max = 0;
                                        int rowGreen = 4;
                                        int countMax = 0;
                                        int sumPixelValue = 0;
                                        byte pixMax = 0;
                                        byte pixMin = 255;
                                        byte bArrayLSection;
                                        int intPixmax = 0;
                                        int intAvgDetectHair = 0;
                                        int intOffsetHair = 0;

                                        for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                        {
                                            bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                            if (bArrayLSection > pixMax)
                                            {
                                                pixMax = bArrayLSection;
                                                intPixmax = i + rowGreen / 2;
                                            }

                                            if (bArrayLSection < pixMin)
                                            {
                                                pixMin = bArrayLSection;
                                            }
                                            sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                        }
                                        intAvgDetectHair = (ArrayLSection[intPixmax + 2, 0] + ArrayLSection[intPixmax - 2, 0]) / 2;
                                        int Avg = sumPixelValue / (intShrimpDiameter - rowGreen + 1) + (pixMax - pixMin) / 8;
                                        if ((intAvgDetectHair < intHairBrightness) && detectHair)
                                        {
                                            //Detect shrimp hair
                                            msg("Detect Shrimp hair");
                                            if (bSection == 0)
                                            {
                                                while (intAvgDetectHair < intHairBrightness)
                                                {
                                                    j = j + 2;

                                                    //Point2D scanshrimp section
                                                    Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                                    ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                                    ArrayLSection = grayFrameSection.Sample(Lsection);
                                                    pixMax = 0;
                                                    pixMin = 255;
                                                    for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                                    {
                                                        bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                                        if (bArrayLSection > pixMax)
                                                        {
                                                            pixMax = bArrayLSection;
                                                            intPixmax = i + rowGreen / 2;
                                                        }

                                                        if (bArrayLSection < pixMin)
                                                        {
                                                            pixMin = bArrayLSection;
                                                        }
                                                        sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                                    }
                                                    intAvgDetectHair = (ArrayLSection[intPixmax + 3, 0] + ArrayLSection[intPixmax - 3, 0]) / 2;

                                                }
                                                intOffsetHair = j;
                                            }

                                            if (bSection == byteNumSection)
                                            {
                                                while (intAvgDetectHair < 65)
                                                {
                                                    j = j - 2;
                                                    intOffsetHair = intOffsetHair - 2;
                                                    //Point2D scanshrimp section
                                                    Lsection = new LineSegment2D(new Point(j, 0), new Point(j, intShrimpDiameter));
                                                    ArrayLSection = new byte[intShrimpDiameter + 1, 1];
                                                    ArrayLSection = grayFrameSection.Sample(Lsection);
                                                    pixMax = 0;
                                                    pixMin = 255;
                                                    for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                                    {
                                                        bArrayLSection = ArrayLSection[i + rowGreen / 2, 0];
                                                        if (bArrayLSection > pixMax)
                                                        {
                                                            pixMax = bArrayLSection;
                                                            intPixmax = i + rowGreen / 2;
                                                        }

                                                        if (bArrayLSection < pixMin)
                                                        {
                                                            pixMin = bArrayLSection;
                                                        }
                                                        sumPixelValue = sumPixelValue + ArrayLSection[i + rowGreen / 2, 0];
                                                    }
                                                    intAvgDetectHair = (ArrayLSection[intPixmax + 2, 0] + ArrayLSection[intPixmax - 2, 0]) / 2;

                                                }
                                            }


                                        }
                                        else
                                        {
                                            intOffsetHair = 0;
                                        }

                                        bool _recOnce = true;
                                        int[] indexDiameter = new int[200];

                                        for (i = 0; i < intShrimpDiameter - rowGreen; i++) // rowGreen is to remove green color line from top and bottom
                                        {
                                            if (ArrayLSection[i + rowGreen / 2, 0] > Avg)
                                            {
                                                //max = ArrayLSection[i + rowGreen/2, 0];
                                                //record first index
                                                if (_recOnce)
                                                {
                                                    index = i;
                                                    _recOnce = false;
                                                }
                                                indexDiameter[countMax] = i;
                                                countMax++;
												
                                            }
                                        }

                                        //index = index + rowGreen / 2 + countMax / 2;
                                        index = rowGreen / 2 + indexDiameter[countMax / 2];
                                        if (bSection > 0)
                                        {
                                            if (bSection == 1)
                                            {
                                                LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace + intOffsetHairOld, indexOld), new Point(bSection * iSectionSpace, index));
                                                grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                LB = new LineSegment2D(new Point(intOffsetHairOld, 0), new Point(intOffsetHairOld, intShrimpDiameter));
                                                grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + (dblShrimpNumeratorLength - intOffsetHairOld) * (dblShrimpNumeratorLength - intOffsetHairOld));
                                            }
                                            else if (bSection == byteNumSection)
                                            {
                                                LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace + intOffsetHairOld, indexOld), new Point(bSection * iSectionSpace + intOffsetHair, index));
                                                grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                LB = new LineSegment2D(new Point(intShrimpLength + intOffsetHair, 0), new Point(intShrimpLength + intOffsetHair, intShrimpDiameter));
                                                grayFrameB.Draw(LB, new Bgr(Color.Lime), 1);
                                                dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + (dblShrimpNumeratorLength + intOffsetHair) * (dblShrimpNumeratorLength + intOffsetHair));
                                            }
                                            else
                                            {
                                                LineSegment2D LB = new LineSegment2D(new Point((bSection - 1) * iSectionSpace, indexOld), new Point(bSection * iSectionSpace, index));
                                                grayFrameB.Draw(LB, new Bgr(Color.Turquoise), 1);
                                                dblSumSection = dblSumSection + Math.Sqrt((indexOld - index) * (indexOld - index) + iSectionSpace * iSectionSpace);
                                            }
                                            // pythagoras triangular

                                        }

                                        indexOld = index;
                                        intOffsetHairOld = intOffsetHair;

                                    }

                                    //msg("T: " + dblSumSection.ToString("F"));

                                    dblShrimpLengthmm = dblSumSection * dblPix2mm;
                                    dblShrimpLengthmm = dblShrimpLengthmm * dblSlope + dblOffset;  //correlation
                                    ldblShrimpLength[intShrimpCounter - 1] = dblShrimpLengthmm;
                                    chkListBox.Items.Add(Convert.ToString(intShrimpCounter) + "   " + dblShrimpLengthmm.ToString("#.00"), true);
                                    //lblShrimpLength.Text = dblShrimpLengthmm.ToString("F");
                                    lblShrimpLength.Text = dblShrimpLengthmm.ToString("#.00");

                                    if (bFirstFrame)
                                    {
                                        bFirstFrame = false;
                                        grayFrameC = grayFrameB.Copy();
                                        int iaddHeight = intSizeHeight - grayFrameC.Size.Height;
                                        //int iaddWidth = 250 - grayFrameC.Size.Width;
                                        grayFrameD = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                        grayFrameC = grayFrameC.ConcateHorizontal(grayFrameD);
                                        grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                        grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                        imageBox3.Image = grayFrameC;
                                    }
                                    else
                                    {
                                        grayFrameD = grayFrameB.Copy();

                                        grayFrameB = new Image<Bgr, byte>(intSizeWidth - (intShrimpLength + 5), intShrimpDiameter + 5, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                        grayFrameD = grayFrameD.ConcateHorizontal(grayFrameB);

                                        grayFrameC = grayFrameC.ConcateVertical(grayFrameD);
                                        //add more space
                                        int iaddHeight = intSizeHeight - grayFrameD.Size.Height;
                                        if (iaddHeight < 0)
                                        {
                                            msg("Overheight");
                                            intShrimpCounter--;
                                            return;
                                        }

                                        grayFrameD = new Image<Bgr, byte>(intSizeWidth, iaddHeight, new Bgr(byteBackColor, byteBackColor, byteBackColor));
                                        grayFrameC = grayFrameC.ConcateVertical(grayFrameD);

                                        imageBox3.Image = grayFrameC;
                                        if ((intShrimpCounter % byteRow) == 0)
                                        {
                                            bFirstFrame = true;
                                            if (bFirstImageColumn)
                                            {
                                                bFirstImageColumn = false;
                                                grayFrameE = grayFrameC.Copy();
                                            }
                                            else
                                            {
                                                Image<Bgr, byte> grayFrameF = grayFrameC.Copy();
                                                grayFrameE = grayFrameE.ConcateHorizontal(grayFrameF);
                                                imageBox3.Image = grayFrameE;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //imageBox1.Image = cannyFrame;
                    #endregion



                    imageBox1.Image = Sbin;

                    //if (bFoundShrimp)
                    //{
                    //imageBox1.Image = RectangleImage;
                    //imageBox1.Image = S;
                    //}

                    lblShrimpCounter.Text = intShrimpCounter.ToString();

                    //imageBox1.Image = img1;
                    //imageBox1.Image = S;

                    watch.Stop();
                    lblWatch.Text = watch.ElapsedMilliseconds.ToString() + " ms";
                }
                else
                {
                    imageBox1.Image = img1;
                }

            }
            else
            {
                imageBox1.Image = img1;
                bOldClearLine = true;
                //watch.Stop();
                //lblShrimpCounter.Text = "none";
                //lblShrimpLength.Text = watch.ElapsedMilliseconds.ToString();
            }
        }



        private void toolStripInspect_Click(object sender, EventArgs e)
        {
            /*string strChosenFileA;
            string strChosenFileB;
            strChosenFileA = txtImagePath.Text + "/" + "2.bmp";
            strChosenFileB = txtImagePath.Text + "/" + "2.bmp";


            if (imageBox1.Visible == true)
            {
                imageBox1.Visible = false;
                lblShrimpCounter.Visible = false;
                lblPieces.Visible = false;
                lblShrimpLength.Visible = false;
                lblWatch.Visible = false;
                label13.Visible = false;
                imageBox3.Visible = true;
                imageBox2.Visible = false;
                //imageBox3.Image = grayFrameC;
            }
            else
            {
                imageBox1.Visible = true;
                lblShrimpCounter.Visible = true;
                lblPieces.Visible = true;
                lblShrimpLength.Visible = true;
                lblWatch.Visible = true;
                label13.Visible = true;
                imageBox3.Visible = false;
                imageBox2.Visible = true;
            }*/


            /* START PASSWORD CHECK*/
            //12011104
            //154856347742817

            int serial_cam;
            int i;
            double intSerial;
            string strSerial;
            string strPassword;
            double lPassword;
            strSerial = txtSerialNo.Text;
            //strSerial = "12011104";
            lPassword = 0;

            for (i = 0; i < 8; i++)
            {
                intSerial = Convert.ToByte(strSerial[i]);
                intSerial = Math.Pow(intSerial, 3) * (i + 1) + 78712 * (i + 1);
                lPassword = intSerial + lPassword;
            }
            lPassword = lPassword * 2;
            strPassword = lPassword.ToString();
            lPassword = lPassword / 2;
            strPassword = strPassword + lPassword.ToString();
            MessageBox.Show(strPassword);


        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            int iWidth, iHeight;

            if (WindowState != FormWindowState.Minimized)
            {
                iWidth = ActiveForm.Size.Width;
                iHeight = ActiveForm.Size.Height;

                imageBox3.Size = new Size(iWidth - 360, iHeight - 60);
            }
        }

        private void btnSave2_Click(object sender, EventArgs e)
        {
            SaveXML();
            DoAOI();
        }

        char chrSize;
        private void rdoLSize_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoLSize.Checked)
            {
                chrSize = 'L';
            }
            DoAOI();
        }

        private void rdoMSize_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoMSize.Checked)
            {
                chrSize = 'M';
            }
            DoAOI();
        }

        private void rdoSSize_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoSSize.Checked)
            {
                chrSize = 'S';
            }
            DoAOI();
        }

        private void rdoCSize_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoCSize.Checked)
            {
                chrSize = 'C';
            }
            DoAOI();
        }

        private void rdoAllSize_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoAllSize.Checked)
            {
                chrSize = 'A';
            }
            DoAOI();
        }

        private void imageBox3_MouseClick(object sender, MouseEventArgs e)
        {

            int Xpos, Ypos;
            Point imageBox3point;
            imageBox3point = imageBox3.PointToClient(MousePosition);
            int intScrollV = imageBox3.VerticalScrollBar.Value;
            int intScrollH = imageBox3.HorizontalScrollBar.Value;
            Xpos = imageBox3point.X;
            Ypos = imageBox3point.Y;

            int ix = (Xpos + intScrollH) / intSizeWidth;
            int iy = (Ypos + intScrollV) / intSizeHeight;
            int intListCount = ix * byteRow + iy;

            if ((Control.ModifierKeys & Keys.Shift) != 0)
            {
                MessageBox.Show("Press Shift");
            }
            else if (intShrimpCounter > intListCount)
            {
                chkListBox.SetItemChecked(intListCount, !chkListBox.GetItemChecked(intListCount));

                _once = true;
                imageBox3.Refresh();
                CreateGraph(zg1);
                zg1.Refresh();
            }

        }

        private bool _once = false;

        private void imageBox3_Paint(object sender, PaintEventArgs e)
        {
            Font drawFont = new Font("Arial", 32);
            SolidBrush drawBrush = new SolidBrush(Color.White);

            if (_once)
            {

                int intListCount = chkListBox.Items.Count;

                for (int i = 0; i < intListCount; i++)
                {
                    int intGraphRow = i / byteRow;
                    int intGraphCol = i % byteRow;
                    if (!chkListBox.GetItemChecked(i))
                    {
                        Rectangle ee = new Rectangle(intGraphRow * intSizeWidth, intGraphCol * intSizeHeight, intSizeWidth, intSizeHeight);
                        //using (Pen pen = new Pen(Color.Red, 2))
                        using (Brush myBrush = new SolidBrush(Color.Black))
                        {
                            //e.Graphics.DrawRectangle(pen, ee);
                            e.Graphics.FillRectangle(myBrush, ee);
                            e.Graphics.DrawString("     " + Convert.ToString(i + 1), drawFont, drawBrush, ee);
                        }
                    }
                }
            }
        }

        private void btnCalib_Click(object sender, EventArgs e)
        {
            if (txtStd_mm.Text == "")
            {
                MessageBox.Show("Please Fill Standard mm", "Warning!");
                return;
            }
            double dblStdmm = Convert.ToDouble(txtStd_mm.Text);


            if (txtMean.Text == "")
            {
                MessageBox.Show("Error Average Value", "Warning");
                return;
            }

            double dblAvg = Convert.ToDouble(txtMean.Text);
            if (dblAvg < 3)
            {
                MessageBox.Show("Error Average Value", "Warning");
                return;
            }

            double dblCurrentPix2mm = Convert.ToDouble(txtPix2mm.Text);

            string message = "New Calibration Pixel2mm is " + Convert.ToString(dblCurrentPix2mm / dblAvg * dblStdmm);

            const string caption = "Calibration Constant";
            var result = MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);


            if (result == DialogResult.Yes)
            {
                // cancel the closure of the form.
                txtPix2mm.Text = Convert.ToString(dblCurrentPix2mm / dblAvg * dblStdmm);
                SaveXML();
                DoAOI();
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 3)
            {
                imageBox1.Visible = false;
                lblShrimpCounter.Visible = false;
                lblPieces.Visible = false;
                lblShrimpLength.Visible = false;
                lblWatch.Visible = false;
                label13.Visible = false;
                imageBox3.Visible = true;
                imageBox2.Visible = false;
                //imageBox3.Image = grayFrameC;
                if (WindowState == FormWindowState.Maximized)
                {

                    WindowState = FormWindowState.Normal;

                }
                if (WindowState == FormWindowState.Normal)
                {

                    WindowState = FormWindowState.Maximized;

                }
            }

            if (tabControl1.SelectedIndex == 0)
            {
                imageBox1.Visible = true;
                lblShrimpCounter.Visible = true;
                lblPieces.Visible = true;
                lblShrimpLength.Visible = true;
                lblWatch.Visible = true;
                label13.Visible = true;
                imageBox3.Visible = false;
                imageBox2.Visible = true;
            }
        }

        private void toolStripMeasure_Click(object sender, EventArgs e)
        {
            /*
            int i, j;
            txtMessage.Clear();
            msg("Detecting Image");
            //this.Refresh();
            m_camera.StartCapture();
			
            // Retrieve an image
            try
            {
                m_camera.RetrieveBuffer(m_rawImage);
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                //continue;
            }

            lock (this)
            {
                m_rawImage.Convert(PixelFormat.PixelFormatBgr, m_processedImage);
            }

            // Get the Bitmap object. Bitmaps are only valid if the
            // pixel format of the ManagedImage is RGB or RGBU.
            System.Drawing.Bitmap Processed_Bitmap = m_processedImage.bitmap;

            try
            {
                m_camera.StopCapture();
            }
            catch (FC2Exception ex)
            {
                Debug.WriteLine("Failed to stop camera: " + ex.Message);
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("Camera is null");
            }

            //Image<Bgr, Byte> frame = new Image<Bgr, Byte>(Processed_Bitmap);
            //Image<Bgr, Byte> img1 = new Image<Gray, Byte>(Processed_Bitmap);
            //grayFrame = img1.Convert<Gray, Byte>(); //where bmp is a Bitmap


            if (false)
            {

            }
            else
            {
                msg("Measuring Failed");
            }
            imageBox1.Image = grayFrame;
            imageBox1.Invalidate();
            */

        }
    }
}


