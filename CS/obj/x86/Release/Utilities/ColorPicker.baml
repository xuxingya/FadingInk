   M S B A M L   `   `   `  ���� D  @FadingInk, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:pdftron.PDF.Toolspdftron.PDF.Tools  #    pdftron.PDF.Tools.ColorPicker   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     1localclr-namespace:pdftron.PDF.Tools     -     �Name$
  this�� � Title$ Color��$	��250��$	��250��$	��500��$	��500��$	��300��$	��300�� � 
ResizeMode$ CanResizeWithGrip=� � ShowInTaskbar .   � WindowStartupLocation$ CenterOwner=�.���  � RowDefinitions �� $��* ��� $
��Auto ��� $
��Auto �.+�A� -    A�Name$ PresetColorGrid�� A� Rows$ 4�� A� Columns$ 4��	 � 	Resources	 
 �� 
TargetType �� Property �� Value��%�   '��       '�� 9     �� "
 ��.���  � ��$ 36����  � ��$ 36���� "
 ��.���  � ��$ 30����  � ��$ 30����  � ��$ Center=���  � ��$ Center=�� $��1��$��	20 5 20 5q�.+��� 
���   �$��2q�$	��100��$��60��$
��Left=�._��� $��30(�$��Empty��$��Center=�$��Center=��� -    ��Name$ ColorRectangle��$	��100��$��60��
����  �$
��Left=�� $��2��$��	25 3 25 3q�.+��� -    ��Name$ NoColorButton��$��No Color��$
��Left=���  �� Orientation$ 
Horizontal=�$��Right=�	 ��%C   '��       �� "
 ��.���  � ��$ 50����  � ��$ 20��.+��� -   $��OkButton�� �� 	IsDefault . .��OK�� -   $��OancelButton�� �� IsCancel . $��15 0 0 0q�.��Cancel