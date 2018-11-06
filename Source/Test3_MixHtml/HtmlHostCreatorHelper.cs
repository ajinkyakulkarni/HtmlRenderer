﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;

namespace LayoutFarm
{
    //YOUR IMPLEMENTATION ...

    public static class HtmlHostCreatorHelper
    {
        public static HtmlBoxes.HtmlHost CreateHtmlHost(AppHost appHost,
            EventHandler<ContentManagers.ImageRequestEventArgs> imageReqHandler,
            EventHandler<ContentManagers.TextRequestEventArgs> textReq)
        {
            List<HtmlBoxes.HtmlVisualRoot> htmlVisualRootUpdateList = new List<HtmlBoxes.HtmlVisualRoot>();

            var config = new HtmlBoxes.HtmlHostCreationConfig()
            {
                RootGraphic = appHost.RootGfx,
                TextService = appHost.RootGfx.TextServices
            };

            //1.
            HtmlBoxes.HtmlHost htmlhost = new HtmlBoxes.HtmlHost(config);  //create html host with config 
            appHost.RootGfx.ClearingBeforeRender += (s, e) =>
            {
                //
                htmlhost.ClearUpdateWaitingCssBoxes();
                //
                int j = htmlVisualRootUpdateList.Count;
                for (int i = 0; i < j; ++i)
                {

                    HtmlBoxes.HtmlVisualRoot htmlVisualRoot = htmlVisualRootUpdateList[i];
                    htmlVisualRoot.RefreshDomIfNeed();
                    htmlVisualRoot.IsInUpdateQueue = false;
                }
                htmlVisualRootUpdateList.Clear();
            };
            //2.
            htmlhost.RegisterCssBoxGenerator(new LayoutFarm.CustomWidgets.MyCustomCssBoxGenerator(htmlhost));
            //3.
            htmlhost.AttachEssentailHandlers(imageReqHandler, textReq);
            //4.
            htmlhost.SetHtmlVisualRootUpdateHandler(htmlVisualRoot =>
            {
                if (!htmlVisualRoot.IsInUpdateQueue)
                {
                    htmlVisualRoot.IsInUpdateQueue = true;
                    htmlVisualRootUpdateList.Add(htmlVisualRoot);
                }
            });

            //-----------------------------------------------------------------

            if (PaintLab.Svg.VgResourceIO.VgImgIOHandler == null)
            {
                var imgLoadingQ = new ContentManagers.ImageLoadingQueueManager();
                imgLoadingQ.AskForImage += (s, e) =>
                {
                    //check loading policy here  
                    //
                    e.SetResultImage(LoadImgForSvgElem(e.ImagSource));
                };
                PaintLab.Svg.VgResourceIO.VgImgIOHandler = (LayoutFarm.ImageBinder binder, PaintLab.Svg.SvgRenderElement imgRun, object requestFrom) =>
                {
                    imgLoadingQ.AddRequestImage(binder);
                };
            }

            return htmlhost;
        }
        static PixelFarm.Drawing.Image LoadImgForSvgElem(string imgName)
        {

            if (!System.IO.File.Exists(imgName))
            {
                return null;
            }

            using (System.Drawing.Bitmap gdiBmp = new System.Drawing.Bitmap(imgName))
            {
                int w = gdiBmp.Width;
                int h = gdiBmp.Height;

                var bmpData = gdiBmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                //TODO: copy from bmpData to internal buffer of actual bitmap
                int[] buffer = new int[w * h];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, buffer, 0, w * h);
                gdiBmp.UnlockBits(bmpData);

                return PixelFarm.CpuBlit.ActualBitmap.CreateFromCopy(gdiBmp.Width, gdiBmp.Height, buffer);
            }

        }
    }
}