 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using pdftron;
using pdftron.PDF;
using pdftron.SDF;
using pdftron.Filters;
using pdftron.Common;
using pdftron.PDF.Tools;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace PDFViewWPFSimple
{
    ///<summary>
    ///Interaction logic for MainWindow.xaml
    ///</summary>
    public partial class MainWindow : Window
    {
        //private static pdftron.PDFNetLoader pdfNetLoader = pdftron.PDFNetLoader.Instance();        
        private PDFViewWPF _pdfviewWpf;
        public PDFViewWPF PDFViewWPF
        {
            get { return _pdfviewWpf; }
        }
        private ToolManager _toolManager;
        private PDFDoc _pdfdoc;
        private String filePath;

        //private bool _updatingFromPDFViewWPF = false;
        private int updateInterval = 3;

        public MainWindow()
        {
            PDFNet.Initialize("DEMO:2:2F43CEE2D84c95809319B76220D054");
            InitializeComponent();
            _pdfviewWpf = new PDFViewWPF();
            PDFGrid.Children.Add(_pdfviewWpf);
            _toolManager = new ToolManager(_pdfviewWpf);
            txtbox_pagenum.KeyDown += txtbox_pagenum_KeyDown;
            //_pdfviewWpf.CurrentZoomChanged += _pdfviewWpf_CurrentZoomChanged; // set zoom textbox value
            _pdfviewWpf.CurrentPageNumberChanged += _pdfviewWpf_CurrentPageNumberChanged;
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            _pdfviewWpf.OnSetDoc += _pdfviewWpf_OnSetDoc;
            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, updateInterval);
            dispatcherTimer.Start();
            _pdfviewWpf.SetPagePresentationMode(PDFViewWPF.PagePresentationMode.e_single_continuous);
            //_pdfviewWpf.SetupThumbnails(false,true,true,1024,500*1024*1024,0.1);
            //_pdfviewWpf.IsManipulationEnabled = true;
            _pdfviewWpf.PixelsPerUnitWidth = 2;
            //_pdfviewWpf.IsManipulationEnabled = false;
        }

        public bool OpenPDF(String filename)
        {
            try
            {
                PDFDoc oldDoc = _pdfviewWpf.GetDoc();
                _pdfdoc = new PDFDoc(filename);
                _pdfviewWpf.SetDoc(_pdfdoc);
                filePath = filename;
                if (oldDoc != null)
                {
                    oldDoc.Dispose();
                    this.Save(_pdfdoc.GetFileName());
                    //logtest();
                }
                //FreehandCreate.logtxt.Append("Title " + filename + "\r\n" + "Start Date: " + DateTime.Now.ToString() + "\r\n" + "fading speed: " + fadingtime.Text + " s" + "\r\n");
                //UpdateAnno();
            }
            catch (PDFNetException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }

            //this.Title = filename;
            return true;

        }

        public void Save(string filename)
        {
            if (_pdfdoc == null) return;

            _pdfdoc.Lock();

            try
            {
                _pdfdoc.Save(filename, pdftron.SDF.SDFDoc.SaveOptions.e_remove_unused);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error during the Save");
            }
            _pdfdoc.Unlock();
        }

        #region DependencyPropertyBinding

        internal void SetPropertyBinding(PDFViewWPF viewer, DependencyProperty dp, string propertyName, FrameworkElement element)
        {
            Binding b = new Binding(propertyName) { Source = element };
            b.Mode = BindingMode.TwoWay;
            viewer.SetBinding(dp, b);
        }

        #endregion

        #region Zoom and Turn Page Events

        void _pdfviewWpf_OnSetDoc(PDFViewWPF viewer)
        {
            //_updatingFromPDFViewWPF = true;
            _pdfviewWpf.SetZoom(1.12, true);
            _pdfviewWpf.SetVScrollPos(0);
            //int val = (int)Math.Round(_pdfviewWpf.GetZoom() * 100);
            txtbox_pagenum.Text = "" + _pdfviewWpf.GetCurrentPage();
            //_updatingFromPDFViewWPF = false;
        }

        void _pdfviewWpf_CurrentPageNumberChanged(PDFViewWPF viewer, int currentPage, int totalPages)
        {
            txtbox_pagenum.Text = "" + currentPage;
        }

        //void slider_zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    //Trace.WriteLine("slide zoom");
        //    int val = (int)Math.Round(e.NewValue * 100);
        //    if (!_updatingFromPDFViewWPF)
        //    {
        //        _pdfviewWpf.SetZoom(e.NewValue, true);
        //    }
        //}

        //void _pdfviewWpf_CurrentZoomChanged(PDFViewWPF viewer)
        //{
        //    //Trace.WriteLine("zoom change");            
        //    _updatingFromPDFViewWPF = true;
        //    int val = (int)Math.Round(_pdfviewWpf.GetZoom() * 100);
        //    _updatingFromPDFViewWPF = false;
        //}

        void txtbox_pagenum_KeyDown(object sender, KeyEventArgs e)
        {
            int pgnum;
            if (Int32.TryParse(txtbox_pagenum.Text, out pgnum))
                _pdfviewWpf.SetCurrentPage(pgnum);
        }


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_pdfdoc != null && _pdfviewWpf != null && (_pdfdoc.IsModified()))
            {
                string messageBoxText = "Would you like to save changes to " + _pdfdoc.GetFileName() + "?";
                string caption = "PDFViewWPF";
                //logtest();
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxImage icon = MessageBoxImage.Question;
                MessageBoxResult defaultResult = MessageBoxResult.Yes;
                MessageBoxOptions options = MessageBoxOptions.DefaultDesktopOnly;

                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);

                if (result == MessageBoxResult.Yes)
                {
                    this.Save(_pdfdoc.GetFileName());
                }
                else if (result == MessageBoxResult.No)
                {
                    this.Save(_pdfdoc.GetFileName());
                    _pdfviewWpf.CloseDoc();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
        int pagenum;
        private void btnOpen_Clicked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*";
            dlg.DefaultExt = ".pdf";

            if (dlg.ShowDialog() == true)
            {
                OpenPDF(dlg.FileName);
                pagenum = _pdfviewWpf.GetPageCount();
                txt_pagecount.Content = " | " + pagenum.ToString();

            }
        }

        //private void btn_Prev_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.GotoPreviousPage();
        //}

        //private void btn_Next_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.GotoNextPage();
        //}

        //private void btn_ZoomIn_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.SetZoom(_pdfviewWpf.GetZoom() * 1.25);
        //    //Console.WriteLine("click zoom is " + _pdfviewWpf.GetZoom());

        //}

        //private void btn_ZoomOut_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.SetZoom(_pdfviewWpf.GetZoom() / 1.25);
        //}

        //private void btnExit_Clicked(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}

        //private void btn_LastPage_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.GotoLastPage();
        //}

        //private void btn_FirstPage_Clicked(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //        _pdfviewWpf.GotoFirstPage();
        //}


        private void btnSave_Clicked(object sender, RoutedEventArgs e)
        {
            if (_pdfdoc != null && _pdfviewWpf != null)
            {
                this.Save(_pdfdoc.GetFileName());
            }
        }

        private void btnSaveAs_Clicked(object sender, RoutedEventArgs e)
        {
            if (_pdfdoc != null && _pdfviewWpf != null)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                dlg.DefaultExt = ".pdf";
                dlg.FileName = _pdfdoc.GetFileName();

                if (dlg.ShowDialog() == true)
                {
                    this.Save(dlg.FileName);
                }
            }
        }

        #endregion


            #region Update Opacity

            /// <summary>
            /// Change the opacity of annotations every 10 seconds according to the time from annotations been created to now.
            /// </summary>
            /// 
            //Fading speed is the speed that the opacity of annotations decrease
            double _DefaultFadingSpeed = 0.018;

        double FadingSpeed;

        //Date AnnoDate;
        //DateTime CurrentTime;
        //DateTime Date = new DateTime(2016, 1, 15, 13, 19, 0);
        //this is the function to update the opacity of all annos
        int fadingnumber;
        //int pagenum;
        //int depage;
        int AnnotNum;
        pdftron.PDF.Page page;
        pdftron.PDF.Annot CurrentAnno;
        double FadingOpacity;
        private void UpdateAnno()
        {
            int pagecount = 1;
            while (pagecount<=pagenum)
            {

                page = _pdfdoc.GetPage(pagecount);
                //pagenum = itr.GetPageNumber();
                //depage = Math.Abs(pagenum - currentpagenum) + 1;
                AnnotNum = page.GetNumAnnots();
                if (AnnotNum > 0)
                {
                    for (int i = 0; i < AnnotNum; i++)
                    {
                        CurrentAnno = page.GetAnnot(i);
                        //some pdf may contain annotations itself without date, which caused error so I exlude them by time
                        if (CurrentAnno.IsValid() && CurrentAnno.GetType() == Annot.Type.e_Ink)
                        {
                            refreshannot(CurrentAnno, pagecount);
                            _pdfviewWpf.Update(CurrentAnno, pagecount);
                        }
                    }

                }
                pagecount++;
            }
            // _pdfviewWpf.Update(false);
        }

        protected Annot mAnnot = null;
        private void refreshannot(Annot annot, int pagenumber)
        {
           
                var content = annot.GetContents();
                if (content != "-2" && content != "0")
                {

                    pdftron.PDF.Annots.Ink mMarkup = new pdftron.PDF.Annots.Ink(annot);
                    //Trace.WriteLine("opacity is " + mMarkup.GetOpacity().ToString());
                    if (mMarkup.GetOpacity() <= 0)
                    {
                        FadingOpacity = 0;
                        mMarkup.SetContents("-2");
                        mMarkup.SetOpacity(FadingOpacity);
                        mMarkup.RefreshAppearance();
                        _pdfviewWpf.Update(mMarkup, pagenumber);
                }
                    else
                    {

                        var tapnumber = int.TryParse(annot.GetContents(), out fadingnumber);
                        if (tapnumber)
                        {
                            if (fadingnumber > 1) {
                                FadingSpeed = _DefaultFadingSpeed / 2;
                        }
                        else
                        {
                            FadingSpeed = _DefaultFadingSpeed;
                        }

                        }
                        FadingOpacity = mMarkup.GetOpacity() - FadingSpeed;
                        if (FadingOpacity < 0)
                        {
                            FadingOpacity = 0;
                            mMarkup.SetContents("-2");
                        }
                        mMarkup.SetOpacity(FadingOpacity);
                        mMarkup.RefreshAppearance();
                       _pdfviewWpf.Update(mMarkup, pagenumber);
                }
                //page.AnnotRemove(annot);
                //page.AnnotPushBack(mMarkup);

            }


        }
        //A timer which will run every 1 seconds(dispatcherTimer.Interval)
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_pdfviewWpf.GetDoc() != null)
            {
                if (SimpleShapeCreate.MainFading)
                {
                    //UpdateAnno();

                }
            }
        }

        //fading on or off
        private void fadingswitch_Clicked(object sender, RoutedEventArgs e)
        {
            if (SimpleShapeCreate.MainFading)    
            {
                SimpleShapeCreate.MainFading = false;
            }
            else
            {
                SimpleShapeCreate.MainFading = true;
            }


        }

        #endregion
        private void setpenid(object sender, StylusEventArgs e)
        {
            var id = e.StylusDevice.Id;
            SimpleShapeCreate.stylusid = id;
            Trace.WriteLine("stylus id is "+id);
        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab|| e.Key == Key.Enter)
            {
                int _fadingtime;
                bool isnumber = int.TryParse(fadingtime.Text, out _fadingtime);
                if (isnumber && _fadingtime != 0)
                {
                    _DefaultFadingSpeed = 1.0/ _fadingtime*updateInterval;
                }
            }
        }

        //log out logtxt
        public static StringBuilder logtxt = new StringBuilder();
        public int annotationsum = 0;
        public int permanentannots = 0;
        private void log(object sender, RoutedEventArgs e)
        {
           
            int pagecount = 1;
            while (pagecount <= pagenum)
            {
                page = _pdfdoc.GetPage(pagecount);
                AnnotNum = page.GetNumAnnots();
                annotationsum += AnnotNum;
                if (AnnotNum > 0)
                {
                    for (int i = 0; i < AnnotNum; i++)
                    {
                        CurrentAnno = page.GetAnnot(i);
                        string datetime= CurrentAnno.GetDate().month.ToString()+"/"+ CurrentAnno.GetDate().day.ToString() + "/" + CurrentAnno.GetDate().hour.ToString() + "/" + CurrentAnno.GetDate().minute.ToString() + "/" + CurrentAnno.GetDate().second.ToString();
                        Trace.WriteLine(datetime);
                        if (CurrentAnno.IsValid() && CurrentAnno.GetType() == Annot.Type.e_Ink)
                        {
                            logtxt.Append(i.ToString() + "\t" + "date:" + datetime +"\t" + "State" + "\t" + CurrentAnno.GetContents() + "\n");
                            if (CurrentAnno.GetContents() == "0")
                            {
                                permanentannots++;
                            }

                        }
                    }

                }

                pagecount++;
            }
            logtxt.Append("Annnotation numbers" + "\t" + annotationsum + "\n");
            logtxt.Append("Permanent annnotation numbers" + "\t" + permanentannots + "\n");
            var savepath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"log", _pdfdoc.GetFileName() + ".txt");
            StreamWriter sw = File.CreateText(savepath);
            sw.WriteLine(logtxt);
            sw.Flush();
            sw.Close();
            logtxt.Clear();
            annotationsum = 0;
            permanentannots = 0;
        }
        private void logtest()
        {
            //FreehandCreate.logtxt.Append("test over Time: " + DateTime.Now.ToString());
            //int index = 1;
            //var savepath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "fadinginktest", index.ToString()+".txt");
            //while (File.Exists(savepath))
            //{
            //    index++;
            //    savepath= System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "fadinginktest", index.ToString()+".txt");
            //}

            //StreamWriter sw = File.CreateText(savepath);
            //sw.WriteLine(FreehandCreate.logtxt);
            //sw.Flush();
            //sw.Close();
            //FreehandCreate.logtxt.Clear();
        }
        //private void ClearAllAnno(object sender, RoutedEventArgs e)
        //{
        //    if (_pdfviewWpf.GetDoc() != null)
        //    {
        //        PageIterator itr = _pdfdoc.GetPageIterator();
        //        while (itr.HasNext())
        //        {
        //            pdftron.PDF.Page page = itr.Current();
        //            int AnnotNum = page.GetNumAnnots();
        //            if (AnnotNum > 0)
        //            {
        //                for (int i = 0; i < AnnotNum; i++)
        //                {
        //                    page.AnnotRemove(i);
        //                }
        //            }
        //            itr.Next(); 
        //        }
        //        _pdfviewWpf.Update(true);

        //    }

        //}

        //public void drawannothumbnail(int col)
        //{
        //    Ellipse myEllipse = new Ellipse();
        //    SolidColorBrush mybrush = new SolidColorBrush();
        //    mybrush.Color = Color.FromArgb(255, 0, 0, 2);
        //    myEllipse.Fill = mybrush;
        //    myEllipse.Width = 20;
        //    myEllipse.Height = 20;
        //    myEllipse.SetValue(Grid.ColumnProperty, col);
        //    //myEllipse.SetValue(Grid.RowProperty, 0);
        //    annoGrid.Children.Add(myEllipse);
        //}

        bool hideannochecked = false;
        private void HideAnno(object sender, RoutedEventArgs e)

        {
            if (hideannochecked == false)
            {
                _pdfviewWpf.SetDrawAnnotations(false);
                ClickHideAnno.Content = "show";
                hideannochecked = true;
            }
            else
            {
                _pdfviewWpf.SetDrawAnnotations(true);
                ClickHideAnno.Content = "hide";
                hideannochecked = false;
            }

        }
        private void ResetAnno(object sender, RoutedEventArgs e)
        {
            if (_pdfviewWpf.GetDoc() != null)
            {
                if (FadingControl.IsChecked==false)
                {
                    FadingControl.IsChecked = true;
                    SimpleShapeCreate.MainFading = false;
                }
                
                PageIterator itr = _pdfdoc.GetPageIterator();
                while (itr.HasNext())
                {
                    pdftron.PDF.Page page = itr.Current();
                    int AnnotNum = page.GetNumAnnots();
                    if (AnnotNum > 0)
                    {
                        for (int i = 0; i < AnnotNum; i++)
                        {
                            var CurrentAnno = page.GetAnnot(i);
                            //some pdf may contain annotations itself without date,which caused error so I exlude them by time
                            if (CurrentAnno.IsValid())
                            {
                                if (CurrentAnno.GetType().ToString() == "e_Ink"&&CurrentAnno.GetContents()!="0")
                                {
                                    pdftron.PDF.Annots.Ink mMarkup = new pdftron.PDF.Annots.Ink(CurrentAnno);                                   
                                    mMarkup.SetOpacity(1);
                                    mMarkup.SetContents("1");
                                    mMarkup.RefreshAppearance();
                                    _pdfviewWpf.Update(mMarkup, page.GetIndex());

                                }
                            }
                        }
                    }
                    itr.Next();
                }
            }
        }
    }
}
