/*M///////////////////////////////////////////////////////////////////////////////////////
//
//  IMPORTANT: READ BEFORE DOWNLOADING, COPYING, INSTALLING OR USING.
//
//  By downloading, copying, installing or using the software you agree to this license.
//  If you do not agree to this license, do not download, install,
//  copy or use the software.
//
//
//                        Intel License Agreement
//                For Open Source Computer Vision Library
//
// Copyright (C) 2000, Intel Corporation, all rights reserved.
// Third party copyrights are property of their respective owners.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
//   * Redistribution's of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//
//   * Redistribution's in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//
//   * The name of Intel Corporation may not be used to endorse or promote products
//     derived from this software without specific prior written permission.
//
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall the Intel Corporation or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//
//M*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// Port of a subset of the OpenCV EigenObjects functions to managed C# so we don't need to
    /// use Emgu CV and bring in the entire unmanaged library.
    /// (Very few comments follow as I'm mostly blindly translating..)
    /// </summary>
    public static class EigenObjects
    {
        /// <summary>
        /// Calculates eigen objects
        /// </summary>
        public static void CalcEigenObjects(Bitmap[] input, int maxIteration, double eps, DoubleImage[] eigVecs, double[] eigVals, DoubleImage avg)
        {
            if (input.Length == 0)
                return;

            int nObjects = input.Length;
            int nEigens = nObjects - 1;

            byte[][] objs = new byte[nObjects][];
            double[][] eigs = new double[nEigens][];
            int obj_step = 0, old_step = 0;
            int eig_step = 0, oldeig_step = 0;
            Size obj_size = avg.Size, old_size = avg.Size, oldeig_size = avg.Size;

            for (var i = 0; i < nObjects; i++)
            {
                Bitmap obj = input[i];
                objs[i] = obj.CopyGrayscaleBitmapToByteArray(out obj_step);
                obj_size = obj.Size;

                if (obj_size != avg.Size || obj_size != old_size)
                    throw new EigenObjectException("Different sizes of objects");
                if (i > 0 && obj_step != old_step)
                    throw new EigenObjectException("Different steps of objects");

                old_step = obj_step;
                old_size = obj_size;
            }

            for (var i = 0; i < nEigens; i++)
            {
                DoubleImage eig = eigVecs[i];
                eig_step = eig.Step;
                eigs[i] = eig.Data;

                if (eig.Size != avg.Size || eig.Size != oldeig_size)
                    throw new EigenObjectException("Different sizes of objects");
                if (i > 0 && eig_step != oldeig_step)
                    throw new EigenObjectException("Different steps of objects");

                oldeig_step = eig.Step;
                oldeig_size = eig.Size;
            }

            CalcEigenObjects(nObjects, objs, obj_step,
                                        eigs, eig_step, obj_size,
                                        maxIteration, eps, avg.Data, avg.Step, eigVals);
        }

        /// <summary>
        /// Calculates eigen decomposite
        /// </summary>
        public static double[] EigenDecomposite(Bitmap obj, DoubleImage[] eigInput, DoubleImage avg)
        {
            var nEigObjs = eigInput.Length;
            var coeffs = new double[nEigObjs];

            int i;

            int obj_step;
            byte[] obj_data = obj.CopyGrayscaleBitmapToByteArray(out obj_step);
            Size obj_size = obj.Size;

            /*cvGetImageRawData( avg, (uchar **) & avg_data, &avg_step, &avg_size );
            if( avg->depth != IPL_DEPTH_32F )
                CV_ERROR( CV_BadDepth, cvUnsupportedFormat );
            if( avg->nChannels != 1 )
                CV_ERROR( CV_BadNumChannels, cvUnsupportedFormat );

            cvGetImageRawData( obj, &obj_data, &obj_step, &obj_size );
            if( obj->depth != IPL_DEPTH_8U )
                CV_ERROR( CV_BadDepth, cvUnsupportedFormat );
            if( obj->nChannels != 1 )
                CV_ERROR( CV_BadNumChannels, cvUnsupportedFormat );*/

            if (obj_size != avg.Size)
                throw new EigenObjectException("Different sizes of objects");

            double[][] eigs = new double[nEigObjs][];
            int eig_step = 0, old_step = 0;
            Size eig_size = avg.Size, old_size = avg.Size;

            for (i = 0; i < nEigObjs; i++)
            {
                DoubleImage eig = eigInput[i];
                eig_step = eig.Step;
                eigs[i] = eig.Data;

                if (eig_size != avg.Size || eig_size != old_size)
                    throw new EigenObjectException("Different sizes of objects");
                if (i > 0 && eig_step != old_step)
                    throw new EigenObjectException("Different steps of objects");

                old_step = eig.Step;
                old_size = eig.Size;
            }

            EigenDecomposite(obj_data, obj_step, nEigObjs, eigs, eig_step, avg.Data, avg.Step, obj_size, coeffs);

            return coeffs;
        }

        /// <summary>
        /// Helper function to calculate eigen decomposite
        /// </summary>
        private static void EigenDecomposite(byte[] obj, int objStep, int nEigObjs,
                            double[][] eigInput, int eigStep, double[] avg, int avgStep,
                            Size size, double[] coeffs)
        {
            int i;

            if (nEigObjs < 2)
                throw new EigenObjectException("Must have at least 2 training images for recognition!");

            if (size.Width > objStep || size.Width > eigStep || size.Width > avgStep || size.Height < 1)
                throw new EigenObjectException("CV_BADSIZE_ERR");

            /* no callback */
            for (i = 0; i < nEigObjs; i++)
            {
                double w = CalcDecompCoeff(obj, objStep, eigInput[i], eigStep, avg, avgStep, size);

                if (w < -1.0e29f)
                    throw new EigenObjectException("NOT DEFINED?");

                coeffs[i] = w;
            }
        }

        /// <summary>
        /// Helper function to calculate the decomp coefficient
        /// </summary>
        private static double CalcDecompCoeff(byte[] obj, int objStep,
                           double[] eigObj, int eigStep,
                           double[] avg, int avgStep, Size size)
        {
            int i, k;
            double w = 0.0f;

            if (size.Width > objStep || size.Width > eigStep || size.Width > avgStep || size.Height < 1)
                return -1.0e30f;

            if (obj == null || eigObj == null || avg == null)
                return -1.0e30f;

            if (size.Width == objStep && size.Width == eigStep && size.Width == avgStep)
            {
                size.Width *= size.Height;
                size.Height = 1;
                objStep = eigStep = avgStep = size.Width;
            }

            for (i = 0; i < size.Height; i++)
            {
                var iObj = i * objStep;
                var iEig = i * eigStep;
                var iAvg = i * avgStep;

                for (k = 0; k < size.Width - 4; k += 4)
                {
                    double o = (double)obj[iObj + k];
                    double e = eigObj[iEig + k];
                    double a = avg[iAvg + k];

                    w += e * (o - a);
                    o = (double)obj[iObj + k + 1];
                    e = eigObj[iEig + k + 1];
                    a = avg[iAvg + k + 1];
                    w += e * (o - a);
                    o = (double)obj[iObj + k + 2];
                    e = eigObj[iEig + k + 2];
                    a = avg[iAvg + k + 2];
                    w += e * (o - a);
                    o = (double)obj[iObj + k + 3];
                    e = eigObj[iEig + k + 3];
                    a = avg[iAvg + k + 3];
                    w += e * (o - a);
                }

                for (; k < size.Width; k++)
                    w += eigObj[iEig + k] * ((double)obj[iObj + k] - avg[iAvg + k]);
            }

            return w;
        }

        /// <summary>
        /// Helper function to calculate eigen objects
        /// </summary>
        private static void CalcEigenObjects(int nObjects, byte[][] input, int objStep,
                                double[][] output, int eigStep, Size size,
                                int maxIteration, double eps, double[] avg,
                                int avgStep, double[] eigVals)
        {
            int i, j, m1 = nObjects - 1, objStep1 = objStep, eigStep1 = eigStep;
            Size objSize, eigSize, avgSize;
            double[] c = null;
            double[] ev = null;
            double[] bf = null;
            double m = 1.0f / (double)nObjects;

            /*if (m1 > maxIteration && calcLimit->type != CV_TERMCRIT_EPS)
                m1 = calcLimit->max_iter;*/

            /* ---- TEST OF PARAMETERS ---- */

            if (nObjects < 2)
                throw new EigenObjectException("CV_BADFACTOR_ERR");
            if (size.Width > objStep || size.Width > eigStep || size.Width > avgStep || size.Height < 1)
                throw new EigenObjectException("CV_BADSIZE_ERR");

            if (objStep == size.Width && eigStep == size.Width && avgStep == size.Width)
            {
                size.Width *= size.Height;
                size.Height = 1;
                objStep = objStep1 = eigStep = eigStep1 = avgStep = size.Width;
            }

            objSize = eigSize = avgSize = size;

            /*n = objSize.height * objSize.width * (ioFlags & CV_EIGOBJ_INPUT_CALLBACK) +
                2 * eigSize.height * eigSize.width * (ioFlags & CV_EIGOBJ_OUTPUT_CALLBACK);*/

            /* Calculation of averaged object */
            bf = avg;
            for (i = 0; i < avgSize.Height; i++)
                for (j = 0; j < avgSize.Width; j++)
                    bf[(i * avgStep) + j] = 0;

            for (i = 0; i < nObjects; i++)
            {
                int k, l;
                byte[] bu = input[i];

                bf = avg;
                for (k = 0; k < avgSize.Height; k++)
                    for (l = 0; l < avgSize.Width; l++)
                        bf[(k * avgStep) + l] += bu[(k * objStep1) + l];
            }

            bf = avg;
            for (i = 0; i < avgSize.Height; i++)
                for (j = 0; j < avgSize.Width; j++)
                    bf[(i * avgStep) + j] *= m;

            /* Calculation of covariance matrix */
            c = new double[nObjects * nObjects];

            CalcCovarMatrixEx(nObjects, input, objStep1, avg, avgStep, size, c);

            /* Calculation of eigenvalues & eigenvectors */
            ev = new double[nObjects * nObjects];

            if (eigVals == null)
                eigVals = new double[nObjects];

            JacobiEigens(c, ev, eigVals, nObjects, 0.0f);

            /* Eigen objects number determination */
            for (i = 0; i < m1; i++)
                if (Math.Abs(eigVals[i] / eigVals[0]) < eps)
                    break;

            m1 = maxIteration = i;

            eps = Math.Abs(eigVals[m1 - 1] / eigVals[0]);

            for (i = 0; i < m1; i++)
                eigVals[i] = 1.0 / Math.Sqrt((double)eigVals[i]);

            /* ----------------- Calculation of eigenobjects ----------------------- */
            {
                int k, p, l;

                /* e.o. annulation */
                for (i = 0; i < m1; i++)
                {
                    double[] be = output[i];

                    for (p = 0; p < eigSize.Height; p++)
                        for (l = 0; l < eigSize.Width; l++)
                            be[(p * eigStep) + l] = 0.0f;
                }

                for (k = 0; k < nObjects; k++)
                {
                    byte[] bv = input[k];

                    for (i = 0; i < m1; i++)
                    {
                        double v = eigVals[i] * ev[(i * nObjects) + k];
                        double[] be = output[i];
                        byte[] bu = bv;

                        bf = avg;

                        for (p = 0; p < size.Height; p++)
                        {
                            int iBu = p * objStep1;
                            int iBf = p * avgStep;
                            int iBe = p * eigStep1;

                            for (l = 0; l < size.Width - 3; l += 4)
                            {
                                double f = bf[iBf + l];
                                byte u = bu[iBu + l];

                                be[iBe + l] += v * (u - f);
                                f = bf[iBf + l + 1];
                                u = bu[iBu + l + 1];
                                be[iBe + l + 1] += v * (u - f);
                                f = bf[iBf + l + 2];
                                u = bu[iBu + l + 2];
                                be[iBe + l + 2] += v * (u - f);
                                f = bf[iBf + l + 3];
                                u = bu[iBu + l + 3];
                                be[iBe + l + 3] += v * (u - f);
                            }

                            for (; l < size.Width; l++)
                                be[iBe + l] += v * (bu[iBu + l] - bf[iBf + l]);
                        }
                    }
                }
            }

            for (i = 0; i < m1; i++)
                eigVals[i] = 1.0f / (eigVals[i] * eigVals[i]);
        }

        /// <summary>
        /// Calculates covariance matrix
        /// </summary>
        private static void CalcCovarMatrixEx(int nObjects, byte[][] input, int objStep1,
                             double[] avg, int avgStep,
                             Size size, double[] covarMatrix)
        {
            int objStep = objStep1;

            /* ---- TEST OF PARAMETERS ---- */

            if (nObjects < 2)
                throw new EigenObjectException("Must have at least 2 training images for recognition!");

            if (size.Width > objStep || size.Width > avgStep || size.Height < 1)
                throw new EigenObjectException("CV_BADSIZE_ERR");

            int i, j;
            byte[][] objects = input;

            for (i = 0; i < nObjects; i++)
            {
                byte[] bu = objects[i];

                for (j = i; j < nObjects; j++)
                {
                    int k, l;
                    double w = 0f;
                    double[] a = avg;
                    byte[] bu1 = bu;
                    byte[] bu2 = objects[j];

                    for (k = 0; k < size.Height; k++)
                    {
                        int kBu1 = k * objStep;
                        int kBu2 = k * objStep;
                        int kA = k * avgStep;

                        for (l = 0; l < size.Width - 3; l += 4)
                        {
                            double f = a[kA + l];
                            byte u1 = bu1[kBu1 + l];
                            byte u2 = bu2[kBu2 + l];

                            w += (u1 - f) * (u2 - f);
                            f = a[kA + l + 1];
                            u1 = bu1[kBu1 + l + 1];
                            u2 = bu2[kBu2 + l + 1];
                            w += (u1 - f) * (u2 - f);
                            f = a[kA + l + 2];
                            u1 = bu1[kBu1 + l + 2];
                            u2 = bu2[kBu2 + l + 2];
                            w += (u1 - f) * (u2 - f);
                            f = a[kA + l + 3];
                            u1 = bu1[kBu1 + l + 3];
                            u2 = bu2[kBu2 + l + 3];
                            w += (u1 - f) * (u2 - f);
                        }

                        for (; l < size.Width; l++)
                        {
                            double f = a[kA + l];
                            byte u1 = bu1[kBu1 + l];
                            byte u2 = bu2[kBu2 + l];

                            w += (u1 - f) * (u2 - f);
                        }
                    }

                    covarMatrix[(i * nObjects) + j] = covarMatrix[(j * nObjects) + i] = w;
                }
            }
        }

        /// <summary>
        /// Calculates jacobi eigens
        /// </summary>
        private static void JacobiEigens(double[] a, double[] v, double[] e, int n, double eps)
        {
            int i, j, k, ind;
            double aMax, anorm = 0, ax;

            if (n <= 0)
                throw new EigenObjectException("CV_BADSIZE_ERR");

            if (eps < 1.0e-7f)
                eps = 1.0e-7f;

            /*-------- Prepare --------*/
            for (i = 0; i < n; i++)
            {
                int ixn = i * n;

                for (j = 0; j < i; j++)
                {
                    double am = a[ixn + j];

                    anorm += am * am;
                }

                for (j = 0; j < n; j++)
                    v[ixn + j] = 0f;

                v[ixn + i] = 1f;
            }

            anorm = Math.Sqrt(anorm + anorm);
            ax = anorm * eps / n;
            aMax = anorm;

            while (aMax > ax)
            {
                aMax /= n;
                
                do
                {
                    int p, q;
                    int v1 = 0, a1 = 0;

                    ind = 0;
                    for (p = 0; p < n - 1; p++, a1 += n, v1 += n)
                    {
                        int a2 = n * (p + 1), v2 = n * (p + 1);

                        for (q = p + 1; q < n; q++, a2 += n, v2 += n)
                        {
                            double x, y, c, s, c2, s2, z;
                            int a3 = 0;
                            double apq = a[a1 + q], app, aqq, aip, aiq, vpi, vqi;

                            if (Math.Abs(apq) < aMax)
                                continue;

                            ind = 1;

                            /*---- Calculation of rotation angle's sine & cosine ----*/
                            app = a[a1 + p];
                            aqq = a[a2 + q];
                            y = 5.0e-1 * (app - aqq);
                            x = -apq / Math.Sqrt((double)(apq * apq) + (double)(y * y));
                            if (y < 0.0)
                                x = -x;
                            s = x / Math.Sqrt(2.0 * (1.0 + Math.Sqrt(1.0 - (x * x))));
                            s2 = s * s;
                            c = Math.Sqrt(1.0 - s2);
                            c2 = c * c;
                            z = 2.0 * apq * c * s;

                            /*---- Apq annulation ----*/
                            for (i = 0; i < p; i++, a3 += n)
                            {
                                aip = a[a3 + p];
                                aiq = a[a3 + q];
                                vpi = v[v1 + i];
                                vqi = v[v2 + i];
                                a[a3 + p] = (aip * c) - (aiq * s);
                                a[a3 + q] = (aiq * c) + (aip * s);
                                v[v1 + i] = (vpi * c) - (vqi * s);
                                v[v2 + i] = (vqi * c) + (vpi * s);
                            }

                            for (; i < q; i++, a3 += n)
                            {
                                aip = a[a1 + i];
                                aiq = a[a3 + q];
                                vpi = v[v1 + i];
                                vqi = v[v2 + i];
                                a[a1 + i] = (aip * c) - (aiq * s);
                                a[a3 + q] = (aiq * c) + (aip * s);
                                v[v1 + i] = (vpi * c) - (vqi * s);
                                v[v2 + i] = (vqi * c) + (vpi * s);
                            }

                            for (; i < n; i++)
                            {
                                aip = a[a1 + i];
                                aiq = a[a2 + i];
                                vpi = v[v1 + i];
                                vqi = v[v2 + i];
                                a[a1 + i] = (aip * c) - (aiq * s);
                                a[a2 + i] = (aiq * c) + (aip * s);
                                v[v1 + i] = (vpi * c) - (vqi * s);
                                v[v2 + i] = (vqi * c) + (vpi * s);
                            }

                            a[a1 + p] = (app * c2) + (aqq * s2) - z;
                            a[a2 + q] = (app * s2) + (aqq * c2) + z;
                            a[a1 + q] = a[a2 + p] = 0.0f;
                        }               /*q */
                    }                   /*p */
                }
                while (ind > 0);

                aMax /= n;
            }                           /* while ( Amax > ax ) */

            for (i = 0, k = 0; i < n; i++, k += n + 1)
                e[i] = a[k];
            /*printf(" M = %d\n", M); */

            /* -------- ordering -------- */
            for (i = 0; i < n; i++)
            {
                int m = i;
                double em = Math.Abs(e[i]);

                for (j = i + 1; j < n; j++)
                {
                    double ej = Math.Abs(e[j]);

                    m = (em < ej) ? j : m;
                    em = (em < ej) ? ej : em;
                }

                if (m != i)
                {
                    int l;
                    double b = e[i];

                    e[i] = e[m];
                    e[m] = b;
                    for (j = 0, k = i * n, l = m * n; j < n; j++, k++, l++)
                    {
                        b = v[k];
                        v[k] = v[l];
                        v[l] = b;
                    }
                }
            }
        }
    }
}
