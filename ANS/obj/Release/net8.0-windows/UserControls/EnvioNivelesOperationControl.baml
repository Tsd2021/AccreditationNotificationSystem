   M S B A M L   `   `   `  ����   ANS3    -ANS.UserControls.EnvioNivelesOperationControl   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � 	Resources      BooleanToVisibilityConverter  ModernButtonStyle �� 
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
 True��. ���  � �� �	#FF0D47A1P�  � {�$ False��. ���  �@J�Opacity$	 0.6Q�.��� .+�%  Xceed.Wpf.Toolkit.BusyIndicator     BusyContent$ Cargando...�����	#80FFFFFF   IsBusy �� *	IsLoading+.���  � ColumnDefinitions �� $��* �.+�� $��0�� � RowDefinitions �� $��* ��� $��* �.+��� $��
20,10,20,0q�._��� $,��&ENVÍO NOTIFICACIONES POR DESCONEXIÓN��$��24(�$
��Bold"�$��HP Simplified)�$��Center=�(  "MaterialDesignThemes.Wpf.ColorZone  $��1��   Mode$
 Dark=�$��Stretch=�$��Stretch=�$��20q�.���� $
��Auto=�.��� .+��� .+��� $��Notificar desconexiones��$��20(�$��HP Simplified)�$��10q�$��Center=�$��Center=�$��0��$��0���� -    ��Name$ btnEnvioDesconexiones��$
��SEND��$��10q�#��[ $��Center=�$��Center=�$��0��$��0������ *EjecutarEnvioNiveles+