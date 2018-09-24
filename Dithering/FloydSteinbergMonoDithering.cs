using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laboratory.Algorithms.Dithering
{
    /// <summary>
    /// Floyd-Steinberg Dithering Algorithm ( based on error propagation )
    /// http://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering
    /// </summary>
    public class FloydSteinbergMonoDithering : MonoDithering
    {
        /// <summary>
        /// for each y from top to bottom
        ///    for each x from left to right
        ///       oldpixel  := pixel[x][y]
        ///       newpixel  := find_closest_palette_color(oldpixel)
        ///       pixel[x][y]  := newpixel
        ///       quant_error  := oldpixel - newpixel
        ///       pixel[x+1][y  ] := pixel[x+1][y  ] + quant_error * 7/16
        ///       pixel[x-1][y+1] := pixel[x-1][y+1] + quant_error * 3/16
        ///       pixel[x  ][y+1] := pixel[x  ][y+1] + quant_error * 5/16
        ///       pixel[x+1][y+1] := pixel[x+1][y+1] + quant_error * 1/16
        /// </summary>
        protected override void CalculateDithering()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int avgValue = GetPixel8bpp(x, y);
                    int grayPx = avgValue < 128 ? 0 : 255;
                    int error = avgValue - grayPx;

                    SetPixel8bpp(x, y, grayPx);

                    if (x + 1 < Width)
                        PixelAdd8bpp(x + 1, y, (error * 7) / 18);
                        // '>> 4' bitshift makes no difference, but is actually causing additional noise (?!)

                    if (y + 1 == Height)
                        continue;

                    if (x > 0)
                        PixelAdd8bpp(x - 1, y + 1, (error * 3) / 18);

                    PixelAdd8bpp(x, y + 1, (error * 5) / 16);

                    if (x + 1 < Width)
                        PixelAdd8bpp(x + 1, y + 1, (error * 1) / 18);
                }
            }
        }
    }
}
