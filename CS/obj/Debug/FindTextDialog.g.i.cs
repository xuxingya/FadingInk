﻿#pragma checksum "..\..\FindTextDialog.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "51E5CE7FB3690358DF6534138B58C907"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace PDFViewWPFSimple {
    
    
    /// <summary>
    /// FindTextDialog
    /// </summary>
    public partial class FindTextDialog : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txtBox;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox matchCase;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox matchWord;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox searchUp;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock FindTextStatus;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btn_find;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\FindTextDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btn_cancel;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/PDFViewWPFSimpleTestCS2013;component/findtextdialog.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\FindTextDialog.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.txtBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 10 "..\..\FindTextDialog.xaml"
            this.txtBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.txtBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.matchCase = ((System.Windows.Controls.CheckBox)(target));
            
            #line 11 "..\..\FindTextDialog.xaml"
            this.matchCase.Checked += new System.Windows.RoutedEventHandler(this.matchCase_Checked);
            
            #line default
            #line hidden
            
            #line 11 "..\..\FindTextDialog.xaml"
            this.matchCase.Unchecked += new System.Windows.RoutedEventHandler(this.matchCase_Unchecked);
            
            #line default
            #line hidden
            return;
            case 3:
            this.matchWord = ((System.Windows.Controls.CheckBox)(target));
            
            #line 12 "..\..\FindTextDialog.xaml"
            this.matchWord.Checked += new System.Windows.RoutedEventHandler(this.matchWord_Checked);
            
            #line default
            #line hidden
            
            #line 12 "..\..\FindTextDialog.xaml"
            this.matchWord.Unchecked += new System.Windows.RoutedEventHandler(this.matchWord_Unchecked);
            
            #line default
            #line hidden
            return;
            case 4:
            this.searchUp = ((System.Windows.Controls.CheckBox)(target));
            
            #line 13 "..\..\FindTextDialog.xaml"
            this.searchUp.Checked += new System.Windows.RoutedEventHandler(this.searchUp_Checked);
            
            #line default
            #line hidden
            
            #line 13 "..\..\FindTextDialog.xaml"
            this.searchUp.Unchecked += new System.Windows.RoutedEventHandler(this.searchUp_Unchecked);
            
            #line default
            #line hidden
            return;
            case 5:
            this.FindTextStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.btn_find = ((System.Windows.Controls.Button)(target));
            
            #line 15 "..\..\FindTextDialog.xaml"
            this.btn_find.Click += new System.Windows.RoutedEventHandler(this.btn_find_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.btn_cancel = ((System.Windows.Controls.Button)(target));
            
            #line 16 "..\..\FindTextDialog.xaml"
            this.btn_cancel.Click += new System.Windows.RoutedEventHandler(this.btn_cancel_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

