   M S B A M L   `   `   `  ����   ANS2    ,ANS.UserControls.EnvioMasivoOperationControl   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � 	Resources      BooleanToVisibilityConverter  ModernButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType ��Name �� CornerRadius J� Effect3  -System.Windows.Media.Effects.DropShadowEffect   
BlurRadius	   ShadowDepth
   Opacity �� Triggers P� Property P� Value��%b  &	        &	      �� �� " ��.���  � �� �	#FF2196F3��  � ��
 �������  � ��$ 16(���  � ��$
 12,6q���  � ��$ 2q���  � ��$	 100����  � ��
 �   ���  �@�Cursor$
 Hand����  � ��$ Center=���  � ��$ Center=���  � �� �� " ��.R��� $ border��#��z��$ 3��   $ 3Q�$	 2Q�$	
 0.2Q�._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � �� �	#FF64B5F6P�  � ��$
 True��. ���  � �� �	#FF0D47A1P�  � {�$ False��. ���  �@J�Opacity$	 0.6Q�.��� .+�%  Xceed.Wpf.Toolkit.BusyIndicator     BusyContent$ Cargando...�����	#80FFFFFF   IsBusy �� *	IsLoading+.���  � ColumnDefinitions �� $��* �.+�� $��0�� � RowDefinitions �� $
��Auto ��� $��* �.+��� $��
20,10,20,0q�._��� $��ENVÍOS MASIVOS��$��24(�$
��Bold"�$��HP Simplified)�$��Center=�(  "MaterialDesignThemes.Wpf.ColorZone  $��1��   Mode$
 Dark=�$��Stretch=�$��Stretch=�$��20q�.���� $
��Auto=�.���  �� $��* ��� $��* ��� $��* � �� $��* �.+��� .+��� $��Envío masivo 1 (7:30 AM)��$��20(�$��HP Simplified)�$��10q�$
��Left=�$��Center=�$��0��$��0���� -    ��Name$ btnEnvioMasivo1��$
��SEND��$��10q�#��[ $��Center=�$
��Left=�$��0��$��0������ *EjecutarEnvioMasivo1+�� .+��� $��Envío Masivo 2 (15:00)��$��20(�$��HP Simplified)�$��10q�$��Center=�$��Center=�$��1��$��0���� -   $ btnEnvioMasivo2��$
��SEND��$��10q�#��[ $��Center=�$��1��$��1������ *EjecutarEnvioMasivo2+�� .+��� $��Envío Masivo 3 (19:00)��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��2��$��0���� -   $ btnEnvioMasivo3��$
��SEND��$��10q�#��[ $��Center=�$��Right=�$��2��$��1������ *EjecutarEnvioMasivo3+