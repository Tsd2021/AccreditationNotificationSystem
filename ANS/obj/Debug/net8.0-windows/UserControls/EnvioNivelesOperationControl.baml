   M S B A M L   `   `   `  ����  ANS3    -ANS.UserControls.EnvioNivelesOperationControl   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5      X TXceed.Wpf.Toolkit, Version=4.7.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5        � 	Resources  5
          BooleanToVisibilityConverter  ModernButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType ��Name �� CornerRadius J� Effect3  -System.Windows.Media.Effects.DropShadowEffect   
BlurRadius	   ShadowDepth
   Opacity �� Triggers P� Property P� Value��%&  &	        &	      �� 5   
   6'   �� 5   
   " ��6*   .�5      ��  � �� �	#FF2196F36+   �� 5       � ��
 �����6+   �� 5       � ��$ 16(�6)   �� 5       � ��$
 12,6q�6(   �� 5       � ��$ 2q�60   �� 5       � ��$	 100��6&   �� 5       � ��
 �   �6,   �� 5       �@�Cursor$
 Hand��6'   �� 5       � ��$ Center=�64   �� 5       � ��$ Center=�62   �� 5       � �� 5      �� 5      " ��6&   .R�5      �� $ border��6!   #��z��5   !   $ 3��5   !    5        5    "   $ 3Q�63   $	 2Q�5!   3   $	
 0.2Q�5"   3   ._�5$      �� $��Center=�6/   $��Center=�5%   /   5&      5'      5)       5*      P� 5+       �@J�IsMouseOver$
 True��61   . �5,      ��  � �� �	#FF64B5F663   5-      P� 5.       � ��$
 True��6/   . �5/      ��  � �� �	#FF0D47A163   50      P� 51       � {�$ False��6/   . �52      ��  �@J�Opacity$	 0.6Q�60   53      55      56      .��59      � .+�5:   
   %  Xceed.Wpf.Toolkit.BusyIndicator     BusyContent$ Cargando...��5;      ���	#80FFFFFF   IsBusy 5:      �� *	IsLoading+.��5?      �  � ColumnDefinitions 5@      �� 5A      $��* �6'   .+�5D      � $��0��6    � RowDefinitions 5E      �� 5F      $��* �6(   �� 5G      $��* �6(   .+�5K      �� $��
20,10,20,0q�6   ._�5L      �� $,��&ENVÍO NOTIFICACIONES POR DESCONEXIÓN��6$   $��24(�5M       $
��Bold"�5N       $��HP Simplified)�5O       $��Center=�5P       5Q      (  "MaterialDesignThemes.Wpf.ColorZone  5R      $��1��6/      Mode$
 Dark=�5S   +   $��Stretch=�5T   +   $��Stretch=�5U   +   $��20q�5V   +   .��5X      �� $
��Auto=�6'   .��5Y      � .+�5Z   "   �� .+�5[   &   �� $��Notificar desconexiones��60   $��20(�5\   (   $��HP Simplified)�5]   (   $��10q�5^   (   $��Center=�5_   (   $��Center=�5`   (   $��0��5a   (   $��0��65   �� 5b   &   -   5         ��Name$ btnEnvioDesconexiones��5b   -   $
��SEND��5c   %   $��10q�5d   %   #��[ 5f   %   $��Center=�5g   %   $��Center=�5h   %   $��0��5i   %   $��0��62   ��5e   %   �� *EjecutarEnvioNiveles+5i   2   5j   #   5k      5l      5m      5n      5o      5p      5q      5r      