   M S B A M L   `   `   `  ���� S  OToggleSwitch, Version=1.1.2.0, Culture=neutral, PublicKeyToken=8637099990568f75A0clr-namespace:ToggleSwitch;assembly=ToggleSwitchToggleSwitch  D @FadingInk, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:pdftron.PDF.Toolspdftron.PDF.Tools !   PDFViewWPFSimple.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     IToggleSwitch0clr-namespace:ToggleSwitch;assembly=ToggleSwitch     1Toolsclr-namespace:pdftron.PDF.Tools     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006       � Title$  	FadingInk��$	��700��$	��900��
������� � 	Resources    RoundedTextbox �� 
TargetType �� CornerRadius ��Name  RoundedButtonStyle �� 
TargetType �� Property �� Value �� Triggers	 P� Property
 P� Value  RoundedToggleButtonStyle�� %�  &	        &	 ^     &	 >    �� " ��.R��� $ 10��
����������	#FF66615B$��1q�._��� $ PART_ContentHost���� " ��.���  �@J�Opacity$ 1Q���  � �� �� " ��.R��� $��border��$ 10��
����������	#FF66615B$��1q�._��� $��Center=�$	��Top=� P� 	 � ��
 . . ���  �@J�Opacity$	 0.2Q��� " d�.���  �@J�Opacity$ 1Q���  � �� �� " d�.R��� $��border��$ 10��
����������	#FF66615B$��1q�._��� $��Center=�$	��Top=� P� 	 � ��
 . . ���  �@J�Opacity$	 0.4Q�P� 	 �@d�	IsChecked$
 FalseY�. ���  � ��$ fading����  � ��$ normal��.��� -    �Name$ 
LayoutRoot�� � ColumnDefinitions �� $
��367* ��� $
��529* �.+�� -   $ MainWindowGrid��$��	0,0,0,0.5q�$��2�� � RowDefinitions �� $
��Auto ��� $
��Auto ��� $��* ��� $
��Auto �.+�� $��0���� 
�������$��0,0,0,1q�._�� $��1��$��0,0,0,0q�$��50�� �� $��70 ��� $��70 ��� $��70 ��� $��* ��� $��70 ��� $��70 ��� $��70 ��� $��70 ��� $��70 �.+�� $��0��.+��� -    ��Name$
 Open��$
��open��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�� $��1��.+��� -   $ SavePDF��$
��save��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�� $��2��.+��� -   $ 	SaveAsPDF��$��save as��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�� $��3��.+��� -   $ button��
�������
�������$��Right=�$��0q�$��Center=�$��75��$��50���� -   $ 	LogButton��$	��log��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�$��70,15,205,15q�� $��4��.+��� -   $ ClickResetAnno��$��reappear��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�� $��5��.+��� -	   $ ClickHideAnno��$
��hide��#��[ $��Center=�$��60��$��20��$��12(�$��Segoe UI Light)�� $��6��.+��� -
   $��
fadingtime��#��[  $	��180��$��Center=�$��60��$��20��$��12(�$��Segoe UI Light)� �� TextAlignment$ Center=�.��.��� $��7��.+�d� -    d�Name$ FadingControl��#��[ $��Center=�$��0,0,0,0q�$��60��$��20��$��Center=�$��12(�$��Segoe UI Light)�� $��8��.+�� $��60��$��20��.+��� -    ��Name$ txtbox_pagenum��$��30��$��0q�
������ $��12(�$��Segoe UI Light)�$��25��$ Center=�$��0,2,20,0q�.���� -    ��Name$ txt_pagecount��$��40��$��12(�$��Segoe UI Light)�$��25��$��	20,-2,0,0q�$��Center=��� ���	#FF66615B$ 10��$��1q�$��20��$��Center=�$��Center=�$��60��� -   $ PDFGrid��$��2��$��0,0,0,0q�� $��2�� �� $
��183* ��� $
��110* ��� $
��603* �