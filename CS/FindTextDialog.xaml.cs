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
using System.Windows.Shapes;

using pdftron;
using pdftron.PDF;
using pdftron.Common;

namespace PDFViewWPFSimple
{
    /// <summary>
    /// Interaction logic for FindTestDialog.xaml
    /// </summary>
    public partial class FindTextDialog : Window
    {
        private string text;
        private bool isMatchCase;
        private bool isMatchWord;
        private bool isSearchUp;
        private bool isSearchActive;

        public FindTextDialog()
        {
            InitializeComponent();
            txtBox.Focus();

            this.KeyDown += new KeyEventHandler(FindTextDialog_KeyDown);
            this.Closed += new EventHandler(FindTextDialog_Closed);
            this.Loaded += FindTextDialog_Loaded;
        }

        void FindTextDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Activate();
        }

        private void FindText()
        {
            MainWindow parent = (MainWindow)this.Owner;
            if (parent != null && !isSearchActive && text != null && text != string.Empty)
            {
                isSearchActive = true;
                FindTextStatus.Text = "";
                parent.PDFViewWPF.FindText(text, isMatchCase, isMatchWord, isSearchUp, false);
            }
        }

        void FindTextDialog_Closed(object sender, EventArgs e)
        {
            MainWindow parent = (MainWindow)this.Owner;
            if (parent != null)
            {
                parent.PDFViewWPF.CancelFindText();
                parent.ClearSearchSelection(true);
                parent.Focus();
            }
        }

        void FindTextDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindText();
            }
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void btn_find_Click(object sender, RoutedEventArgs e)
        {
            FindText();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void searchUp_Checked(object sender, RoutedEventArgs e)
        {
            isSearchUp = true;
        }

        private void searchUp_Unchecked(object sender, RoutedEventArgs e)
        {
            isSearchUp = false;
        }

        private void matchWord_Checked(object sender, RoutedEventArgs e)
        {
            isMatchWord = true;
        }

        private void matchWord_Unchecked(object sender, RoutedEventArgs e)
        {
            isMatchWord = false;
        }

        private void matchCase_Checked(object sender, RoutedEventArgs e)
        {
            isMatchCase = true;
        }

        private void matchCase_Unchecked(object sender, RoutedEventArgs e)
        {
            isMatchCase = false;
        }

        private void txtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainWindow parent = (MainWindow)this.Owner;
            parent.PDFViewWPF.CancelFindText();
            text = txtBox.Text;
            FindTextStatus.Text = "";
        }

        public void Activate()
        {
            txtBox.Focus();
        }

        public void TextSearchFinished(object sender, bool found, pdftron.PDF.PDFViewWPF.Selection selection)
        {
            if (!isSearchActive)
            {
                return;
            }
            isSearchActive = false;
            if (found == false)
            {
                FindTextStatus.Text = "No math found";
            }
        }
    }
}
