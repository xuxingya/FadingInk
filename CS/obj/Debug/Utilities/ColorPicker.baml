   M S B A M L   `   `   `  ����U  QPDFViewWPFSimpleTestCS2013, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:pdftron.PDF.Toolspdftron.PDF.Tools  5      #    pdftron.PDF.Tools.ColorPicker   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      1localclr-namespace:pdftron.PDF.Tools     5      -   5          �Name$
  this��5       � Title$ Color��5      $	��250��5      $	��250��5      $	��500��5	      $	��500��5
      $	��300��5      $	��300��5       � 
ResizeMode$ CanResizeWithGrip=�5       � ShowInTaskbar .   � WindowStartupLocation$ CenterOwner=�5      .��5      �  � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   A� -   5         A�Name$ PresetColorGrid��5       A� Rows$ 4��6/    A� Columns$ 4��68   	 � 	Resources	 5      
 �� 
TargetType �� Property �� Value��%<  '��       '�� l     �� 5      "
 ��6   .�5      ��  � ��$ 36��6.   �� 5       � ��$ 36��6/   5      �� 5       "
 ��6   .�5!      ��  � ��$ 30��6.   �� 5"       � ��$ 30��6/   �� 5#       � ��$ Center=�6<   �� 5$       � ��$ Center=�6:   5%      5&      5'      � 5)   
   $��1��6   $��	20 5 20 5q�6   .+�5*      �� 
���   �$��2q�6)   $	��100��6=   $��60��6I   $
��Left=�6U   ._�5+      �� $��30(�6   $��Empty��6*   $��Center=�67   $��Center=�6T   5,      �� 5-      -   5         ��Name$ ColorRectangle��5-      $	��100��60   $��60��6<   
����  �$
��Left=�6T   5.      � 51   
   $��2��6   $��	25 3 25 3q�6   .+�52   
   �� -   5         ��Name$ NoColorButton��52      $��No Color��6)   $
��Left=�6<   6W   �� 53   
    �� Orientation$ 
Horizontal=�6   $��Right=�6/   	 54      ��%v   '��       �� 55      "
 ��6   .�56      ��  � ��$ 50��61   �� 57       � ��$ 20��62   58      59      .+�5:      �� -   5        $��OkButton��5:       �� 	IsDefault . .��6M   OK6Q   �� 5;      -   5        $��OancelButton��5;       �� IsCancel . $��15 0 0 0q�69   .��6K   Cancel6S   5<      5=      5?      5A      