   M S B A M L   `   `   `  ���� >  :ANS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:ANS.ViewModelANS.ViewModel       ANS.Views.AltaEmailDestino   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     *vmclr-namespace:ANS.ViewModel       � Title$  Alta Email Destino��$	��550��$	��800�� � WindowStartupLocation$ CenterScreen=� � 	Resources  �� 
TargetType �� Triggers P� Property P� Value �� Property �� Value��%i   '��       �� " �� P�  �@=�HasError$
 True��. ���  � ��
 �  ����  � ��$ 1q�	 � DataContext	 &    ANS.ViewModel.VMaltaEmailDestino  .��� $��10q�
 � ColumnDefinitions
 �� $��2* ��� $��3* �.+�� $��0��$��0,0,10,0q� � RowDefinitions �� $��* ��� $
��Auto �.+�&   System.Windows.Controls.DataGrid     AutoGenerateColumns$ False��   CanUserAddRows$ False��   SelectionMode$ Single=�   SelectionUnit$ FullRow=�   
IsReadOnly$
 True��$��0������ *
Clientes+ �� SelectedItem �� *ClienteSeleccionado+ �� Mode$ TwoWay=�   Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn   Header$ ID��   
Visibility$ 	Collapsed=�1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *Id+  $ Cliente��   Width5  /System.Windows.Controls.DataGridLengthConverter$ *  �� *Nombre+��  �� Orientation$ 
Horizontal=�$��1��$��0,5,0,0q�.+��� $��Filtrar:��$��Center=��� $	��200��$��5,0,0,0q����� *FiltroCliente+ �� UpdateSourceTrigger$ PropertyChanged=�� $��1��$��10,0,0,0q� �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $��* �.+��� $��Banco:��$��0����  �� DisplayMemberPath$ NombreBanco��$��1��$��0,0,0,10q����� *Bancos+ �� *BancoSeleccionado+$ TwoWay=��� $��Tipo Acreditación:��$��2���� $ TipoAcreditacion��$��3��$��0,0,0,20q����� *TiposAcreditacion+ �� *TipoSeleccionado+$ TwoWay=��� $��Nuevo Email:��$��4���� $ 
Horizontal=�$��Center=�$��5��$��0,0,0,0q�.+��� -    ��Name$ NuevoEmailTextBox��$	��250������ *
NuevoEmail+$ PropertyChanged=� �� ValidatesOnDataErrors$
 True�� �� NotifyOnValidationError$
 True���� $��Es Principal��$��10,0,0,0q� d� 	IsChecked �� *EsPrincipal+$ TwoWay=��� $��6��$��0,2,0,10q�
���  ��$��12(����� *%#(Validation.Errors)[0].ErrorContent+ �� ElementName$ NuevoEmailTextBox���� $ 
Horizontal=�$��Right=�$��7��$��0,0,0,10q�.+��� $��Guardar��$��80��$��0,0,5,0q����� *GuardarCommand+�� $��Cancelar��$��80������ *CancelarCommand+�� $&�� Emails relacionados actualmente:��$
��Bold"�$��8��$��0,10,0,5q�  $ False��$ False��$
 True��$��9������ *RelatedEmails+   $ Email��$ *  �� *Correo+4  .System.Windows.Controls.DataGridCheckBoxColumn  $ Es Principal��$
 Auto  �� *EsPrincipal+