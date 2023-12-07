using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging; //WriteableBitmap
using System.Windows.Media; //PixelFormats

namespace Azure.ImagingSystem
{
    public class ImagingHelper
    {
        public static void Delay(int mm)
        {
            try
            {
                DateTime current = DateTime.Now;

                while (current.AddMilliseconds(mm) > DateTime.Now)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
            catch
            {
                System.Threading.Thread.Sleep(mm);
            }
            return;
        }

        /// <summary>
        /// Align the source images to the reference (the laser C/Red channel) image.
        /// </summary>
        /// <param name="imagesUnaligned"></param>
        /// <param name="imagesAligned"></param>
        /// <param name="deltaX">the x-axis offset of other channels to channel C</param>
        /// <param name="deltaY">the y-axis offset of other channels to channel C</param>
        public static unsafe void AlignImage(byte*[] imagesUnaligned, byte*[] imagesAligned, int[] deltaX, int[] deltaY, int imageWidth, int imageHeight)
        {
            int positiveDeltaXMax = 0;
            int negativeDeltaXMax = 0;
            int positiveDeltaYMax = 0;
            int negativeDeltaYMax = 0;

            positiveDeltaXMax = deltaX.Max();
            if (positiveDeltaXMax < 0)
            {
                positiveDeltaXMax = 0;
            }
            negativeDeltaXMax = deltaX.Min();
            if (negativeDeltaXMax > 0)
            {
                negativeDeltaXMax = 0;
            }

            positiveDeltaYMax = deltaY.Max();
            if (positiveDeltaYMax < 0)
            {
                positiveDeltaYMax = 0;
            }
            negativeDeltaYMax = deltaY.Min();
            if (negativeDeltaYMax > 0)
            {
                negativeDeltaYMax = 0;
            }

            int destImagePixelWidth = (int)imageWidth + negativeDeltaXMax - positiveDeltaXMax;
            int destImagePixelHeight = (int)imageHeight + negativeDeltaYMax - positiveDeltaYMax;

            if (destImagePixelWidth != imageWidth || destImagePixelHeight != imageHeight)
            {
                int fixSourceImageWidthNoDouble = 0;
                int fixDestImageWidthNoDouble = 0;
                if (imageWidth % 2 != 0)
                {
                    fixSourceImageWidthNoDouble = 1;
                }
                if (destImagePixelWidth % 2 != 0)
                {
                    fixDestImageWidthNoDouble = 1;
                }

                unsafe
                {
                    // reference image: laser c/red channel
                    if (imagesUnaligned[2] != null)
                    {
                        var sourcePtr = (UInt16*)imagesUnaligned[2];
                        var destPtr = (UInt16*)imagesAligned[2];

                        for (int y = 0; y < destImagePixelHeight; y++)
                        {
                            for (int x = 0; x < destImagePixelWidth; x++)
                            {
                                destPtr[y * (destImagePixelWidth + fixDestImageWidthNoDouble) + x] = sourcePtr[(y + positiveDeltaYMax) * (imageWidth + fixSourceImageWidthNoDouble) + (x + positiveDeltaXMax)];
                            }
                        }
                    }
                    for (int i = 0; i < imagesUnaligned.Length; i++)
                    {
                        // skip reference image (already processed).
                        if (imagesUnaligned[i] != null && i != 2)
                        {
                            var sourcePtr = (UInt16*)imagesUnaligned[i];
                            var destPtr = (UInt16*)imagesAligned[i];

                            for (int y = 0; y < destImagePixelHeight; y++)
                            {
                                for (int x = 0; x < destImagePixelWidth; x++)
                                {
                                    destPtr[y * (destImagePixelWidth + fixDestImageWidthNoDouble) + x] =
                                        sourcePtr[(y + positiveDeltaYMax - deltaY[i]) * (imageWidth + fixSourceImageWidthNoDouble) + (x + positiveDeltaXMax - deltaX[i])];
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < imagesUnaligned.Length; i++)
                {
                    if (imagesUnaligned[i] != null && imagesAligned[i] != null)
                    {
                        imagesAligned[i] = imagesUnaligned[i];
                    }
                }
            }
        }

    }
}
