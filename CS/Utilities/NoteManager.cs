using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows;
using pdftron.PDF;
using pdftron.PDF.Annots;

using UIPoint = System.Windows.Point;
using UIRect = System.Windows.Rect;

using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;





namespace pdftron.PDF.Tools
{
    /// <summary>
    /// Will let us use the Annot class as a key in a dictionary.
    /// </summary>
    public class AnnotComparer : IEqualityComparer<Markup>
    {
        public bool Equals(Markup a1, Markup a2)
        {
            return a1.GetSDFObj().GetObjNum() == a2.GetSDFObj().GetObjNum();
        }

        public int GetHashCode(Markup a)
        {
            return a.GetSDFObj().GetObjNum();
        }
    }



    /// <summary>
    /// This class is the base class for wrapping annotations into selection objects.
    /// This class will use the annotations bounding box and gives the user 8 control points to move around.
    /// 
    /// Create a subclass of this to make a more specific tool for other annotations.
    /// </summary>
    internal class NoteManager
    {
        protected Dictionary<Markup, NoteHost> mActiveNotes;
        protected Dictionary<int, List<Markup>> mNotesPerPage;

        protected PDFViewWPF mPDFView;
        internal PDFViewWPF PDFView
        {
            get { return mPDFView; }

        }
        protected ToolManager mToolManager;
        internal ToolManager ToolManager
        {
            get { return mToolManager; }
        }


        protected Canvas mViewerCanvas;
        protected Canvas mArrowCanvas;
        protected Canvas mNoteCanvas;

        protected PDFViewWPF.PagePresentationMode mCurrentPagePresentationMode = PDFViewWPF.PagePresentationMode.e_single_continuous;



        internal NoteManager(PDFViewWPF view, ToolManager manager)
        {
            mPDFView = view;
            mToolManager = manager;

            mActiveNotes = new Dictionary<Markup, NoteHost>(new AnnotComparer());
            mNotesPerPage = new Dictionary<int, List<Markup>>();

            mArrowCanvas = new Canvas();
            mArrowCanvas.IsHitTestVisible = false;
            mNoteCanvas = new Canvas();

            mViewerCanvas = mToolManager.AnnotationCanvas;
            mViewerCanvas.Children.Add(mArrowCanvas);
            mViewerCanvas.Children.Add(mNoteCanvas);

            mPDFView.CurrentZoomChanged += PDFView_CurrentZoomChanged;
            mPDFView.CurrentPageNumberChanged += PDFView_CurrentPageNumberChanged;
            mPDFView.LayoutChanged += PDFView_LayoutChanged;
        }

        void PDFView_CurrentZoomChanged(PDFViewWPF viewer)
        {
            HandleLayoutChanges();     
        }

        private void PDFView_CurrentPageNumberChanged(PDFViewWPF viewer, int currentPage, int totalPages)
        {
            if (mCurrentPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing
                    || mCurrentPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing_cover
                    || mCurrentPagePresentationMode == PDFViewWPF.PagePresentationMode.e_single_page)
            {
                int activePage1 = mPDFView.GetCurrentPage();
                int activePage2 = -1;
                if (mCurrentPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing)
                {
                    if (activePage1 % 2 == 0)
                    {
                        activePage2 = activePage1 - 1;
                    }
                    else
                    {
                        activePage2 = activePage1 + 1;
                    }
                }
                if (mCurrentPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing_cover)
                {
                    if (activePage1 % 2 == 0)
                    {
                        activePage2 = activePage1 + 1;
                    }
                    else
                    {
                        activePage2 = activePage1 - 1;
                    }
                }
                foreach (NoteHost noteHost in mActiveNotes.Values)
                {
                    if (noteHost.PageNumber == activePage1 || noteHost.PageNumber == activePage2)
                    {
                        noteHost.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        noteHost.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        void PDFView_LayoutChanged(PDFViewWPF viewer)
        {
            HandleLayoutChanges();
        }


        protected void HandleLayoutChanges()
        {
            int activePage1 = -1;
            int activePage2 = -1;

            // if page presentation mode changes, we need to hide or show some notes
            PDFViewWPF.PagePresentationMode newPagePresentationMode = mPDFView.GetPagePresentationMode();
            if (newPagePresentationMode != mCurrentPagePresentationMode)
            {
                // figure out the pages on screen
                activePage1 = mPDFView.GetCurrentPage();
                if (newPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing)
                {
                    if (activePage1 % 2 == 0)
                    {
                        activePage2 = activePage1 - 1;
                    }
                    else
                    {
                        activePage2 = activePage1 + 1;
                    }
                }
                if (newPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing_cover)
                {
                    if (activePage1 % 2 == 0)
                    {
                        activePage2 = activePage1 + 1;
                    }
                    else
                    {
                        activePage2 = activePage1 - 1;
                    }
                }

                // Hide notes not on the page
                if (newPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing
                    || newPagePresentationMode == PDFViewWPF.PagePresentationMode.e_facing_cover
                    || newPagePresentationMode == PDFViewWPF.PagePresentationMode.e_single_page)
                {
                    foreach (NoteHost noteHost in mActiveNotes.Values)
                    {
                        if (noteHost.PageNumber == activePage1 || noteHost.PageNumber == activePage2)
                        {
                            noteHost.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            noteHost.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    foreach (NoteHost noteHost in mActiveNotes.Values)
                    {
                        noteHost.Visibility = Visibility.Visible;
                    }
                }
            }
            mCurrentPagePresentationMode = newPagePresentationMode;

            // update position of all notes.
            foreach (NoteHost noteHost in mActiveNotes.Values)
            {
                noteHost.RepositionAfterZoom();
            }
        }



        /// <summary>
        ///  When an annotation is dragged around, this function should be called to update the arrow connected to it.
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="rect"></param>
        //internal void AnnotationMoved(Annot markup, PDFRect rect)
        //{
        //    if (mActiveNotes.ContainsKey(markup))
        //    {
        //        mActiveNotes[markup].AnnotationMoving(rect);
        //    }
        //}

        /// <summary>
        /// When an annotation is dragged around, this function should be called to update the arrow connected to it.
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="targetPoints">A list of target points in canvas space.</param>
        internal void AnnotationMoved(Markup markup, IList<UIPoint> targetPoints)
        {
            if (markup != null && mActiveNotes.ContainsKey(markup))
            {
                mActiveNotes[markup].AnnotationMoving(targetPoints);
            }
        }

        /// <summary>
        /// Will create a new note and pup it on top of the note Canvas
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="pageNumber"></param>
        internal void OpenNote(Markup markup, int pageNumber, IList<UIPoint> targetPoints)
        {
            if (mActiveNotes.ContainsKey(markup))
            {
                mActiveNotes[markup].Activate();
                return;
            }
            NoteHost nh = new NoteHost(this, mArrowCanvas, mNoteCanvas, markup, pageNumber, targetPoints);
            mActiveNotes.Add(markup, nh);
            if (!mNotesPerPage.ContainsKey(pageNumber))
            {
                mNotesPerPage.Add(pageNumber, new List<Markup>());
            }
            mNotesPerPage[pageNumber].Add(markup);
        }

        /// <summary>
        /// Will close the note associated with markup
        /// </summary>
        /// <param name="markup"></param>
        internal void CloseNote(Markup markup)
        {
            if (!mActiveNotes.ContainsKey(markup))
            {
                return;
            }
            int pgnm = mActiveNotes[markup].PageNumber;
            mActiveNotes[markup].RemoveFromCanvas();
            mActiveNotes.Remove(markup);

            List<Markup> aList = mNotesPerPage[pgnm];
            aList.Remove(markup);
            
            if (aList.Count == 0)
            {
                mNotesPerPage.Remove(pgnm);
            }
        }
    }
}