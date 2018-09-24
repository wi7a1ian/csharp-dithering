using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Laboratory.Algorithms.Dithering
{
    /// <summary>
    /// Base class for dithering  algorithms.
    /// </summary>
	public abstract class MonoDithering
	{
        private Bitmap _srcBitmap;
        private Bitmap _dstBitmap;
        private byte[] _dstRawBytes; //byte* _rawBytes; // same performance using pointers...

        protected int Stride;
        protected int Width;
        protected int Height;

        protected abstract void CalculateDithering();

		public Bitmap Dither(Bitmap srcImage)
		{
            if (srcImage == null)
                throw new ArgumentNullException("srcImage");

            _srcBitmap = srcImage;

            _dstBitmap = new Bitmap(_srcBitmap.Width, _srcBitmap.Height, PixelFormat.Format8bppIndexed);
            _dstBitmap.SetResolution(_srcBitmap.HorizontalResolution, _srcBitmap.VerticalResolution);
            Width = _dstBitmap.Width;
            Height = _dstBitmap.Height;

            Stride = GetRawBytes(_dstBitmap, out _dstRawBytes);
            
            NormalizeBitmap();
            CalculateDithering();

            SetRawBytes(_dstBitmap, _dstRawBytes);

            _srcBitmap = null;
			return _dstBitmap;
		}

        public void SaveAsMonoTiff(Bitmap image, string tiffFile)
        {
            ImageCodecInfo myImageCodecInfo = GetEncoderInfo("image/tiff");
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionCCITT4);

            image.Save(tiffFile, myImageCodecInfo, myEncoderParameters);
        }

        /// <summary>
        /// http://tech.pro/tutorial/660/csharp-tutorial-convert-a-color-image-to-grayscale
        /// http://bobpowell.net/lockingbits.aspx
        /// http://csharpexamples.com/tag/parallel-bitmap-processing/
        /// </summary>
        private void NormalizeBitmap()
        {
            int bytesPerPixel;
            bool isLE = BitConverter.IsLittleEndian;
            ParallelOptions parOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount // May cause context switching
            };

            // Check input bitmap's pixel format
            switch (_srcBitmap.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    bytesPerPixel = 1;
                    break;
                //case PixelFormat.Format16bppGrayScale:
                //    bytesPerPixel = 2;
                //    break;
                case PixelFormat.Format24bppRgb: 
                    bytesPerPixel = 3; 
                    break;
                case PixelFormat.Format32bppArgb:
                    bytesPerPixel = 4;
                    break;
                case PixelFormat.Format32bppRgb:
                    bytesPerPixel = 4;
                    break;
                default: 
                    throw new InvalidOperationException("Image format not supported");
            }

            byte[] srcRawBytes;
            int srcStride = GetRawBytes(_srcBitmap, out srcRawBytes);

            // Perf. improvement by splitting cases ~500ms
            if (bytesPerPixel == 1)
            {
                Array.Copy(srcRawBytes, _dstRawBytes, srcRawBytes.Length);
            }
            else if (bytesPerPixel == 3)  
            {
                // Perf. improvement ~100ms (thread safe operation)
                Parallel.For(0, Height, /*parOptions,*/ y =>
                //for (int y = 0; y < Height; y++)
                {
                    for (int xi = 0, xo = 0; xo < Width; xi += 3, ++xo)
                    {
                        int grayVal = 0;
                        int index = y * srcStride + xi;

                        // Convert the pixel to it's luminance using the formula:
                        // L = .299*R + .587*G + .114*B
                        // On a little-endian machine the byte order is bb gg rr.

                        if (isLE) // Perf. improvement ~100ms
                        {
                            grayVal = (byte)(int)
                                    (0.299f * srcRawBytes[index + 2] +  // R
                                     0.587f * srcRawBytes[index + 1] +  // G
                                     0.114f * srcRawBytes[index]);      // B
                        }
                        else
                        {
                            grayVal = (byte)(int)
                                    (0.299f * srcRawBytes[index] +      // R
                                     0.587f * srcRawBytes[index + 1] +  // G
                                     0.114f * srcRawBytes[index + 2]);  // B
                        }

                        SetPixel8bpp(xo, y, grayVal);
                    }
                }); // Parallel.For
            }
            else if(bytesPerPixel == 4)
            {
                // Perf. improvement ~100ms (thread safe operation)
                Parallel.For(0, Height, /*parOptions,*/ y =>
                //for (int y = 0; y < Height; y++)
                {
                    for (int xi = 0, xo = 0; xo < Width; xi += 4, ++xo)
                    {
                        int grayVal = 0;
                        int index = y * srcStride + xi;

                        // Convert the pixel to it's luminance using the formula:
                        // L = alpha * (.299*R + .587*G + .114*B)
                        // On a little-endian machine the byte order is bb gg rr aa.

                        if (isLE) // Perf. improvement ~100ms
                        {
                            grayVal = (byte)(int)
                                ((srcRawBytes[index + 3] / 255.0f) *    // A
                                    (0.299f * srcRawBytes[index + 2] +  // R
                                     0.587f * srcRawBytes[index + 1] +  // G
                                     0.114f * srcRawBytes[index]));     // B
                        }
                        else
                        {
                            grayVal = (byte)(int)
                                    ((srcRawBytes[index] / 255.0f) *    // A
                                    (0.299f * srcRawBytes[index + 1] +  // R
                                     0.587f * srcRawBytes[index + 2] +  // G
                                     0.114f * srcRawBytes[index + 3])); // B
                        }

                        SetPixel8bpp(xo, y, grayVal);
                    }
                }); // Parallel.For
            }
        }

        
        private int GetRawBytes(Bitmap image, out byte[] rawBytes)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            rawBytes = new byte[image.Height * bitmapData.Stride];

            // Copy raw bytes
            Marshal.Copy(bitmapData.Scan0, rawBytes, 0, rawBytes.Length);
            
            image.UnlockBits(bitmapData);
            return bitmapData.Stride;
        }

        private void SetRawBytes(Bitmap image, byte[] rawBytes)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            if (rawBytes == null)
                throw new ArgumentNullException("rawBytes");

            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, image.PixelFormat);
            var stride = bitmapData.Stride;

            if (rawBytes.Length != image.Height * stride)
                throw new ArgumentException("Raw bytes do not fit in image size.");

            Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
            image.UnlockBits(bitmapData);
        }

        #region pixel manipulation

        private void SetPixel1bpp(int y, int x, bool isWhite)
        {
            int index = y * Stride + (x >> 3);
            byte mask = (byte)(0x80 >> (x & 0x7));

            if (isWhite)
                _dstRawBytes[index] |= mask;
            else
                _dstRawBytes[index] &= (byte)~mask; //(byte)(mask ^ 0xff); // 
        }

        private bool GetPixel1bpp(int y, int x)
        {
            int index = y * Stride + (x >> 3);
            byte mask = (byte)(0x80 >> (x & 0x7));
            int n = _dstRawBytes[index] & mask;

            return n > 0 ? true : false;
        }

		protected void SetPixel8bpp(int x, int y, int value)
		{
			int index = y * Stride + x;
			_dstRawBytes[index] = (byte)value;
		}

        protected void SetPixel8bpp(int index, int value)
        {
            _dstRawBytes[index] = (byte)value;
        }

        protected byte GetPixel8bpp(int x, int y)
		{
			int index = y * Stride + x;
			return _dstRawBytes[index];
		}

        protected byte GetPixel8bpp(int index)
        {
            return _dstRawBytes[index];
        }

        protected void PixelAdd8bpp(int x, int y, int value)
        {
            int index = y * Stride + x;
            _dstRawBytes[index] = (byte)PixelAddTruncate8bpp(
                _dstRawBytes[index], value
            );
        }

        protected void PixelAdd8bpp(int index, int value)
        {
            SetPixel8bpp(index, PixelAddTruncate8bpp(
                GetPixel8bpp(index), value
            ));
        }

        protected int PixelAddTruncate8bpp(int a, int b)
		{
			if (a + b < 0)
				return 0;
			else if (a + b > 255)
				return 255;
			else
				return (a + b);
		}

        protected int PixelClamp8bpp(int value)
        {
            if (value < 0)
                return 0;
            else if (value > 255)
                return 255;
            else
                return value;
        }

        #endregion pixel manipulation

        #region utilities

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        #endregion utilities

    }
}
