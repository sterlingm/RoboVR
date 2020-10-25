/*
© Siemens AG, 2017
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class PointCloud
    {
        public Point[] Points;
        
        public PointCloud(Messages.Sensor.Image depthImage, Messages.Sensor.Image rgbImage, float focalX, float focalY)
        {
            uint width = depthImage.width;
            uint height = depthImage.height;
            float invFocalX = 1.0f / focalX;
            float invFocalY = 1.0f / focalY;

            Points = new Point[width * height];
            int numNotNan = 0;
            for (uint v = 0; v < height; v++)
            {
                for (uint u = 0; u < width; u++)
                {
                    uint i = u + (v * width);
                    float depth = depthImage.data[i];

                    Points[i] = new Point();
                    if (depth == 0)
                    {
                        Points[i].x = float.NaN;
                        Points[i].y = float.NaN;
                        Points[i].z = float.NaN;
                        Points[i].rgb = new int[] { 0, 0, 0 };
                    }
                    else
                    {
                        Points[i].z = depth * invFocalX;
                        //float xi = u / depth;
                        //float xs = u * depth * invFocalX;
                        //Points[i].z = (xs / xi) * focalX;
                        Points[i].x = (u * depth * invFocalX) / 100.0f;
                        Points[i].y = (v * depth * invFocalY) / 100.0f;
                        Points[i].rgb = new int[] { rgbImage.data[i], rgbImage.data[i+1], rgbImage.data[i+2] };
                        numNotNan++;
                    }
                }
            }
            //MonoBehaviour.print(string.Format("numNotNan: {0}", numNotNan));
        }


        public void printRandPoint(int seed)
        {
            System.Random rng = new System.Random(seed);
            int i = rng.Next(Points.Length);
            MonoBehaviour.print(string.Format("point: {0},{1},{2}", Points[i].x, Points[i].y, Points[i].z));
        }
    }
    public class Point
    {
        public float x;
        public float y;
        public float z;
        public int[] rgb;
        

        public override string ToString()
        {
            return "xyz=(" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")"
                + "  rgb=(" + rgb[0].ToString() + ", " + rgb[1].ToString() + ", " + rgb[2].ToString() + ")";
        }
        private static float getValue(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            float result = BitConverter.ToSingle(bytes, 0);
            return result;
        }
        private static int[] getRGB(byte[] bytes)
        {
            int[] rgb = new int[3];
            rgb[0] = Convert.ToInt16(bytes[0]);
            rgb[1] = Convert.ToInt16(bytes[1]);
            rgb[2] = Convert.ToInt16(bytes[2]);
            return rgb;
        }
    }

}