   M S B A M L   `   `   `  ����   ANS0    *ANS.UserControls.SantanderOperationControl   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � 	Resources      BooleanToVisibilityConverter  ModernButtonStyle �� 
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
 True��. ���  � �� �	#FF0D47A1P�  � {�$ False��. ���  �@J�Opacity$	 0.6Q�.��� .+��� -    ��Name$ btnAsociarMails��$��Asociar Mails��
���   �#��[ $��Right=�$	��Top=�$��5q�
���   �$��10q�$��0������ *!EjecutarAltaEmailDestinoCommand+%  Xceed.Wpf.Toolkit.BusyIndicator     BusyContent$ Cargando...�����	#80FFFFFF   IsBusy �� *	IsLoading+.���  � ColumnDefinitions �� $��* ��� $��* �.+�� $��0�� � RowDefinitions �� $
��Auto ��� $��* �.+��� $��
20,10,20,0q�._��� $��ACREDITACIONES��$��24(�$
��Bold"�$��HP Simplified)�$��Center=�(  "MaterialDesignThemes.Wpf.ColorZone  $��1��   Mode$
 Dark=�$��Stretch=�$��Stretch=�$��20q�.���� $
��Auto=�.���  �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto � �� $��* ��� $
��Auto �.+��� $��Acreditar Punto a Punto��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��0��$��0���� -   $ btnEjecutarPuntoAPunto��$	��P2P��$��10q�#��[ $��Center=�$��0��$��1������ *EjecutarPuntoAPuntoTXTCommand+�� $!��Acreditar Tanda 1 Henderson��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��1��$��0���� -   $  btnEjecutarTanda1Henderson��$��TANDA 1��$��10q�#��[ $��Center=�$��1��$��1������ *#!EjecutarTanda1HendersonTXTCommand+�� $!��Acreditar Tanda 2 Henderson��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��2��$��0���� -   $  btnEjecutarTanda2Henderson��$��TANDA 2��$��10q�#��[ $��Center=�$��2��$��1������ *#!EjecutarTanda2HendersonTXTCommand+�� $��Acreditar Día a Día��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��3��$��0���� -   $ btnEjecutarDiaADia��$	��DXD��$��10q�#��[ $��Center=�$��3��$��1������ *EjecutarDiaADiaTXTCommand+�� $,��&Acreditar De las Sierras (Día a día)��$��20(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��4��$��0���� -   $ btnEjecutarDeLasSierras��$��DXD SIERRAS��$��10q�#��[ $��Center=�$��4��$��1������ * EjecutarDeLasSierrasTXTCommand+� $��1�� �� $
��Auto ��� $��* �.+��� $��
20,10,20,0q�._��� $��EXCEL��$��24(�$
��Bold"�$��HP Simplified)�$��Center=�  $��1��$
 Dark=�$��Stretch=�$��Stretch=�$��20q�.���� $
��Auto=�.���  �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto � �� $��* ��� $
��Auto �.+��� $��Tanda 1 Henderson��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��0��$��0���� $��Generar Excel T1��$��10q�#��[ $��Center=�$��0��$��1������ *%#EjecutarTanda1HendersonExcelCommand+�� $��Tanda 2 Henderson��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��1��$��0���� $��Generar Excel T2��$��10q�#��[ $��Center=�$��1��$��1������ *%#EjecutarTanda2HendersonExcelCommand+�� $��Tanda 1 TESORERÍA��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��2��$��0���� $��Generar Excel T1 TESO��$��10q�#��[ $��Center=�$��2��$��1������ *%#EjecutarTanda1ExcelTesoreriaCommand+�� $��Tanda 2 TESORERÍA��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��3��$��0���� $��Generar Excel T2 TESO��$��10q�#��[ $��Center=�$��3��$��1������ *%#EjecutarTanda2ExcelTesoreriaCommand+�� $��Día a Día��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��4��$��0���� $��Generar Excel DXD��$��10q�#��[ $��Center=�$��4��$��1������ *EjecutarDiaADiaExcelCommand+�� $��Reporte Diario��$��16(�$��HP Simplified)�$��10q�$��Right=�$��Center=�$��5��$��0���� $��Generar Excel DXD��$��10q�#��[ $��Center=�$��5��$��1������ *EjecutarReporteDiarioCommand+