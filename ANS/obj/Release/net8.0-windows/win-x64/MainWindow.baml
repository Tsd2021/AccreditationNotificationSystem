   M S B A M L   `   `   `  ���� >  :ANS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:ANS.Model	ANS.Model      ANS.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  #localclr-namespace:ANS.Model    � Title$
  TAAS�� � WindowState$ 	Maximized=� � 	Resources    CircularButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius J� SnapsToDevicePixels	 �� DataType   ANS.Model.SnackbarMsg
 �� Padding �� TextAlignment��%n  &	        (�� �     *, +)�� " ��.���  � ��
 ���� ��  � ��
 ��ح���  � ��$ 2q���  � �� �� " ��.R��� #��z��#��z��#��z��$ 18��$
 True��._��� $��Center=�$��Center=�$
��True���� "	  .R��� $
 6q�$ 8������ *Fondo+._��� 
�������$��18(�$��Normal"�$
��Wrap=�$ Center=����� *Texto+.��� .+�(  "MaterialDesignThemes.Wpf.ColorZone     Mode$
 Dark=�$��Stretch=�$��Stretch=�.��� $��10q� � RowDefinitions �� $
��Auto ��� $��* �.+�� $��0��.+��� $��48(�$
��Bold"�
�������$��Center=�$��Center=����� *CurrentTime+ �� StringFormat$ yyyy-MM-dd HH:mm:ss���� -    ��Name$ SettingsButton��$��36��$��36��$��Right=�$	��Top=�$��16q�#��[  .��'  !MaterialDesignThemes.Wpf.PackIcon     Kind$ Settings=�$��28��$��28��
�������� $��1�� � ColumnDefinitions �� $��* ��� $��* �.+�� $��0�� �� $��* ��� $
��Auto ��� $��* �.+��� $/��)Tecnisegur Automatic Accreditation System��
�������$��26(�$��Center=�$	��Top=�$��0��$��0,50,0,0q�'  !MaterialDesignThemes.Wpf.Snackbar  -     Name$ JobSnackbar��
������ $��Center=�$	��Top=� K� ZIndex$	 999��$��0,10,0,0q�$��1��   MessageQueue 4  .MaterialDesignThemes.Wpf.MessageQueueExtension  �� $��1��$��3��$
 10q�
�������$��2q�  MaterialDesignPaper#���  $ 8��$��10q�._��� $
��Auto=�.���� 
�������$��Stretch=�$��Stretch=����� *TuplaMensajes+���� .R��� $
 5q�
�������$ 10��$��5q�$��Stretch=�._��  �� $
��Auto ��� $��* ��� $��* ��� $��* ��� $��* �.+�  $��19��$��19��$��10q�$��Center=�$��0�� �� *Icon+�� $��16(�$
��Bold"�
���   �$��10q�$��Center=�$��1������ *Estado+�� $��14(�
�������$��10q�$��Center=�$��2������ *Tipo+�� $��14(�$��10q�$��Center=�$��3������ *Banco+���� *Color+  ColorToBrushConverter �� 	Converter# [ �� $��12(�
�������$��10q�$��Center=�$��4������ *Fecha+$ yyyy-MM-dd HH:mm:ss��