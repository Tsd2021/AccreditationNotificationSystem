   M S B A M L   `   `   `  ����>  :ANS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:ANS.ViewModelANS.ViewModel  5   	        ANS.Views.AltaEmailDestino   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   *vmclr-namespace:ANS.ViewModel     5   	     � Title$  Alta Email Destino��5   	   $	��550��5   	   $	��800��5   	    � WindowStartupLocation$ CenterScreen=�5   	    � 	Resources 5       �� 
TargetType �� Triggers P� Property P� Value �� Property �� Value��%�   '��       �� 5   
   " ��6    5      P� 5       �@=�HasError$
 True��69   . �5      ��  � ��
 �  ��67   �� 5       � ��$ 1q�68   5      5      5      	 � DataContext	 5      &    ANS.ViewModel.VMaltaEmailDestino  5   
   .��5      � $��10q�6   
 � ColumnDefinitions
 5   
   �� 5      $��2* �6   �� 5      $��3* �6   .+�5!   
   � $��0��6   $��0,0,10,0q�6    � RowDefinitions 5"      �� 5#      $��* �6    �� 5$      $
��Auto �6    .+�5'      &   System.Windows.Controls.DataGrid     AutoGenerateColumns$ False��5)         CanUserAddRows$ False��5*         SelectionMode$ Single=�5+         SelectionUnit$ FullRow=�5,         
IsReadOnly$
 True��5-      $��0��5.      ��5'      �� *
Clientes+ �� SelectedItem 5(      �� *ClienteSeleccionado+ �� Mode$ TwoWay=�   Columns 5/      0  *System.Windows.Controls.DataGridTextColumn  50      ,  &System.Windows.Controls.DataGridColumn   Header$ ID��6)      
Visibility$ 	Collapsed=�52   )   1  +System.Windows.Controls.DataGridBoundColumn   Binding 51   )   �� *Id+52   )     53      $ Cliente��6)      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 55   )    54   )   �� *Nombre+55   )   57      �� 59       �� Orientation$ 
Horizontal=�6   $��1��5:      $��0,5,0,0q�5;      .+�5<      �� $��Filtrar:��6   $��Center=�6+   �� 5=      $	��200��5>      $��5,0,0,0q�5?      ��5=      �� *FiltroCliente+ �� UpdateSourceTrigger$ PropertyChanged=�5?      5@      5A      � 5D   
   $��1��6   $��10,0,0,0q�6    5E      �� 5F      $
��Auto �6    �� 5H      $
��Auto �6    �� 5J      $
��Auto �6    �� 5L      $
��Auto �6    �� 5N      $
��Auto �6    �� 5P      $
��Auto �6    �� 5R      $
��Auto �6    �� 5T      $
��Auto �6    �� 5V      $
��Auto �6    �� 5X      $��* �6    .+�5]      �� $��Banco:��6   $��0��6%   �� 5^       �� DisplayMemberPath$ NombreBanco��5`      $��1��5a      $��0,0,0,10q�5b      ��5^      �� *Bancos+ 5_      �� *BancoSeleccionado+$ TwoWay=�5b      �� 5e      $��Tipo Acreditación:��6   $��2��61   �� 5f      $ TipoAcreditacion��5h      $��3��5i      $��0,0,0,20q�5j      ��5f      �� *TiposAcreditacion+ 5g      �� *TipoSeleccionado+$ TwoWay=�5j      �� 5m      $��Nuevo Email:��6   $��4��6+   �� 5n      $ 
Horizontal=�6   $��Center=�5o      $��5��5p      $��0,0,0,0q�5q      .+�5r      �� -   5         ��Name$ NuevoEmailTextBox��5r      $	��250��5s      ��5t      �� *
NuevoEmail+$ PropertyChanged=� �� ValidatesOnDataErrors$
 True�� �� NotifyOnValidationError$
 True���� 5x      $��Es Principal��5y      $��10,0,0,0q�5z       d� 	IsChecked 5x      �� *EsPrincipal+$ TwoWay=�5z      5{      �� 5~      $��6��6   $��0,2,0,10q�5      
���  ��$��12(�5�      ��5�      �� *%#(Validation.Errors)[0].ErrorContent+ �� ElementName$ NuevoEmailTextBox���� 5�      $ 
Horizontal=�6   $��Right=�5�      $��7��5�      $��0,0,0,10q�5�      .+�5�      �� $��Guardar��6   $��80��5�      $��0,0,5,0q�5�      ��5�      �� *GuardarCommand+�� 5�      $��Cancelar��6   $��80��5�      ��5�      �� *CancelarCommand+5�      �� 5�      $&�� Emails relacionados actualmente:��6   $
��Bold"�5�      $��8��5�      $��0,10,0,5q�5�        5�      $ False��5�      $ False��5�      $
 True��5�      $��9��5�      ��5�      �� *RelatedEmails+ 5�        5�      $ Email��6)   $ * 5�   )    5�   )   �� *Correo+5�   )   4  .System.Windows.Controls.DataGridCheckBoxColumn  5�      $ Es Principal��6-   $
 Auto 5�   -    5�   -   �� *EsPrincipal+5�   -   5�      5�      5�      5�      