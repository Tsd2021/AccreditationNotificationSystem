   M S B A M L   `   `   `  ����  ANS+    %ANS.UserControls.BBVAOperationControl   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5      X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5        � 	Resources  5          BooleanToVisibilityConverter  ModernButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType ��Name �� CornerRadius J� Effect3  -System.Windows.Media.Effects.DropShadowEffect   
BlurRadius	   ShadowDepth
   Opacity �� Triggers P� Property P� Value��%&  &	        &	      �� 5   
   6'   �� 5   
   " ��6*   .�5      ��  � �� �	#FF2196F36+   �� 5       � ��
 �����6+   �� 5       � ��$ 16(�6)   �� 5       � ��$
 12,6q�6(   �� 5       � ��$ 2q�60   �� 5       � ��$	 100��6&   �� 5       � ��
 �   �6,   �� 5       �@�Cursor$
 Hand��6'   �� 5       � ��$ Center=�64   �� 5       � ��$ Center=�62   �� 5       � �� 5      �� 5      " ��6&   .R�5      �� $ border��6!   #��z��5   !   $ 3��5   !    5         5!   "   $ 3Q�63   $	 2Q�5"   3   $	
 0.2Q�5#   3   ._�5%      �� $��Center=�6/   $��Center=�5&   /   5'      5(      5*       5+      P� 5,       �@J�IsMouseOver$
 True��61   . �5-      ��  � �� �	#FF64B5F663   5.      P� 5/       � ��$
 True��6/   . �50      ��  � �� �	#FF0D47A163   51      P� 52       � {�$ False��6/   . �53      ��  �@J�Opacity$	 0.6Q�60   54      56      57      .��5:      � .+�5;   
   %  Xceed.Wpf.Toolkit.BusyIndicator     BusyContent$ Cargando...��5<      ���	#80FFFFFF   IsBusy 5;      �� *	IsLoading+.��5@      �  � ColumnDefinitions 5A      �� 5B      $��* �6'   �� 5C      $��* �6'   .+�5G      � $��0��6    � RowDefinitions 5H      �� 5I      $
��Auto �6(   �� 5J      $��* �6(   .+�5N      �� $��
20,10,20,0q�6   ._�5O      �� $��ACREDITACIONES��6$   $��24(�5P       $
��Bold"�5Q       $��HP Simplified)�5R       $��Center=�5S       5T      (  "MaterialDesignThemes.Wpf.ColorZone  5W      $��1��6/      Mode$
 Dark=�5X   +   $��Stretch=�5Y   +   $��Stretch=�5Z   +   $��20q�5[   +   .��5\      �� $
��Auto=�6'   .��5]      �  5^   "   �� 5_   &   $��* �64   �� 5`   &   $��* �64    5e   "   �� 5f   &   $��* �67   �� 5g   &   $
��Auto �67   .+�5k   "   �� $��Acreditar Punto a Punto��6,   $��20(�5l   (   $��HP Simplified)�5m   (   $��10q�5n   (   $��Right=�5o   (   $��Center=�5p   (   $��0��5q   (   $��0��65   �� 5r   "   -   5         ��Name$ btnEjecutarPuntoAPunto��5r   )   $��TXT P2P��5s   %   $��10q�5t   %   #��[ 5v   %   $��Center=�5w   %   $��0��5x   %   $��1��62   ��5u   %   �� *EjecutarPuntoAPuntoTXTCommand+5x   2   �� 5{   "   $��Acreditar Día a día��6,   $��20(�5|   (   $��HP Simplified)�5}   (   $��10q�5~   (   $��Right=�5   (   $��Center=�5�   (   $��1��5�   (   $��0��65   �� 5�   "   -   5        $  btnEjecutarTanda2Henderson��5�   )   $��TXT DXD��5�   %   $��10q�5�   %   #��[ 5�   %   $��Center=�5�   %   $��1��5�   %   $��1��62   ��5�   %   �� *EjecutarDiaADiaTXTCommand+5�   2   5�      5�      5�      5�      � 5�      $��1��6    5�      �� 5�      $
��Auto �6(   �� 5�      $��* �6(   .+�5�      �� $��
20,10,20,0q�6   ._�5�      �� $��EXCEL��6$   $��24(�5�       $
��Bold"�5�       $��HP Simplified)�5�       $��Center=�5�       5�        5�      $��1��6/   $
 Dark=�5�   +   $��Stretch=�5�   +   $��Stretch=�5�   +   $��20q�5�   +   .��5�      �� $
��Auto=�6'   .��5�      �  5�   "   �� 5�   &   $
��Auto �64   �� 5�   &   $
��Auto �64   �� 5�   &   $
��Auto �64   �� 5�   &   $
��Auto �64   �� 5�   &   $
��Auto �64   �� 5�   &   $
��Auto �64    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $ ��Excel de todos los buzones��6,   $��16(�5�   (   $��HP Simplified)�5�   (   $��10q�5�   (   $��Right=�5�   (   $��Center=�5�   (   $��0��5�   (   $��0��65   �� 5�   "   $��Generar Excel��6)   $��10q�5�   %   #��[ 5�   %   $��Center=�5�   %   $��0��5�   %   $��1��62   ��5�   %   �� *EjecutarExcelDiaADia+5�   2   �� 5�   "   $��Excel solo TATA��6,   $��16(�5�   (   $��HP Simplified)�5�   (   $��10q�5�   (   $��Right=�5�   (   $��Center=�5�   (   $��1��5�   (   $��0��65   �� 5�   "   $��Generar Excel TATA��6)   $��10q�5�   %   #��[ 5�   %   $��Center=�5�   %   $��1��5�   %   $��1��62   ��5�   %   �� *EjecutarExcelTataCommand+5�   2   5�      5�      5�      5�      5�      5�      5�      5�      