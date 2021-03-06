﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using System.Text;
using PixelFarm.Drawing;
using LayoutFarm.RenderBoxes;
namespace LayoutFarm.UI
{
    class RenderElementEventPortal : IEventPortal
    {
        //current hit chain         
        Stack<HitChain> _hitChainStack = new Stack<HitChain>();
#if DEBUG
        int dbugMsgChainVersion;
#endif
        readonly RenderElement topRenderElement;
        public RenderElementEventPortal(RenderElement topRenderElement)
        {
            this.topRenderElement = topRenderElement;
#if DEBUG
            dbugRootGraphics = (MyRootGraphic)topRenderElement.Root;
#endif
        }

        HitChain GetFreeHitChain()
        {
            if (_hitChainStack.Count > 0)
            {
                return _hitChainStack.Pop();
            }
            else
            {
                return new HitChain();
            }
        }
        void ReleaseHitChain(HitChain hitChain)
        {
            hitChain.ClearAll();
            _hitChainStack.Push(hitChain);
        }

        static void SetEventOrigin(UIEventArgs e, HitChain hitChain)
        {
            int count = hitChain.Count;
            if (count > 0)
            {
                var hitInfo = hitChain.GetHitInfo(count - 1);
                e.ExactHitObject = hitInfo.HitElem;
            }
        }




        void HitTestCoreWithChain(HitChain hitPointChain, int x, int y)
        {
            hitPointChain.ClearAll();
            hitPointChain.SetStartTestPoint(x, y);
            RenderElement commonElement = this.topRenderElement;
            commonElement.HitTestCore(hitPointChain);
        }

        void IEventPortal.PortalMouseWheel(UIMouseEventArgs e)
        {
        }
        void IEventPortal.PortalMouseDown(UIMouseEventArgs e)
        {
#if DEBUG
            if (this.dbugRootGraphics.dbugEnableGraphicInvalidateTrace)
            {
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("================");
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("MOUSEDOWN");
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("================");
            }
            dbugMsgChainVersion = 1;
            int local_msgVersion = 1; 
#endif


            HitChain hitPointChain = GetFreeHitChain();
            HitTestCoreWithChain(hitPointChain, e.X, e.Y);
            int hitCount = hitPointChain.Count;

            if (hitCount > 0)
            {
                //------------------------------
                //1. origin object 
                SetEventOrigin(e, hitPointChain);
                //------------------------------ 
                var prevMouseDownElement = e.PreviousMouseDown;
                IUIEventListener currentMouseDown = null;
                //portal                
                ForEachOnlyEventPortalBubbleUp(e, hitPointChain, (portal) =>
                {
                    portal.PortalMouseDown(e);
                    //*****
                    currentMouseDown = e.CurrentContextElement;
                    return true;
                });
                //------------------------------
                //use events
                if (!e.CancelBubbling)
                {
                    e.CurrentContextElement = currentMouseDown = null; //clear 
                    ForEachEventListenerBubbleUp(e, hitPointChain, (listener) =>
                    {
                        currentMouseDown = listener;
                        listener.ListenMouseDown(e);
                        //------------------------------------------------------- 
                        bool cancelMouseBubbling = e.CancelBubbling;
                        if (prevMouseDownElement != null &&
                            prevMouseDownElement != listener)
                        {
                            prevMouseDownElement.ListenLostMouseFocus(e);
                            prevMouseDownElement = null;//clear
                        }
                        //------------------------------------------------------- 
                        return e.CancelBubbling || !listener.BypassAllMouseEvents;

                    });
                }

                if (prevMouseDownElement != currentMouseDown &&
                    prevMouseDownElement != null)
                {
                    prevMouseDownElement.ListenLostMouseFocus(e);
                    prevMouseDownElement = null;
                }
            }
            //---------------------------------------------------------------

#if DEBUG
            RootGraphic visualroot = this.dbugRootGraphics;
            if (visualroot.dbug_RecordHitChain)
            {
                visualroot.dbug_rootHitChainMsg.Clear();
                HitInfo hitInfo;
                for (int i = hitPointChain.Count - 1; i >= 0; --i)
                {
                    hitInfo = hitPointChain.GetHitInfo(i);
                    RenderElement ve = hitInfo.HitElemAsRenderElement;
                    if (ve != null)
                    {
                        ve.dbug_WriteOwnerLayerInfo(visualroot, i);
                        ve.dbug_WriteOwnerLineInfo(visualroot, i);
                        string hit_info = new string('.', i) + " [" + i + "] "
                            + "(" + hitInfo.point.X + "," + hitInfo.point.Y + ") "
                            + ve.dbug_FullElementDescription();
                        visualroot.dbug_rootHitChainMsg.AddLast(new dbugLayoutMsg(ve, hit_info));
                    }
                }
            }
#endif
            ReleaseHitChain(hitPointChain);
            e.StopPropagation();
#if DEBUG
            if (local_msgVersion != dbugMsgChainVersion)
            {
                return;
            }
            visualroot.dbugHitTracker.Write("stop-mousedown");
            visualroot.dbugHitTracker.Play = false;
#endif
        }
        void IEventPortal.PortalMouseMove(UIMouseEventArgs e)
        {
            HitChain hitPointChain = GetFreeHitChain();
            HitTestCoreWithChain(hitPointChain, e.X, e.Y);
            SetEventOrigin(e, hitPointChain);
            //-------------------------------------------------------
            ForEachOnlyEventPortalBubbleUp(e, hitPointChain, (portal) =>
            {
                portal.PortalMouseMove(e);
                return true;
            });
            //-------------------------------------------------------  
            if (!e.CancelBubbling)
            {
                bool foundSomeHit = false;
                ForEachEventListenerBubbleUp(e, hitPointChain, (listener) =>
                {
                    foundSomeHit = true;
                    bool isFirstMouseEnter = false;
                    if (e.CurrentMouseActive != null &&
                        e.CurrentMouseActive != listener)
                    {
                        e.CurrentMouseActive.ListenMouseLeave(e);
                        isFirstMouseEnter = true;
                    }

                    if (!e.IsCanceled)
                    {
                        e.CurrentMouseActive = listener;
                        e.IsFirstMouseEnter = isFirstMouseEnter;
                        e.CurrentMouseActive.ListenMouseMove(e);
                        e.IsFirstMouseEnter = false;
                    }

                    return true;//stop
                });
                if (!foundSomeHit && e.CurrentMouseActive != null)
                {
                    e.CurrentMouseActive.ListenMouseLeave(e);
                    if (!e.IsCanceled)
                    {
                        e.CurrentMouseActive = null;
                    }
                }
            }
            ReleaseHitChain(hitPointChain);
            e.StopPropagation();
        }
        void IEventPortal.PortalGotFocus(UIFocusEventArgs e)
        {
        }
        void IEventPortal.PortalLostFocus(UIFocusEventArgs e)
        {
        }
        void IEventPortal.PortalMouseUp(UIMouseEventArgs e)
        {
#if DEBUG

            if (this.dbugRootGraphics.dbugEnableGraphicInvalidateTrace)
            {
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("================");
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("MOUSEUP");
                this.dbugRootGraphics.dbugGraphicInvalidateTracer.WriteInfo("================");
            }
#endif

            HitChain hitPointChain = GetFreeHitChain();
            HitTestCoreWithChain(hitPointChain, e.X, e.Y);
            int hitCount = hitPointChain.Count;
            if (hitCount > 0)
            {
                SetEventOrigin(e, hitPointChain);
                //--------------------------------------------------------------- 
                ForEachOnlyEventPortalBubbleUp(e, hitPointChain, (portal) =>
                {
                    portal.PortalMouseUp(e);
                    return true;
                });
                //---------------------------------------------------------------
                if (!e.CancelBubbling)
                {
                    ForEachEventListenerBubbleUp(e, hitPointChain, (listener) =>
                    {
                        listener.ListenMouseUp(e);
                        return true;

                    });
                }
                //---------------------------------------------------------------
                if (!e.CancelBubbling)
                {
                    if (e.IsAlsoDoubleClick)
                    {
                        ForEachEventListenerBubbleUp(e, hitPointChain, listener =>
                        {
                            listener.ListenMouseDoubleClick(e);
                            //------------------------------------------------------- 
                            //retrun true to stop this loop (no further bubble up)
                            //return false to bubble this to upper control       
                            return e.CancelBubbling || !listener.BypassAllMouseEvents;
                        });
                    }
                    else
                    {
                        ForEachEventListenerBubbleUp(e, hitPointChain, listener =>
                        {
                            listener.ListenMouseClick(e);
                            return e.CancelBubbling || !listener.BypassAllMouseEvents;
                        });
                    }
                }
            }
            ReleaseHitChain(hitPointChain);
            e.StopPropagation();
        }
        void IEventPortal.PortalKeyDown(UIKeyEventArgs e)
        {
        }
        void IEventPortal.PortalKeyUp(UIKeyEventArgs e)
        {
        }
        void IEventPortal.PortalKeyPress(UIKeyEventArgs e)
        {
        }
        bool IEventPortal.PortalProcessDialogKey(UIKeyEventArgs e)
        {
            return false;
        }

        //===================================================================
        delegate bool EventPortalAction(IEventPortal evPortal);
        delegate bool EventListenerAction(IUIEventListener listener);
        static void ForEachOnlyEventPortalBubbleUp(UIEventArgs e, HitChain hitPointChain, EventPortalAction eventPortalAction)
        {
            for (int i = hitPointChain.Count - 1; i >= 0; --i)
            {
                HitInfo hitPoint = hitPointChain.GetHitInfo(i);
                object currentHitElement = hitPoint.HitElemAsRenderElement.GetController();
                IEventPortal eventPortal = currentHitElement as IEventPortal;
                if (eventPortal != null)
                {
                    var ppp = hitPoint.point;
                    e.CurrentContextElement = currentHitElement as IUIEventListener;
                    e.SetLocation(ppp.X, ppp.Y);
                    if (eventPortalAction(eventPortal))
                    {
                        return;
                    }
                }
            }
        }
        static void ForEachEventListenerBubbleUp(UIEventArgs e, HitChain hitPointChain, EventListenerAction listenerAction)
        {
            HitInfo hitInfo;
            for (int i = hitPointChain.Count - 1; i >= 0; --i)
            {
                hitInfo = hitPointChain.GetHitInfo(i);
                IUIEventListener listener = hitInfo.HitElemAsRenderElement.GetController() as IUIEventListener;
                if (listener != null)
                {
                    if (e.SourceHitElement == null)
                    {
                        e.SourceHitElement = listener;
                    }

                    Point hitPoint = hitInfo.point;
                    e.SetLocation(hitPoint.X, hitPoint.Y);
                    e.CurrentContextElement = listener;
                    if (listenerAction(listener))
                    {
                        return;
                    }
                }
            }
        }


        //        public override void OnDragStart(UIMouseEventArgs e)
        //        {

        //#if DEBUG
        //            if (this.dbugRootGraphic.dbugEnableGraphicInvalidateTrace)
        //            {
        //                this.dbugRootGraphic.dbugGraphicInvalidateTracer.WriteInfo("================");
        //                this.dbugRootGraphic.dbugGraphicInvalidateTracer.WriteInfo("START_DRAG");
        //                this.dbugRootGraphic.dbugGraphicInvalidateTracer.WriteInfo("================");
        //            }
        //#endif

        //            HitTestCoreWithPrevChainHint(
        //              hitPointChain.LastestRootX,
        //              hitPointChain.LastestRootY);

        //            DisableGraphicOutputFlush = true;
        //            this.currentDragElem = null;

        //            //-----------------------------------------------------------------------

        //            ForEachEventListenerPreviewBubbleUp(this.hitPointChain, (hitobj, listener) =>
        //            {
        //                listener.PortalMouseMove(e);
        //                return true;
        //            });

        //            //-----------------------------------------------------------------------

        //            ForEachEventListenerBubbleUp(this.hitPointChain, (hit, listener) =>
        //            {
        //                currentDragElem = listener;
        //                listener.ListenDragEvent(UIDragEventName.DragStart, e);
        //                return true;
        //            });
        //            DisableGraphicOutputFlush = false;
        //            FlushAccumGraphicUpdate();

        //            hitPointChain.SwapHitChain();
        //        }
        //        public override void OnDrag(UIMouseEventArgs e)
        //        {
        //            if (currentDragElem == null)
        //            {
        //                return;
        //            }

        //#if DEBUG
        //            this.dbugRootGraphic.dbugEventIsDragging = true;
        //#endif

        //            //if (currentDragingElement == null)
        //            //{

        //            //    return;
        //            //}
        //            //else
        //            //{
        //            //}

        //            //--------------

        //            DisableGraphicOutputFlush = true;

        //            currentDragElem.ListenDragEvent(UIDragEventName.Dragging, e);

        //            DisableGraphicOutputFlush = false;
        //            FlushAccumGraphicUpdate();

        //            //Point globalDragingElementLocation = currentDragingElement.GetGlobalLocation();
        //            //e.TranslateCanvasOrigin(globalDragingElementLocation);
        //            //e.SourceHitElement = currentDragingElement;
        //            //Point dragPoint = hitPointChain.PrevHitPoint;
        //            //dragPoint.Offset(currentXDistanceFromDragPoint, currentYDistanceFromDragPoint);
        //            //e.Location = dragPoint;
        //            //e.DragingElement = currentDragingElement;

        //            //IEventListener ui = currentDragingElement.GetController() as IEventListener;
        //            //if (ui != null)
        //            //{
        //            //    ui.ListenDragEvent(UIDragEventName.Dragging, e);
        //            //}
        //            //e.TranslateCanvasOriginBack();


        //        }


        //        public override void OnDragStop(UIMouseEventArgs e)
        //        {

        //            if (currentDragElem == null)
        //            {
        //                return;
        //            }
        //#if DEBUG
        //            this.dbugRootGraphic.dbugEventIsDragging = false;
        //#endif

        //            DisableGraphicOutputFlush = true;

        //            currentDragElem.ListenDragEvent(UIDragEventName.DragStop, e);

        //            DisableGraphicOutputFlush = false;
        //            FlushAccumGraphicUpdate();

        //            //if (currentDragingElement == null)
        //            //{
        //            //    return;
        //            //}

        //            //DisableGraphicOutputFlush = true;

        //            //Point globalDragingElementLocation = currentDragingElement.GetGlobalLocation();
        //            //e.TranslateCanvasOrigin(globalDragingElementLocation);

        //            //Point dragPoint = hitPointChain.PrevHitPoint;
        //            //dragPoint.Offset(currentXDistanceFromDragPoint, currentYDistanceFromDragPoint);
        //            //e.Location = dragPoint;

        //            //e.SourceHitElement = currentDragingElement;
        //            //var script = currentDragingElement.GetController() as IEventListener;
        //            //if (script != null)
        //            //{
        //            //    script.ListenDragEvent(UIDragEventName.DragStop, e);
        //            //}

        //            //e.TranslateCanvasOriginBack();

        //            //UIMouseEventArgs d_eventArg = new UIMouseEventArgs();
        //            //if (hitPointChain.DragHitElementCount > 0)
        //            //{
        //            //    ForEachDraggingObjects(this.hitPointChain, (hitobj, listener) =>
        //            //    {
        //            //        //d_eventArg.TranslateCanvasOrigin(globalLocation);
        //            //        //d_eventArg.SourceHitElement = elem;
        //            //        //d_eventArg.DragingElement = currentDragingElement;

        //            //        //var script2 = elem.GetController();
        //            //        //if (script2 != null)
        //            //        //{
        //            //        //}

        //            //        //d_eventArg.TranslateCanvasOriginBack();
        //            //        return true;
        //            //    });
        //            //    //foreach (RenderElement elem in hitPointChain.GetDragHitElementIter())
        //            //    //{
        //            //    //    Point globalLocation = elem.GetGlobalLocation();
        //            //    //    d_eventArg.TranslateCanvasOrigin(globalLocation);
        //            //    //    d_eventArg.SourceHitElement = elem;
        //            //    //    d_eventArg.DragingElement = currentDragingElement;

        //            //    //    var script2 = elem.GetController();
        //            //    //    if (script2 != null)
        //            //    //    {
        //            //    //    }

        //            //    //    d_eventArg.TranslateCanvasOriginBack();
        //            //    //}
        //            //} 
        //            DisableGraphicOutputFlush = false;
        //            FlushAccumGraphicUpdate();
        //        }


        void BroadcastDragHitEvents(UIMouseEventArgs e)
        {
            //Point globalDragingElementLocation = currentDragingElement.GetGlobalLocation();
            //Rectangle dragRect = currentDragingElement.GetGlobalRect();

            //VisualDrawingChain drawingChain = this.WinRootPrepareRenderingChain(dragRect);

            //List<RenderElement> selVisualElements = drawingChain.selectedVisualElements;
            //int j = selVisualElements.Count;
            //LinkedList<RenderElement> underlyingElements = new LinkedList<RenderElement>();
            //for (int i = j - 1; i > -1; --i)
            //{

            //    if (selVisualElements[i].ListeningDragEvent)
            //    {
            //        underlyingElements.AddLast(selVisualElements[i]);
            //    }
            //}

            //if (underlyingElements.Count > 0)
            //{
            //    foreach (RenderElement underlyingUI in underlyingElements)
            //    {

            //        if (underlyingUI.IsDragedOver)
            //        {   
            //            hitPointChain.RemoveDragHitElement(underlyingUI);
            //            underlyingUI.IsDragedOver = false;
            //        }
            //    }
            //}
            //UIMouseEventArgs d_eventArg = UIMouseEventArgs.GetFreeDragEventArgs();

            //if (hitPointChain.DragHitElementCount > 0)
            //{
            //    foreach (RenderElement elem in hitPointChain.GetDragHitElementIter())
            //    {
            //        Point globalLocation = elem.GetGlobalLocation();
            //        d_eventArg.TranslateCanvasOrigin(globalLocation);
            //        d_eventArg.SourceVisualElement = elem;
            //        var script = elem.GetController();
            //        if (script != null)
            //        {
            //        }
            //        d_eventArg.TranslateCanvasOriginBack();
            //    }
            //}
            //hitPointChain.ClearDragHitElements();

            //foreach (RenderElement underlyingUI in underlyingElements)
            //{

            //    hitPointChain.AddDragHitElement(underlyingUI);
            //    if (underlyingUI.IsDragedOver)
            //    {
            //        Point globalLocation = underlyingUI.GetGlobalLocation();
            //        d_eventArg.TranslateCanvasOrigin(globalLocation);
            //        d_eventArg.SourceVisualElement = underlyingUI;

            //        var script = underlyingUI.GetController();
            //        if (script != null)
            //        {
            //        }

            //        d_eventArg.TranslateCanvasOriginBack();
            //    }
            //    else
            //    {
            //        underlyingUI.IsDragedOver = true;
            //        Point globalLocation = underlyingUI.GetGlobalLocation();
            //        d_eventArg.TranslateCanvasOrigin(globalLocation);
            //        d_eventArg.SourceVisualElement = underlyingUI;

            //        var script = underlyingUI.GetController();
            //        if (script != null)
            //        {
            //        }

            //        d_eventArg.TranslateCanvasOriginBack();
            //    }
            //}
            //UIMouseEventArgs.ReleaseEventArgs(d_eventArg);
        }

#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
        MyRootGraphic dbugRootGfx;
        MyRootGraphic dbugRootGraphics
        {
            get { return dbugRootGfx; }
            set
            {
                this.dbugRootGfx = value;
            }
        }
#endif
    }
}