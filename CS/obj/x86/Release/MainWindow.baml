   M S B A M L   `   `   `  ���� S  OToggleSwitch, Version=1.1.2.0, Culture=neutral, PublicKeyToken=8637099990568f75A0clr-namespace:ToggleSwitch;assembly=ToggleSwitchToggleSwitch  D @FadingInk, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:pdftron.PDF.Toolspdftron.PDF.Tools !   PDFViewWPFSimple.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     IToggleSwitch0clr-namespace:ToggleSwitch;assembly=ToggleSwitch     1Toolsclr-namespace:pdftron.PDF.Tools       � Title$  	FadingInk��$	��700��$	��900��
������� � 	Resources    AnimatedSwitch �� 
TargetType �� Property �� Value �� 
TargetType (� Stretch ��Name �� Left
	 �� Top
 ��Name ��Name A�Name T� X T� Y J� BitmapEffect F� Softness F� ShadowDepth F� 	Direction F� Color �� Triggers P� Property P� Value N� EnterActions �� TargetProperty	 �� To g� Duration �� 
TargetName O� KeyTime O� Value �� 	KeySpline N� ExitActions�� %T  &	        �� " d�.���  � ��
 �   ���  � �� �#FAFAFB��  � �� �#EAEAEB��  � �� �� " d�.R�(� $ Uniform=�.���� $ Layer_1��$��20��$��20��$ 10��$	 0��.+��� $
 textBox1��$��18��$
��Wrap=�$��
Fading Off��$��	-70,4,0,0q�$��10.667(�$��Center=�$��Center=�$��Microsoft Sans Serif)����	#FF9E9D9D�� $
 textBox2��$��18��$
��Wrap=�$��	Fading On��$��40,4,0,0q�$��10.667(�$��Center=�$��Center=�$��Microsoft Sans Serif)����	#FF9E9D9D$��54��A� $ 0��$��20��$��20��#��z��#��z��$	��0.5��A� $ 15��$��20��$��20��#��z��#��z��$	��0.5���� $ 
rect416927��$ 10��$��15��$��20��#��z��#��z��$��0,0.5,0,0.5q�A� $ ellipse��$ 0��$��20��$��20��
�������#��z��$	��0.3��z�T� $ 0Q�$ 0Q� F� $	 0.1Q�$	 0.7Q�$	 270Q�$ #BBBBBB�� P�  �@d�	IsChecked$
 TrueY� �� .���� .���� $ Background.Color�$ #52D468X�$ 0:0:0.2D��� $ BorderBrush.Color�$ #41C955X�$ 0:0:0.2D��� $ textBox1��$4 .(TextBlock.Foreground).(SolidColorBrush.Color)�$ #FFFFFFX�$ 0:0:0.4D��� $ textBox2��$4 .(TextBlock.Foreground).(SolidColorBrush.Color)�$ 	#FF9E9D9DX�$ 0:0:0.4D�U� $8 2(UIElement.RenderTransform).(TranslateTransform.X)�$ ellipse��.\��� $ 0�� 0�� $ 0:0:0.4��$ 0, 1, 0.6, 1�� 15 �� .���� .���� $ Background.Color�$ #FAFAFBX�$ 0:0:0.2D��� $ BorderBrush.Color�$ #EAEAEBX�$ 0:0:0.2D��� $ textBox1��$4 .(TextBlock.Foreground).(SolidColorBrush.Color)�$ 	#FF9E9D9DX�$ 0:0:0.4D��� $ textBox2��$4 .(TextBlock.Foreground).(SolidColorBrush.Color)�$ 	#FFFFFFFFX�$ 0:0:0.4D�U� $8 2(UIElement.RenderTransform).(TranslateTransform.X)�$ ellipse��.\��� $ 0�� 15�� $ 0:0:0.3��$ 0, 0.5, 0.5, 1�� 0.��� -     �Name$  
LayoutRoot��.+�� -   $  MainWindowGrid��! � RowDefinitions! �� $
��Auto ��� $
��Auto ��� $��* ��� $
��Auto �.+�� $��0��� $��1��$��0,0,0,1q�.+�`� $	��Top=�" `� 
Background
" �����# `� IsLocked# . .�c� -   $ c�Name$$ toolbar_header1��% c� Band$% 1��& c� 	BandIndex$& 2��$
��Left=�$
��1011��$��
0,0,-926,0q�$��40����Z� .@��� -   .���� $��Resources\fileopen.png��$��20��$��Center=�$��20,0,0,0q�' J� RenderTransformOrigin$' 0.5,0.5&�z�V� .~��� �� �� T� $ -8Q�d� -   ( d�Name$( fadingswitch��$��20��$��Center=�#��[  $��	700,0,0,0q�$��20��) d� 	IsChecked$
) trueY��� 
�������$��1��$��1��$��Bottom=�� -   $  PDFGrid��$��2��$��0,1,0,0q�.+��� -   * ��Name$* slider��$
��Left=�$��506,-30,0,0q�$	��Top=�$	��118��+ � SmallChange$+ 5Q�, �� TickFrequency$, 10Q�- �� IsSnapToTickEnabled- . . � Maximum$	. 300Q�/ � LargeChange$/ 10Q�0 �� TickPlacement$0 BottomRight=�1 � Value$1 30Q�2 � BindingGroup2 &   System.Windows.Data.BindingGroup  �� -   3 ��Name$3 textBox��$
��Left=�$��24��$��612,-30,0,0q�4 �� TextWrapping$
4 Wrap=�$	��Top=�$��31��5 �� TextAlignment$5 Center=�$��0q����	#FF5CD339���� 6 �� ElementName$6 slider��7 �� Path$7 Value�8 �� UpdateSourceTrigger$8 PropertyChanged=��� -	   $
 	textBlock��$
��Left=�$��640,-29,0,0q�$
��Wrap=�$	��Top=�$��10.667(��� $�S���� �� �� 
�������$��1��$��3��$	��Top=�� $��3��$��0,1,0,0q�.+�c� -
   $$ toolbar_footer��#��� ��$��Right=�{�.  $��0,1,0,0q�.@��� -   .���� $"��Resources\previous-green.png��$��16���� -   .���� $!��Resources\reverse_green.png��$��16���� -   $3 txtbox_pagenum��$��40��$��Center=�
��������� -   9 ��Name$9 txt_pagecount��$��45��$��Center=��� -   .���� $��Resources\play_green.png��$��16���� -   .���� $��Resources\next_green.png��$��16���� -   .���� $��Resources\zoomout.gif���� -   $* slider_zoom��$	��100��$. 4Q�: � Minimum$	: 0.1Q��� -   .���� $��Resources\zoomin.gif���� -   $3 txtbox_zoom��$��40��$��Center=�
�������; �� 
IsReadOnly; . �� $��20��$��%��$
��Left=��� -   $
 
textBlock1��$
��Left=�$��
541,-1,0,0q�$
��Wrap=�$��
FadingTime��$	��Top=�$��10.667(�