﻿/*************************************************************************
 *  Copyright (c) 2010 Hu Fei(xiaotie@geblab.com; geblab, www.geblab.com)
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Imaging;

namespace Geb.Image
{
    [StructLayout(LayoutKind.Explicit)]
    public partial struct Argb32
    {
        public static Argb32 WHITE = new Argb32 { Red = 255, Green = 255, Blue = 255, Alpha = 255 };
        public static Argb32 BLACK = new Argb32 { Alpha = 255 };
        public static Argb32 RED = new Argb32 { Red = 255, Alpha = 255 };
        public static Argb32 BLUE = new Argb32 { Blue = 255, Alpha = 255 };
        public static Argb32 GREEN = new Argb32 { Green = 255, Alpha = 255 };
        public static Argb32 EMPTY = new Argb32 { };

        [FieldOffset(0)]
        public Byte Blue;
        [FieldOffset(1)]
        public Byte Green;
        [FieldOffset(2)]
        public Byte Red;
        [FieldOffset(3)]
        public Byte Alpha;

        public Argb32(int red, int green, int blue, int alpha = 255)
        {
            Red = (byte)red;
            Green = (byte)green;
            Blue = (byte)blue;
            Alpha = (byte)alpha;
        }

        public Argb32(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public Byte ToGray()
        {
            return (Byte)(0.299 * Red + 0.587 * Green + 0.114 * Blue);
        }

        public override string ToString()
        {
            return "Argb32 [A="+ Alpha +", R=" + Red.ToString() + ", G=" + Green.ToString() + ", B=" + Blue.ToString() + "]";
        }
    }

    public struct Argb32Ptr
    {
        public unsafe Argb32* Ptr;

        public unsafe Argb32Ptr(Argb32* val)
        {
            Ptr = val;
        }

        public unsafe void Increase()
        {
            Ptr++;
        }
    }

    public struct Argb32Converter : IColorConverter
    {
        public unsafe void Copy(Rgb24* from, void* to, int length)
        {
            UnmanagedImageConverter.ToArgb32(from, (Argb32*)to, length);
        }

        public unsafe void Copy(Argb32* from, void* to, int length)
        {
            UnmanagedImageConverter.Copy((byte*)from, (byte*)to, 4* length);
        }

        public unsafe void Copy(byte* from, void* to, int length)
        {
            UnmanagedImageConverter.ToArgb32(from, (Argb32*)to, length);
        }
    }

    public partial class ImageArgb32 : UnmanagedImage<Argb32>, IEnumerable<Argb32Ptr>
    {
        public unsafe ImageArgb32(Int32 width, Int32 height)
            : base(width, height)
        {
        }

        public ImageArgb32(Bitmap map)
            :base(map)
        {
        }

        public ImageArgb32(String path)
            : base(path)
        {
            
        }

        protected override IColorConverter CreateByteConverter()
        {
            return new Argb32Converter();
        }

        public ImageU8 ToGrayscaleImage()
        {
            return ToGrayscaleImage(0.299, 0.587, 0.114);
        }

        public ImageU8 ToGrayscaleImage(byte transparentColor)
        {
            return ToGrayscaleImage(0.299, 0.587, 0.114, transparentColor);
        }

        public unsafe ImageU8 ToGrayscaleImage(double rCoeff, double gCoeff, double bCoeff, byte transparentColor)
        {
            ImageU8 img = new ImageU8(this.Width, this.Height);
            Argb32* p = Start;
            Byte* to = img.Start;
            Argb32* end = p + Length;

            while (p != end)
            {
                if (p->Alpha == 0)
                {
                    *to = transparentColor;
                }
                else
                {
                    *to = (Byte)(p->Red * rCoeff + p->Green * gCoeff + p->Blue * bCoeff);
                }
                p++;
                to++;
            }

            return img;
        }

        public unsafe ImageU8 ToGrayscaleImage(double rCoeff, double gCoeff, double bCoeff)
        {
            ImageU8 img = new ImageU8(this.Width, this.Height);
            Argb32* p = Start;
            Byte* to = img.Start;
            Argb32* end = p + Length;

            if (Length < 1024)
            {
                while (p != end)
                {
                    *to = (Byte)(p->Red * rCoeff + p->Green * gCoeff + p->Blue * bCoeff);
                    p++;
                    to++;
                }
            }
            else
            {
                int* bCache = stackalloc int[256];
                int* gCache = stackalloc int[256];
                int* rCache = stackalloc int[256];

                const int shift = 1 << 10;
                int rShift = (int)(rCoeff * shift);
                int gShift = (int)(gCoeff * shift);
                int bShift = shift - rShift - gShift;

                int r = 0, g = 0, b = 0;
                for (int i = 0; i < 256; i++)
                {
                    bCache[i] = b;
                    gCache[i] = g;
                    rCache[i] = r;
                    b += bShift;
                    g += gShift;
                    r += rShift;
                }

                while (p != end)
                {
                    *to = (Byte)((bCache[p->Red] + gCache[p->Green] + rCache[p->Red]) >> 10);

                    p++;
                    to++;
                }
            }
            return img;
        }

        public override IImage Clone()
        {
            ImageArgb32 img = new ImageArgb32(this.Width, this.Height);
            img.CloneFrom(this);
            return img;
        }

        protected override System.Drawing.Imaging.PixelFormat GetOutputBitmapPixelFormat()
        {
            return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
        }

        protected override unsafe void ToBitmapCore(byte* src, byte* dst, int width)
        {
            UnmanagedImageConverter.Copy(src, dst, width * 4);
        }

        public unsafe void SetAlpha(byte alpha)
        {
            Argb32* start = (Argb32*)this.Start;
            Argb32* end = start + this.Length;
            while (start != end)
            {
                start->Alpha = alpha;
                start++;
            }
        }

        public unsafe void CombineAlpha(ImageArgb32 src, System.Drawing.Point start, System.Drawing.Rectangle region, System.Drawing.Point destAnchor)
        {
            if (start.X >= src.Width || start.Y >= src.Height) return;
            int startSrcX = Math.Max(0, start.X);
            int startSrcY = Math.Max(0, start.Y);
            int endSrcX = Math.Min(start.X + region.Width, src.Width);
            int endSrcY = Math.Min(start.Y + region.Height, src.Height);
            int offsetX = start.X < 0 ? -start.X : 0;
            int offsetY = start.Y < 0 ? -start.Y : 0;
            offsetX = destAnchor.X + offsetX;
            offsetY = destAnchor.Y + offsetY;
            int startDstX = Math.Max(0, offsetX);
            int startDstY = Math.Max(0, offsetY);
            offsetX = offsetX < 0 ? -offsetX : 0;
            offsetY = offsetY < 0 ? -offsetY : 0;
            startSrcX += offsetX;
            startSrcY += offsetY;
            int endDstX = Math.Min(destAnchor.X + region.Width, this.Width);
            int endDstY = Math.Min(destAnchor.Y + region.Height, this.Height);
            int copyWidth = Math.Min(endSrcX - startSrcX, endDstX - startDstX);
            int copyHeight = Math.Min(endSrcY - startSrcY, endDstY - startDstY);
            if (copyWidth <= 0 || copyHeight <= 0) return;

            int srcWidth = src.Width;
            int dstWidth = this.Width;

            Argb32* srcLine = (Argb32*)(src.StartIntPtr) + srcWidth * startSrcY + startSrcX;
            Argb32* dstLine = this.Start + dstWidth * startDstY + startDstX;
            Argb32* endSrcLine = srcLine + srcWidth * copyHeight;
            while (srcLine < endSrcLine)
            {
                Argb32* pSrc = srcLine;
                Argb32* endPSrc = pSrc + copyWidth;
                Argb32* pDst = dstLine;
                while (pSrc < endPSrc)
                {
                    Argb32 p0 = *pSrc;
                    Argb32 p1 = *pDst;
                    switch (p0.Alpha)
                    {
                        case 255:
                            *pDst = p0;
                            break;
                        case 0:
                        default:
                            break;
                    }
                    pSrc++;
                    pDst++;
                }
                srcLine += srcWidth;
                dstLine += dstWidth;
            }
        }

        public IEnumerator<Argb32Ptr> GetEnumerator()
        {
            return new Argb32Enumerataor(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class Argb32Enumerataor : IEnumerator<Argb32Ptr>
        {
            public unsafe Argb32* _start;
            public unsafe Argb32* _end;
            public Argb32Ptr _current;
            public unsafe Argb32** _pCurrent;

            public unsafe Argb32Enumerataor(UnmanagedImage<Argb32> img)
            {
                _start = (Argb32*)img.StartIntPtr;
                _end = _start + img.Length;
                this._current = new Argb32Ptr(_start);
            }

            public Argb32Ptr Current
            {
                get { return _current; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return _current; }
            }

            public unsafe bool MoveNext()
            {
                //_current.Increase();
                return _current.Ptr++ < _end;
            }

            public unsafe void Reset()
            {
                _current.Ptr = _start;
            }
        }
    }
}
