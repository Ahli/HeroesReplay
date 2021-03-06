﻿using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.Drawing;

namespace HeroesReplay.Core.Processes
{
    public class CaptureBitBlt : CaptureStrategy
    {
        public CaptureBitBlt(ILogger<CaptureBitBlt> logger) : base(logger)
        {

        }

        public override Bitmap Capture(IntPtr handle, Rectangle? region = null)
        {
            try
            {
                DateTime start = DateTime.Now;

                Rectangle bounds = region ?? GetDimensions(handle);

                using (Graphics source = Graphics.FromHwnd(handle))
                {
                    Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, source);

                    using (Graphics destination = Graphics.FromImage(bitmap))
                    {
                        IntPtr deviceContextSource = source.GetHdc();
                        IntPtr deviceContextDestination = destination.GetHdc();

                        Gdi32.BitBlt(
                            deviceContextDestination, 0, 0, bounds.Width, bounds.Height,
                            deviceContextSource, bounds.Left, bounds.Top,
                            (int)TernaryRasterOperations.SRCCOPY);

                        source.ReleaseHdc(deviceContextSource);
                        destination.ReleaseHdc(deviceContextDestination);
                    }

                    Logger.LogDebug("capture time: " + (DateTime.Now - start));

                    return bitmap;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not capture handle");
                throw;
            }
        }
    }
}