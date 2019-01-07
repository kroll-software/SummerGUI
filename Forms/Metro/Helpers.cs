using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;

namespace MetroFramework.Drawing
{
    public static class Helpers
    {
        private static ColorMatrix _disabledImageColorMatrix;


        internal static ColorMatrix MultiplyColorMatrix(float[][] matrix1, float[][] matrix2)
        {
            int num = 5;
            float[][] newColorMatrix = new float[num][];
            for (int i = 0; i < num; i++)
            {
                newColorMatrix[i] = new float[num];
            }
            float[] numArray2 = new float[num];
            for (int j = 0; j < num; j++)
            {
                for (int k = 0; k < num; k++)
                {
                    numArray2[k] = matrix1[k][j];
                }
                for (int m = 0; m < num; m++)
                {
                    float[] numArray3 = matrix2[m];
                    float num6 = 0f;
                    for (int n = 0; n < num; n++)
                    {
                        num6 += numArray3[n] * numArray2[n];
                    }
                    newColorMatrix[m][j] = num6;
                }
            }
            return new ColorMatrix(newColorMatrix);
        }

        /// <summary>
        /// Gets the disabled image color matrix
        /// </summary>
        private static ColorMatrix DisabledImageColorMatrix
        {
            get
            {
                if (_disabledImageColorMatrix == null)
                {
                    float[][] numArray = new float[5][];
                    numArray[0] = new float[] { 0.2125f, 0.2125f, 0.2125f, 0f, 0f };
                    numArray[1] = new float[] { 0.2577f, 0.2577f, 0.2577f, 0f, 0f };
                    numArray[2] = new float[] { 0.0361f, 0.0361f, 0.0361f, 0f, 0f };
                    float[] numArray3 = new float[5];
                    numArray3[3] = 1f;
                    numArray[3] = numArray3;
                    numArray[4] = new float[] { 0.38f, 0.38f, 0.38f, 0f, 1f };
                    float[][] numArray2 = new float[5][];
                    float[] numArray4 = new float[5];
                    numArray4[0] = 1f;
                    numArray2[0] = numArray4;
                    float[] numArray5 = new float[5];
                    numArray5[1] = 1f;
                    numArray2[1] = numArray5;
                    float[] numArray6 = new float[5];
                    numArray6[2] = 1f;
                    numArray2[2] = numArray6;
                    float[] numArray7 = new float[5];
                    numArray7[3] = 0.7f;
                    numArray2[3] = numArray7;
                    numArray2[4] = new float[5];
                    _disabledImageColorMatrix = MultiplyColorMatrix(numArray2, numArray);
                }
                return _disabledImageColorMatrix;
            }
        }        

        public static Image CreateDisabledImage(Image normalImage)
        {
            ImageAttributes imageAttr = new ImageAttributes();
            imageAttr.ClearColorKey();
            imageAttr.SetColorMatrix(DisabledImageColorMatrix);
            Size size = normalImage.Size;
            Bitmap image = new Bitmap(size.Width, size.Height);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawImage(normalImage, new Rectangle(0, 0, size.Width, size.Height), 0, 0, size.Width, size.Height, GraphicsUnit.Pixel, imageAttr);
            }
            return image;
        }
    }
}
