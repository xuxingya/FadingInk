using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pdftron.PDF;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using pdftron.SDF;

using UIPoint = System.Windows.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;



namespace pdftron.PDF.Tools
{

    public class FormFill : Pan
    {
        
        protected Annot mActiveWidget;
        protected int mWidgetPageNumber;
        protected Field mField;
        protected Field.Type mFieldType;

        protected bool mClickingOnWidget;
        protected PDFRect mWidgetRect;
        protected Rectangle mClickRectangle;

        protected bool mAnnotIsEdited = false;

        protected bool mWidgetHandledKeyDown = false;

        //////////////////////////////////////////////////////////////////////////
        // Text Fields
        protected bool mIsMultiLine;
        protected Border mTextPopupBorder;
        protected TextBox mTextBox;
        protected PasswordBox mPasswordBox;
        protected bool mIsTextPopupOpen = false;
        protected bool mIsPasswordPopupOpen = false;
        protected string mOldText;

        protected Brush mBackgroundBrush;
        protected Brush mTextBrush;
        protected double mFontSize = 0;
        protected double mBorderWidth = 0;
        protected bool mIsContentUpToDate = true;

        //////////////////////////////////////////////////////////////////////////
        // Choices
        protected bool mIsMultiChoice;
        protected bool mIsCombo;
        protected List<int> mOriginalChoice;

        protected ListBox mSelectionListBox;
        protected int selectedIndex = -1;
        protected List<int> selectedIndices;
        protected bool mIsListPopupOpen = false;

        protected ComboBox mSelectionComboBox;
        protected bool mIsComboPopupOpen = false;

        protected bool mDoNotSave = false;

        protected const double INSIDE_RECT_OPACITY = 0.5;
        protected const double OUTSIDE_RECT_OPACTIY = 0.25;

        // Touch input
        protected bool mTouchInput = false;
        protected bool mTouchMenuIsOpen;
        protected FormSelectionView mFormSelectionView;
        protected IList<string> mSelectedItems;

        
        

        public FormFill(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_form_fill;
            mNextToolMode = ToolManager.ToolType.e_form_fill;

            mViewerCanvas = mToolManager.AnnotationCanvas;

            mClickRectangle = new Rectangle();
            mClickRectangle.Fill = new SolidColorBrush(Color.FromArgb(255, 50, 150, 50));
            mClickRectangle.Visibility = Visibility.Collapsed;
            mClickRectangle.Opacity = INSIDE_RECT_OPACITY;
            this.Children.Add(mClickRectangle);
        }

        internal override void OnCreate()
        {
 	        base.OnCreate();
            mActiveWidget = mAnnot;
            mWidgetPageNumber = mAnnotPageNum;
            // We need to create our own references to annot and page number, as pointer pressed might change them
        }

        internal override void OnClose()
        {
            if (mIsTextPopupOpen)
            {
                CloseTextBox();
                mIsTextPopupOpen = false;
            }
            else if (mIsPasswordPopupOpen && !mDoNotSave)
            {
                SavePassword();
                mIsPasswordPopupOpen = false;
            }
            else if (mIsListPopupOpen && !mDoNotSave)
            {
                CloseList();
                mIsListPopupOpen = false;
            }
            else if (mIsComboPopupOpen && !mDoNotSave)
            {
                SaveCombo();
                mIsComboPopupOpen = false;
            }
            CloseTouchChoice();
            base.OnClose();
        }

        #region Event Handlers

        internal override void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            mShouldHandleKeyEvents = false;
            mViewerCanvas.CaptureMouse();
            mTouchInput = false;

            if (mTouchMenuIsOpen)
            {
                mNextToolMode = ToolManager.ToolType.e_pan;
                return;
            }

            if (ProcessInputDown(e))
            {
                base.MouseLeftButtonDownHandler(sender, e);
            }
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            mTouchInput = true;
            base.TouchDownHandler(sender, e);
            if (CloseTouchChoice())
            {
                mNextToolMode = ToolManager.ToolType.e_pan;
            }
            if (mTextBox != null || mPasswordBox != null)
            {
                mNextToolMode = ToolManager.ToolType.e_pan;
            }
            mViewerCanvas.CaptureTouch(e.TouchDevice);
        }

        private bool ProcessInputDown(InputEventArgs e)
        {
            UIPoint screenPoint = GetPosition(e, mPDFView);

            pdftron.SDF.Obj annotObj = mPDFView.GetAnnotationAt(screenPoint.X, screenPoint.Y);
            if (annotObj != null)
            {
                Annot annot = new Annot(annotObj);
                if (annot != mActiveWidget)
                {
                    mAnnot = annot;
                    mNextToolMode = ToolManager.ToolType.e_pan;
                    return false;
                }
                else
                {
                    mWidgetPageNumber = mPDFView.GetPageNumberFromScreenPt(screenPoint.X, screenPoint.Y);
                    HandleWidget();
                    return false;
                }
            }
            return true;
        }

        internal override void MouseMovedHandler(object sender, MouseEventArgs e)
        {
            if (mClickingOnWidget)
            {
                UIPoint canvasPoint = e.GetPosition(mViewerCanvas);
                if (mWidgetRect.Contains(canvasPoint.X, canvasPoint.Y))
                {
                    mClickRectangle.Opacity = INSIDE_RECT_OPACITY;
                }
                else
                {
                    mClickRectangle.Opacity = OUTSIDE_RECT_OPACTIY;
                }
                return;
            }

            base.MouseMovedHandler(sender, e);
        }

        internal override void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (mTextBox == null)
            {
                mShouldHandleKeyEvents = true;
            }
            if (mClickingOnWidget)
            {
                mWidgetRectangle.Visibility = Visibility.Collapsed;
                UIPoint canvasPoint = e.GetPosition(mViewerCanvas);
                if (mWidgetRect.Contains(canvasPoint.X, canvasPoint.Y))
                {
                    switch (mFieldType)
                    {
                    case Field.Type.e_check:
                        HandleCheck();
                        break;
                    case Field.Type.e_radio:
                        HandleRadio();
                        break;
                    case Field.Type.e_button:
                        HandleButton();
                        break;
                    }

                }
                mNextToolMode = ToolManager.ToolType.e_pan;
            }
            else
            {
                base.MouseLeftButtonUpHandler(sender, e);
            }
            mViewerCanvas.ReleaseMouseCapture();
        }

        private void ProcessInputUp(InputEventArgs e)
        {

        }

        /// <summary>
        /// When we are clicking on a widget, we don't want the context menu, and when we're editing text or passwords, we want the defauly WPF one.
        /// 
        /// Else, we use the standard pan one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void MouseRightButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            UIPoint canvasPoint = e.GetPosition(mViewerCanvas);
            if (mClickingOnWidget || mAnnotBBox.Contains(canvasPoint.X, canvasPoint.Y))
            {
                return; 
            }

            base.MouseRightButtonUpHandler(sender, e);
        }


        /// <summary>
        /// We want to prevent scrolling and zooming in certain cases
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal override void PreviewMouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            UIPoint canvasPoint = e.GetPosition(mViewerCanvas);
            if (mClickingOnWidget || mAnnotBBox.Contains(canvasPoint.X, canvasPoint.Y))
            {
                e.Handled = true;
            }
        }


        internal override void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            RepositionElement();
        }

        internal override void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            RepositionElement();
        }


        internal override void MouseClickHandler(object sender, System.Windows.Input.MouseEventArgs e)
        {
            base.MouseClickHandler(sender, e);
            UIPoint canvasPoint = e.GetPosition(mViewerCanvas);
            if (mClickingOnWidget)
            {
                if (mWidgetRect.Contains(canvasPoint.X, canvasPoint.Y))
                {
                    mClickRectangle.Opacity = INSIDE_RECT_OPACITY;
                }
                else
                {
                    mClickRectangle.Opacity = OUTSIDE_RECT_OPACTIY;
                }
                return;
            }

            if (!mWidgetRect.Contains(canvasPoint.X, canvasPoint.Y))
            {
                EndCurrentTool(ToolManager.ToolType.e_pan);
            }
        }

        //internal override void KeyDownAction(object sender, KeyEventArgs e)
        //{
        //    if (!mWidgetHandledKeyDown)
        //    {
        //        base.KeyDownAction(sender, e);
        //    }
        //    else
        //    {
        //        mWidgetHandledKeyDown = false;
        //    }
        //}

        public override void TapHandler(object sender, TouchEventArgs e)
        {
            base.TapHandler(sender, e);
            e.Handled = true;
            mTouchInput = true;
            ProcessInputDown(e);
            //HandleWidget();
            switch (mFieldType)
            {
                case Field.Type.e_check:
                    HandleCheck();
                    break;
                case Field.Type.e_radio:
                    HandleRadio();
                    break;
                case Field.Type.e_button:
                    HandleButton();
                    break;
            }
            if (mFieldType == Field.Type.e_check || mFieldType == Field.Type.e_radio || mFieldType == Field.Type.e_button)
            {
                mNextToolMode = ToolManager.ToolType.e_pan;
            }
        }


        #endregion Event Handlers


        #region Utility Functions

        protected void HandleWidget()
        {
            if (mActiveWidget != null)
            {
                try
                {
                    mPDFView.DocLockRead();
                    pdftron.PDF.Annots.Widget w = new pdftron.PDF.Annots.Widget(mActiveWidget);
                    mField = w.GetField();

                    if (mField.IsValid() && !mField.GetFlag(Field.Flag.e_read_only))
                    {
                        mFieldType = mField.GetType();
                        if (mFieldType == Field.Type.e_check || mFieldType == Field.Type.e_radio || mFieldType == Field.Type.e_button)
                        {
                            mClickingOnWidget = true;
                            mWidgetRect = mActiveWidget.GetRect();

                            ConvertPageRectToCanvasRect(mWidgetRect, mWidgetPageNumber);

                            mClickRectangle.Width = mWidgetRect.Width();
                            mClickRectangle.Height = mWidgetRect.Height();
                            mClickRectangle.SetValue(Canvas.LeftProperty, mWidgetRect.x1);
                            mClickRectangle.SetValue(Canvas.TopProperty, mWidgetRect.y1);
                            mWidgetRectangle.Opacity = INSIDE_RECT_OPACITY;
                            mClickRectangle.Visibility = Visibility.Visible;

                            DrawWidgetFrame(mActiveWidget, mWidgetPageNumber);
                        }
                        else
                        {
                            mViewerCanvas.ReleaseMouseCapture();
                            if (mFieldType == Field.Type.e_choice)
                            {
                                HandleChoice();
                            }
                            else if (mFieldType == Field.Type.e_text)
                            {
                                if (mField.GetFlag(Field.Flag.e_password))
                                {
                                    HandlePassword();
                                }
                                else if (mField.GetFlag(Field.Flag.e_multiline))
                                {
                                    mIsMultiLine = true;
                                    HandleText();
                                }
                                else
                                {
                                    mIsMultiLine = false;
                                    HandleText();
                                }
                            }
                        }

                    //    else if (fieldType == FieldType.e_signature)
                    //    {
                    //        // TODO: Encryption
                    //    }
                    }
                }
                catch (Exception)
                {
                }
                finally {
                    mPDFView.DocUnlockRead();
                }
            }
        }


        private void HandleCheck()
        {
            try
            {
                mPDFView.DocLock(true);
                mField.SetValue(!mField.GetValueAsBool());
                UpdateDate();
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlock();
            }
            
            PDFRect updateRect = mField.GetUpdateRect();
            ConvertPageRectToScreenRect(updateRect, mWidgetPageNumber);
            updateRect.Normalize();
            mPDFView.Update(updateRect);
            mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
        }

        private void HandleRadio()
        {
            try
            {
                mPDFView.DocLock(true);
                mField.SetValue(true);
                UpdateDate();
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlock();
            }

            PDFRect updateRect = mField.GetUpdateRect();
            ConvertPageRectToScreenRect(updateRect, mWidgetPageNumber);
            updateRect.Normalize();
            mPDFView.Update(updateRect);
            mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
        }

        private void HandleButton()
        {
            try
            {
                mPDFView.DocLockRead();

                pdftron.PDF.Annots.Link link = new pdftron.PDF.Annots.Link(mActiveWidget.GetSDFObj());
                // Can't use regular casting, since mAnnot is of type Widget.

                pdftron.PDF.Action a = link.GetAction();
                if (a != null && a.IsValid())
                {
                    pdftron.PDF.Action.Type at = a.GetType();
                    if (at == pdftron.PDF.Action.Type.e_URI)
                    {
                        pdftron.SDF.Obj o = a.GetSDFObj();
                        o = o.FindObj("URI");
                        if (o != null)
                        {
                            String uristring = o.GetAsPDFText();
                            if (uristring != null)
                            {
                                System.Text.RegularExpressions.Regex emailExpression = new System.Text.RegularExpressions.Regex("^([a-zA-Z0-9_\\-\\.]+)@((\\[[0-9]{1,3}" + "\\.[0-9]{1,3}\\.[0-9]{1,3}\\.)|(([a-zA-Z0-9\\-]+\\" + ".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\\]?)$");
                                if (emailExpression.IsMatch(uristring))
                                {
                                    if (!uristring.StartsWith("mailto:"))
                                    {
                                        uristring = uristring.Insert(0, "mailto:");
                                    }
                                }
                                System.Diagnostics.Process.Start(uristring);
                            }
                        }
                    }
                    else if (at == pdftron.PDF.Action.Type.e_GoTo)
                    {
                        mPDFView.ExecuteAction(a);
                    }
                }
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlockRead();
            }
            
        }

        /// <summary>
        /// Used by all kinds of controls to prevent the PDFViewWPF from scrolling to accomodate the controls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollChangedHandler(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true; // this will prevent the entire view from scrolling to accommodate the text box
        }

        // Gets the border width of the Widget
        protected void GetBorderWidth()
        {
            Annot.BorderStyle bs = mActiveWidget.GetBorderStyle();
            Obj aso = mActiveWidget.GetSDFObj();
            if (aso.FindObj("BS") == null && aso.FindObj("Border") == null)
            {
                bs.width = 0; //.SetWidth(0);
            }
            if (bs.border_style == Annot.BorderStyle.Style.e_beveled
                    || bs.border_style == Annot.BorderStyle.Style.e_inset)
            {
                bs.width *= 2;
            }
            mBorderWidth = bs.width;
        }

        protected void RepositionElement()
        {
            SetFontSize();
            if (mIsTextPopupOpen)
            {
                // Sets the fontsize

                if (mFontSize > 0)
                {
                    mTextBox.FontSize = mFontSize;
                }
                else
                {
                    mTextBox.FontSize = 10 * mPDFView.GetZoom();
                }

                // Get text from field, together with text properties
                ExtractTextProperties();

                // position the textbox
                PositionElement(mTextPopupBorder);
                mTextPopupBorder.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
                mTextPopupBorder.BorderBrush = Brushes.Red;
            }
            if (mIsPasswordPopupOpen)
            {
                // Sets the fontsize

                if (mFontSize > 0)
                {
                    mPasswordBox.FontSize = mFontSize;
                }
                else
                {
                    mPasswordBox.FontSize = 10 * mPDFView.GetZoom();
                }

                // Get text from field, together with text properties
                ExtractTextProperties();

                // position the textbox
                PositionElement(mPasswordBox);
                mPasswordBox.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
                mPasswordBox.BorderBrush = Brushes.Red;
            }
            if (mIsComboPopupOpen)
            {
                PositionElement(mSelectionComboBox);
                // This here makes sure that the selected item appears in the combo box.
                double height = mSelectionComboBox.Height;
                double fontNeededHeight = mFontSize;
                if (height - fontNeededHeight < 5)
                {
                    mSelectionComboBox.Padding = new Thickness(0, height - fontNeededHeight - 5, 0, 0);
                }

                mSelectionComboBox.FontSize = mFontSize;
                mSelectionComboBox.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
                mSelectionComboBox.BorderBrush = Brushes.Transparent;
                foreach (object o in mSelectionComboBox.Items)
                {
                    ComboBoxItem item = o as ComboBoxItem;
                    item.FontSize = mFontSize;
                }
            }
            if (mIsListPopupOpen)
            {
                PositionElement(mSelectionListBox);
                mSelectionListBox.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
                mSelectionListBox.BorderBrush = Brushes.Transparent;
                foreach (object o in mSelectionListBox.Items)
                {
                    ListBoxItem item = o as ListBoxItem;
                    TextBlock tb = item.Content as TextBlock;

                    // This here reduces the space between items, and make them more like the ones in the actual list.
                    tb.Height = mFontSize * 1.3;
                    item.FontSize = mFontSize;
                }

            }
        }

        protected void UpdateDate()
        {
            DateTime now = DateTime.Now;
            Date date = new Date((short)now.Year, (byte)now.Month, (byte)now.Day, (byte)now.Hour, (byte)now.Minute, (byte)now.Second);
            mActiveWidget.SetDate(date);
        }

        #endregion Utility Functions




        #region Handle Text

        private void HandleText()
        {
            mIsTextPopupOpen = true;
            ShouldHandleKeyEvents = false;

            // create the control to host the text canvas
            mTextPopupBorder = new Border();

            // create text box
            mTextBox = new TextBox();

            // Sets the font size
            SetFontSize(true);
            if (mFontSize > 0)
            {
                mTextBox.FontSize = mFontSize;
            }
            else
            {
                // this size doesn't really matter, since it's in an auto-sizing view box.
                mTextBox.FontSize = 10 * mPDFView.GetZoom();
            }

            // Get text from field, together with text properties
            ExtractTextProperties();

            // Get the colors for the textbox
            MapColorFont();
            mTextBox.Foreground = mTextBrush;
            mTextBox.Background = mBackgroundBrush;
            mTextBox.BorderThickness = new Thickness(0);

            // position the textbox
            PositionElement(mTextPopupBorder);
            mTextPopupBorder.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
            mTextPopupBorder.BorderBrush = Brushes.Red;
            mTextPopupBorder.Background = mBackgroundBrush;

            if (mIsMultiLine)
            {
                mTextBox.TextWrapping = TextWrapping.Wrap;
                mTextBox.AcceptsReturn = true;
            }
            else
            {
                mTextBox.TextWrapping = TextWrapping.NoWrap;
                mTextBox.AcceptsReturn = false;
            }

            // Add event handlers
            mTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChangedHandler), true);
            mTextBox.Loaded += TextBox_Loaded;
            mTextBox.KeyDown += TextBox_KeyDown;
            mTextBox.TextChanged += mTextBox_TextChanged;

            if (mFontSize == 0)
            {
                Viewbox vb = new Viewbox();
                vb.Child = mTextBox;
                mTextPopupBorder.Child = vb;
            }
            else
            {
                mTextPopupBorder.Child = mTextBox;
            }
            this.Children.Add(mTextPopupBorder);
        }

        void mTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mAnnotIsEdited = true;
            bool locked = false;
            try
            {
                locked = mPDFView.DocTryLock();
                if (locked)
                {
                    mField.SetValue(mTextBox.Text);
                    mField.RefreshAppearance();
                    mIsContentUpToDate = true;
                }
                else
                {
                    mIsContentUpToDate = false;
                }
            }
            catch (System.Exception)
            {

            }
            finally
            {
                if (locked)
                {
                    mPDFView.DocUnlock();
                }
            }
        }

        void TextBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            mTextBox.Focus();
            mTextBox.SelectionStart = mTextBox.Text.Length;
            mShouldHandleKeyEvents = false;
        }

        void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                mDoNotSave = true;
                EndCurrentTool(ToolManager.ToolType.e_pan);
            }
            mWidgetHandledKeyDown = true;
        }

        private void HandlePassword()
        {
            mIsPasswordPopupOpen = true;
            ShouldHandleKeyEvents = false;

            // create the control to host the text canvas
            mTextPopupBorder = new Border();

            // create password obx
            mPasswordBox = new PasswordBox();
            mPasswordBox.PasswordChar = '*';

            // Sets the fontsize
            SetFontSize();
            if (mFontSize > 0)
            {
                mPasswordBox.FontSize = mFontSize;
            }
            else
            {
                mPasswordBox.FontSize = 10 * mPDFView.GetZoom();
            }

            // Get text from field, together with text properties
            ExtractTextProperties();

            // Get the colors for the password box
            MapColorFont();
            mPasswordBox.Foreground = mTextBrush;
            mPasswordBox.Background = mBackgroundBrush;

            // position the passaword box
            PositionElement(mTextPopupBorder);
            mTextPopupBorder.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
            mTextPopupBorder.BorderBrush = Brushes.Red;

            // Add event handlers
            mPasswordBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChangedHandler), true);
            mPasswordBox.Loaded += PasswordBox_Loaded;
            mPasswordBox.KeyDown += TextBox_KeyDown;

            mTextPopupBorder.Child = mPasswordBox;
            this.Children.Add(mTextPopupBorder);
        }

        void PasswordBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            mPasswordBox.Focus();
            mPasswordBox.SelectAll();
        }


        protected void ExtractTextProperties()
        {
            try
            {
                // compute border width
                GetBorderWidth();

                if (mTextBox != null)
                {
                    // compute alignment
                    Field.TextJustification just = mField.GetJustification();//  .GetJustification();
                    if (just == Field.TextJustification.e_left_justified)
                    {
                        mTextBox.TextAlignment = TextAlignment.Left;
                    }
                    else if (just == Field.TextJustification.e_centered)
                    {
                        mTextBox.TextAlignment = TextAlignment.Center;
                    }
                    else if (just == Field.TextJustification.e_right_justified)
                    {
                        mTextBox.TextAlignment = TextAlignment.Right;
                    }
                }

                // set initial text
                if (mTextBox != null)
                {
                    mOldText = mField.GetValueAsString();
                    mTextBox.Text = mOldText;
                }
                if (mPasswordBox != null)
                {
                    mOldText = mField.GetValueAsString();
                    mPasswordBox.Password = mOldText;
                    
                }

                // comb and max length
                int maxLength = mField.GetMaxLen();
                if (maxLength >= 0)
                {
                    if (mTextBox != null)
                    {
                        mTextBox.MaxLength = maxLength;
                    }
                    if (mPasswordBox != null)
                    {
                        mPasswordBox.MaxLength = maxLength;
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        protected void PositionElement(FrameworkElement target)
        {
            BuildAnnotBBox();
            mWidgetRect = new PDFRect(mAnnotBBox.x1, mAnnotBBox.y1, mAnnotBBox.x2, mAnnotBBox.y2);
            ConvertPageRectToCanvasRect(mWidgetRect, mWidgetPageNumber);

            pdftron.PDF.Annots.Widget widget = new pdftron.PDF.Annots.Widget(mActiveWidget);
            PDFPage page = mPDFView.GetDoc().GetPage(mWidgetPageNumber);
            // Get clockwise rotation
            int rotation = (4 - (widget.GetRotation() / 90) + (int)page.GetRotation() + (int)mPDFView.GetRotation()) % 4;

            TransformGroup transGroup = new TransformGroup();
            RotateTransform rotateTransform = new RotateTransform(rotation * 90);
            transGroup.Children.Add(rotateTransform);
            target.RenderTransform = transGroup;

            switch (rotation)
            {
                case 0: // no rotation
                    target.SetValue(Canvas.LeftProperty, mWidgetRect.x1);
                    target.SetValue(Canvas.TopProperty, mWidgetRect.y1);
                    target.Width = mWidgetRect.Width();
                    target.Height = mWidgetRect.Height();
                    break;
                case 1: // 90 degrees clockwise rotation
                    target.SetValue(Canvas.LeftProperty, mWidgetRect.x1 + mWidgetRect.Width());
                    target.SetValue(Canvas.TopProperty, mWidgetRect.y1);
                    target.Width = mWidgetRect.Height();
                    target.Height = mWidgetRect.Width();
                    break;
                case 2: // 180 degrees
                    target.SetValue(Canvas.LeftProperty, mWidgetRect.x1 + mWidgetRect.Width());
                    target.SetValue(Canvas.TopProperty, mWidgetRect.y1 + mWidgetRect.Height());
                    target.Width = mWidgetRect.Width();
                    target.Height = mWidgetRect.Height();
                    break;
                case 3: // 270 degrees
                    target.SetValue(Canvas.LeftProperty, mWidgetRect.x1);
                    target.SetValue(Canvas.TopProperty, mWidgetRect.y1 + mWidgetRect.Height());
                    target.Width = mWidgetRect.Height();
                    target.Height = mWidgetRect.Width();
                    break;
            }

        }

        protected void SetFontSize(bool canBe0 = false)
        {
            mFontSize = 10 * mPDFView.GetZoom();
            double fontSize = 0;
            try
            {
                fontSize = 10 * (double)mPDFView.GetZoom();
                GState gs = mField.GetDefaultAppearance();
                if (gs != null)
                {
                    fontSize = (double)gs.GetFontSize();
                    if (fontSize <= 0 && !canBe0)
                    {
                        //auto size
                        fontSize = (double)(mTextPopupBorder.Height / 2.5);
                    }
                    else
                    {
                        fontSize *= (double)mPDFView.GetZoom();
                    }
                }
                mFontSize = fontSize;
            }
            catch (Exception)
            {
            }
            finally
            {
            }
        }

        protected void MapColorFont()
        {
            mTextBrush = new SolidColorBrush(Color.FromArgb(255, 50, 255, 150));
            mBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 180, 180, 120));

            try
            {
                GState gs = mField.GetDefaultAppearance();
                if (gs != null)
                {
                    //set text color
                    ColorPt color = new ColorPt();
                    ColorSpace cs = gs.GetFillColorSpace();
                    string s = cs.ToString();
                    ColorPt fc = gs.GetFillColor();
                    string s2 = fc.ToString();
                    cs.Convert2RGB(fc, color);
                    int r = (int)(color.Get(0) * 255 + 0.5);
                    int g = (int)(color.Get(1) * 255 + 0.5);
                    int b = (int)(color.Get(2) * 255 + 0.5);
                    mTextBrush = new SolidColorBrush(Color.FromArgb(255, (byte)r, (byte)g, (byte)b));

                    //set background color
                    color = GetFieldBkColor();
                    if (color == null)
                    {
                        r = 255;
                        g = 255;
                        b = 255;
                        mBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, (byte)r, (byte)g, (byte)b));
                    }
                    else
                    {
                        r = (int)(color.Get(0) * 255 + 0.5);
                        g = (int)(color.Get(1) * 255 + 0.5);
                        b = (int)(color.Get(2) * 255 + 0.5);
                        mBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, (byte)r, (byte)g, (byte)b));
                    }


                    //set the font of the EditBox to match the PDF form field's. in order to do this,
                    //you need to bundle with you App the fonts, such as "Times", "Arial", "Courier", "Helvetica", etc.
                    //the following is just a place holder.
                    Font font = gs.GetFont();
                    Obj obj = font.GetSDFObj();
                    if (obj != null && obj.IsDict())
                    {
                        
                        String name = font.GetName();
                        if (name == null || name.Length == 0)
                        {
                            name = "Times New Roman";
                        }
                        if (name.Contains("Times"))
                        {
                            //NOTE: you need to bundle the font file in you App and use it here.
                        }
                    }
                    else
                    {
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        protected ColorPt GetFieldBkColor()
        {
            try
            {
                Obj o = mActiveWidget.GetSDFObj().FindObj("MK");
                if (o != null)
                {
                    Obj bgc = o.FindObj("BG");
                    if (bgc != null && bgc.IsArray())
                    {
                        int sz = (int)bgc.Size();
                        switch (sz)
                        {
                            case 1:
                                Obj n = bgc.GetAt(0);
                                if (n.IsNumber())
                                {
                                    return new ColorPt(n.GetNumber(), n.GetNumber(), n.GetNumber());
                                }
                                break;
                            case 3:
                                Obj r = bgc.GetAt(0), g = bgc.GetAt(1), b = bgc.GetAt(2);
                                if (r.IsNumber() && g.IsNumber() && b.IsNumber())
                                {
                                    return new ColorPt(r.GetNumber(), g.GetNumber(), b.GetNumber());
                                }
                                break;
                            case 4:
                                Obj c = bgc.GetAt(0), m = bgc.GetAt(1), y = bgc.GetAt(2), k = bgc.GetAt(3);
                                if (c.IsNumber() && m.IsNumber() && y.IsNumber() && k.IsNumber())
                                {
                                    ColorPt cp = new ColorPt(c.GetNumber(), m.GetNumber(), y.GetNumber(), k.GetNumber());
                                    ColorPt cpout = new ColorPt();
                                    ColorSpace cs = ColorSpace.CreateDeviceCMYK();
                                    cs.Convert2RGB(cp, cpout);
                                    return cpout;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private void CloseTextBox()
        {
            ShouldHandleKeyEvents = true;
            if (mDoNotSave)
            {
                try
                {
                    mPDFView.DocLock(true);
                    mField.SetValue(mOldText);
                    mField.RefreshAppearance();
                }
                catch (Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlock();
                }
            }
            else
            {
                try
                {
                    mPDFView.DocLock(true);
                    if (!mIsContentUpToDate)
                    {
                        mField.SetValue(mTextBox.Text);
                        mField.RefreshAppearance();
                    }
                    if (mAnnotIsEdited)
                    {
                        UpdateDate();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlock();
                }

                if (mAnnotIsEdited)
                {
                    mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
                }
            }
            mPDFView.Update(mActiveWidget, mWidgetPageNumber);
        }

        protected void SavePassword()
        {
            ShouldHandleKeyEvents = true;
            if (!mDoNotSave)
            {
                string str = mPasswordBox.Password;
                if (!str.Equals(mOldText, StringComparison.Ordinal))
                {
                    try
                    {
                        mPDFView.DocLock(true);
                        mField.SetValue(str);
                        mField.RefreshAppearance();
                        
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        mPDFView.DocUnlock();
                    }
                    mPDFView.Update(mActiveWidget, mWidgetPageNumber);
                    mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
                }
            }
        }

        #endregion Handle Text


        #region Handle Choice

        protected void HandleChoice()
        {
            mIsMultiChoice = mField.GetFlag(Field.Flag.e_multiselect);
            mIsCombo = mField.GetFlag(Field.Flag.e_combo);

            if (mTouchInput)
            {
                HandleTouchInput();
                return;
            }

            if (mIsCombo)
            {
                HandleCombo();
            }
            else
            {
                HandleList();
            }
        }

        protected void HandleCombo()
        {
            mIsComboPopupOpen = true;

            List<string> options = GetOptionList();

            mSelectionComboBox = new ComboBox();
            mSelectionComboBox.Loaded += SelectionComboBox_Loaded;
            
            GetBorderWidth();

            PositionElement(mSelectionComboBox);
            SetFontSize();

            // This here makes sure that the selected item appears in the combo box.
            double height = mSelectionComboBox.Height;
            double fontNeededHeight = mFontSize;
            if (height - fontNeededHeight< 5)
            {
                mSelectionComboBox.Padding = new Thickness(0, height - fontNeededHeight - 5, 0, 0);
            }

            mSelectionComboBox.FontSize = mFontSize;
            mSelectionComboBox.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
            mSelectionComboBox.BorderBrush = Brushes.Transparent;

            foreach (string option in options)
            {
                ComboBoxItem item = new ComboBoxItem();

                item.Content = option;
                item.FontSize = mFontSize;

                mSelectionComboBox.Items.Add(item);
            }

            string selectedStr = mField.GetValueAsString();
            foreach (object o in mSelectionComboBox.Items)
            {
                ComboBoxItem item = o as ComboBoxItem;
                string s = item.Content as string;

                if (s.Equals(selectedStr, StringComparison.Ordinal))
                {
                    mSelectionComboBox.SelectedItem = o;
                    mOriginalChoice = new List<int>();
                    mOriginalChoice.Add(mSelectionComboBox.SelectedIndex);
                }
            }

            this.Children.Add(mSelectionComboBox);
        }

        void SelectionComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            mSelectionComboBox.IsDropDownOpen = true;
            mSelectionComboBox.SelectionChanged += mSelectionComboBox_SelectionChanged;
            mSelectionComboBox.Focus();
        }

        void mSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mAnnotIsEdited = true;
            TrySaveCombo();
        }

        protected void HandleList()
        {
            mIsListPopupOpen = true;

            mSelectionListBox = new ListBox();
            mSelectionListBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChangedHandler), true);
            mSelectionListBox.Loaded += SelectionListBox_Loaded;

            GetBorderWidth();

            List<string> options = GetOptionList();

            PositionElement(mSelectionListBox);
            SetFontSize();

            Style listBoxItemStyle = new Style();
            listBoxItemStyle.TargetType = typeof(ListBoxItem);
            Setter setter = new Setter();
            setter.Property = ListBoxItem.PaddingProperty;
            setter.Value = new Thickness(0);
            listBoxItemStyle.Setters.Add(setter);

            mSelectionListBox.ItemContainerStyle = listBoxItemStyle;

            mSelectionListBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);

            mSelectionListBox.BorderThickness = new Thickness(mBorderWidth * mPDFView.GetZoom());
            mSelectionListBox.BorderBrush = Brushes.Transparent;

            pdftron.PDF.Annots.Widget widget = new pdftron.PDF.Annots.Widget(mActiveWidget);
            int colorCompNum  = widget.GetBackgroundColorCompNum();
            ColorPt bgColor = widget.GetBackgroundColor();
            double bgr = 1;
            double bgg = 1;
            double bgb = 1;
            if (colorCompNum == 1)
            {
                bgr = bgColor.Get(0);
                bgg = bgColor.Get(0);
                bgb = bgColor.Get(0);
            }
            else if (colorCompNum == 3)
            {
                bgr = bgColor.Get(0);
                bgg = bgColor.Get(1);
                bgb = bgColor.Get(2);
            }

            SolidColorBrush bgBrush = new SolidColorBrush(Color.FromArgb(255, (byte)(bgr * 255), (byte)(bgg * 255), (byte)(bgb * 255)));
            mSelectionListBox.Background = bgBrush;

            double annotColorLuminance = (0.3 * bgr + 0.59 * bgg + 0.11 * bgb);
            SolidColorBrush textBrush;
            if (annotColorLuminance > 0.5)
                textBrush = Brushes.Black;
            else
                textBrush = Brushes.White;

            foreach (string option in options)
            {
                ListBoxItem item = new ListBoxItem();
                TextBlock tb = new TextBlock();
                tb.Foreground = textBrush;

                // This here reduces the space between items, and make them more like the ones in the actual list.
                tb.Height = mFontSize * 1.3;
                tb.Text = option;
                tb.Padding = new Thickness(0);
                tb.Margin = new Thickness(0, -3 , 0, 0);

                item.Content = tb;
                item.FontSize = mFontSize;
                item.ToolTip = option;
                mSelectionListBox.Items.Add(item);
            }

            if (mIsMultiChoice)
            {
                mSelectionListBox.SelectionMode = SelectionMode.Multiple;
                mOriginalChoice = new List<int>();

                selectedIndices = GetSelectedPositions();
                foreach (int i in selectedIndices)
                {
                    mOriginalChoice.Add(i);
                    mSelectionListBox.SelectedItems.Add(mSelectionListBox.Items[i]);
                }
                mSelectionListBox.UpdateLayout();
            }
            else
            {
                mSelectionListBox.SelectionMode = SelectionMode.Single;

                string selectedStr = mField.GetValueAsString();
                foreach (object o in mSelectionListBox.Items)
                {
                    ListBoxItem item = o as ListBoxItem;
                    //string s = item.Content as string;
                    TextBlock tb = item.Content as TextBlock;
                    string s = tb.Text;
                    if (s.Equals(selectedStr, StringComparison.Ordinal))
                    {
                        mSelectionListBox.SelectedItem = o;
                        mOriginalChoice = new List<int>();
                        mOriginalChoice.Add(mSelectionListBox.SelectedIndex);
                    }
                }
            }

            this.Children.Add(mSelectionListBox);
        }

        void SelectionListBox_Loaded(object sender, RoutedEventArgs e)
        {
            mSelectionListBox.SelectionChanged += mSelectionListBox_SelectionChanged;
            mSelectionListBox.ScrollIntoView(mSelectionListBox.SelectedItem); // TODO: Improve this to look more like the actual pdf
            mSelectionListBox.Focus();
        }

        void mSelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mAnnotIsEdited = true;
            TrySaveList();
        }

        //populate list from the choice annotation
        protected List<string> GetOptionList()
        {
            try
            {
                string[] options = mField.GetOpts();
                if (options != null)
                {
                    return new List<String>(options);
                }
                return null;
            }
            catch (Exception)
            {
            }
            return null;
        }


        //find the selected items from a multiple choice list
        protected List<int> GetSelectedPositions()
        {
            try
            {
                List<int> retList = new List<int>();
                Obj val = mField.GetValue();
                if (val != null)
                {
                    if (val.IsString())
                    {
                        Obj o = mAnnot.GetSDFObj().FindObj("Opt");
                        if (o != null)
                        {
                            int id = GetOptIdx(val, o);
                            if (id >= 0)
                            {
                                retList.Add(id);
                            }
                        }
                    }
                    else if (val.IsArray())
                    {
                        int sz = (int)val.Size();
                        for (int i = 0; i < sz; ++i)
                        {
                            Obj entry = val.GetAt(i);
                            if (entry.IsString())
                            {
                                Obj o = mAnnot.GetSDFObj().FindObj("Opt");
                                if (o != null)
                                {
                                    int id = GetOptIdx(entry, o);
                                    if (id >= 0)
                                    {
                                        retList.Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
                return retList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Get index of options in multiple choice list
        protected int GetOptIdx(Obj str_val, Obj opt)
        {
            try
            {
                int sz = (int)opt.Size();
                string str_val_string = str_val.GetAsPDFText();
                for (int i = 0; i < sz; ++i)
                {
                    Obj v = opt.GetAt(i);
                    if (v.IsString() && str_val.Size() == v.Size())
                    {
                        string v_string = v.GetAsPDFText();
                        if (str_val_string.Equals(v_string))
                        {
                            return i;
                        }
                    }
                    else if (v.IsArray() && v.Size() >= 2 && v.GetAt(1).IsString() && str_val.Size() == v.GetAt(1).Size())
                    {
                        v = v.GetAt(1);
                        String v_string = v.GetAsPDFText();
                        if (str_val_string.Equals(v_string, StringComparison.Ordinal))
                        {
                            return i;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return -1;
        }

        protected void TrySaveCombo()
        {
            bool locked = false;
            mIsContentUpToDate = false;
            try
            {
                locked = mPDFView.DocTryLock();
                if (locked)
                {
                    object o = mSelectionComboBox.SelectedItem;
                    ComboBoxItem item = o as ComboBoxItem;
                    string str = item.Content as String;

                    if (mField.GetValueAsString() != str)
                    {
                        mField.SetValue(str);
                        mField.RefreshAppearance();
                    }

                    mIsContentUpToDate = true;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (locked)
                {
                    mPDFView.DocUnlock();
                }
            }
        }


        protected void SaveCombo()
        {
            if (mDoNotSave)
            {
                if (mOriginalChoice != null && mOriginalChoice.Count > 0)
                {
                    try
                    {
                        mPDFView.DocLock(true);

                        ComboBoxItem item = mSelectionComboBox.Items[mOriginalChoice[0]] as ComboBoxItem;
                        string str = item.Content as String;

                        if (mField.GetValueAsString() != str)
                        {
                            mField.SetValue(str);
                            mField.RefreshAppearance();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        mPDFView.DocUnlock();
                    }
                }
            }
            try
            {
                mPDFView.DocLock(true);
                if (!mIsContentUpToDate)
                {
                    object o = mSelectionComboBox.SelectedItem;
                    ComboBoxItem item = o as ComboBoxItem;
                    string str = item.Content as String;

                    if (mField.GetValueAsString() != str)
                    {
                        mField.SetValue(str);
                        mField.RefreshAppearance();
                    }
                }
                if (mAnnotIsEdited)
                {
                    UpdateDate();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }

            mPDFView.Update(mActiveWidget, mWidgetPageNumber);
            if (mAnnotIsEdited && !mDoNotSave)
            {
                mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
            }
        }

        protected void TrySaveList()
        {
            bool locked = false;
            mIsContentUpToDate = false;
            try
            {
                locked = mPDFView.DocTryLock();
                if (locked)
                {
                    PDFDoc doc = mPDFView.GetDoc();
                    Obj arr = doc.CreateIndirectArray();

                    // We need to add the items in the order that they appear, so we can't just take the SelectedItems directly
                    // Otherwise they may not appear as selected
                    foreach (object o in mSelectionListBox.Items)
                    {
                        if (mSelectionListBox.SelectedItems.Contains(o))
                        {
                            ListBoxItem item = o as ListBoxItem;
                            TextBlock tb = item.Content as TextBlock;
                            arr.PushBackText(tb.Text);
                        }
                    }
                    mField.SetValue(arr);
                    mField.EraseAppearance();
                    mField.RefreshAppearance();

                    mIsContentUpToDate = true;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (locked)
                {
                    mPDFView.DocUnlock();
                }
            }

        }

        protected void CloseList()
        {
            if (mDoNotSave)
            {
                if (mOriginalChoice != null)
                {
                    try
                    {
                        mPDFView.DocLock(true);
                        PDFDoc doc = mPDFView.GetDoc();
                        Obj arr = doc.CreateIndirectArray();

                        // We need to add the items in the order that they appear, so we can't just take the SelectedItems directly
                        // Otherwise they may not appear as selected
                        foreach (int i in mOriginalChoice)
                        {
                            ListBoxItem item = mSelectionListBox.Items[i] as ListBoxItem;
                            TextBlock tb = item.Content as TextBlock;
                            arr.PushBackText(tb.Text);
                        }
                        mField.SetValue(arr);
                        mField.EraseAppearance();
                        mField.RefreshAppearance();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        mPDFView.DocUnlock();
                    }
                }
            }
            else
            {
                try
                {
                    mPDFView.DocLock(true);
                    if (!mIsContentUpToDate)
                    {
                        PDFDoc doc = mPDFView.GetDoc();
                        Obj arr = doc.CreateIndirectArray();

                        // We need to add the items in the order that they appear, so we can't just take the SelectedItems directly
                        // Otherwise they may not appear as selected
                        foreach (object o in mSelectionListBox.Items)
                        {
                            if (mSelectionListBox.SelectedItems.Contains(o))
                            {
                                ListBoxItem item = o as ListBoxItem;
                                TextBlock tb = item.Content as TextBlock;
                                arr.PushBackText(tb.Text);
                            }
                        }
                        mField.SetValue(arr);
                        mField.EraseAppearance();
                        mField.RefreshAppearance();
                    }
                    if (mAnnotIsEdited)
                    {
                        UpdateDate();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlock();
                }
            }
            mPDFView.Update(mActiveWidget, mWidgetPageNumber);
            if (mAnnotIsEdited && !mDoNotSave)
            {
                mToolManager.RaiseAnnotationEditedEvent(mActiveWidget);
            }
        }

        #endregion Handle Choice

        #region Handle Choice Through Touch

        private void HandleTouchInput()
        {
            List<string> options = GetOptionList();
            
            GetBorderWidth();
            SetFontSize();

            PDFRect widgetRect = new PDFRect(mAnnotBBox.x1, mAnnotBBox.y1, mAnnotBBox.x2, mAnnotBBox.y2);
            ConvertPageRectToScreenRect(widgetRect, mWidgetPageNumber);

            mFormSelectionView = new FormSelectionView(mPDFView, mActiveWidget, widgetRect, options, mFontSize, mBorderWidth, mIsMultiChoice, mIsCombo);
            mTouchMenuIsOpen = true;

            if (mIsMultiChoice)
            {
                mFormSelectionView.SetSelectedItems(GetSelectedPositions());
            }
            else
            {
                int index = options.IndexOf(mField.GetValueAsString());
                mFormSelectionView.SetSelectedItem(index);
            }
            mFormSelectionView.SelectionChanged += mFormSelectionView_SelectionChanged;
        }

        private void mFormSelectionView_SelectionChanged(IList<string> selection)
        {
            mSelectedItems = selection;
            mIsContentUpToDate = false;
            SaveChoiseFromTouch();
        }

        private void SaveChoiseFromTouch()
        {
            if (mSelectedItems == null || mSelectedItems.Count < 1)
            {
                return;
            }

            try
            {
                mPDFView.DocLock(true);
                if (mIsCombo)
                {
                    PushBackComboChoice(mSelectedItems[0]);
                }
                else
                {
                    PushBackListSelection(mSelectedItems);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
                mPDFView.Update(mActiveWidget, mWidgetPageNumber);
            }
        }

        private void PushBackComboChoice(string option)
        {
            mField.SetValue(option);
            mField.RefreshAppearance();
        }

        private void PushBackListSelection(IList<string> options)
        {
            PDFDoc doc = mPDFView.GetDoc();
            Obj arr = doc.CreateIndirectArray();

            foreach (string option in options)
            {
                arr.PushBackText(option);
            }
            mField.SetValue(arr);
            mField.EraseAppearance();
            mField.RefreshAppearance();
        }

        // Closes the current touch selection if open. Returns true if it was open.
        protected bool CloseTouchChoice()
        {
            mTouchMenuIsOpen = false;
            if (mFormSelectionView != null && mFormSelectionView.IsOpen)
            {
                mFormSelectionView.IsOpen = false;
                return true;
            }
            return false;
        }

        #endregion HandleChoiceThroughTouch
    }
}