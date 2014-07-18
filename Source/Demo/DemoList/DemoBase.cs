﻿using System;
using HtmlRenderer.Composers;
using HtmlRenderer.WebDom;

namespace HtmlRenderer.Demo
{
    public abstract class DemoBase
    {
        public void StartDemo(HtmlPanel panel)
        {
            this.OnStartDemo(panel);
        }
        protected virtual void OnStartDemo(HtmlPanel panel)
        {
        }
    }

    static class BridgeHtmlExtension
    {
        //level 1
        public static HtmlElement AddChild(this HtmlElement h, string elementName)
        {
            var newchild = h.OwnerDocument.CreateElement(elementName);
            h.AddChild(newchild);
            return newchild;
        }
        public static HtmlElement AddChild(this HtmlElement h, string elementName, out HtmlElement elemExit)
        {
            var newchild = h.OwnerDocument.CreateElement(elementName);
            h.AddChild(newchild);
            elemExit = newchild;
            return newchild;
        }
        public static void AddTextContent(this HtmlElement h, string text)
        {
            var newTextNode = h.OwnerDocument.CreateTextNode(text.ToCharArray());
            h.AddChild(newTextNode);
        }
        //------------------------------------------------------------------------------

        //level 2
        

    }
}