Imports System.Reflection
Imports DERRA.Tasking.Hunting
Imports GTA
Imports LemonUI
Imports LemonUI.Menus

Namespace Menus
    ''' <summary>
    ''' 表示菜单对话框。
    ''' </summary>
    Public Class MenuDialog
        Protected ReadOnly pool As ObjectPool
        Protected ReadOnly menu As NativeMenu
        Public Sub New(title As String)
            pool = New ObjectPool()
            menu = New NativeMenu(title)
            pool.Add(menu)

        End Sub
        ''' <summary>
        ''' 显示菜单直到用户关闭菜单。
        ''' </summary>
        Public Sub ShowDialog()
            menu.Visible = True
            While menu.Visible
                pool.Process()
                Script.Wait(1)
            End While
        End Sub
        Public Function AddMenuItem(title As String, description As String, click As Action, Optional close_menu_after_select As Boolean = False) As NativeItem
            Dim item As New NativeItem(title, description)
            AddHandler item.Activated, Sub()
                                           click.Invoke()
                                           If close_menu_after_select Then
                                               menu.Visible = False
                                           End If
                                       End Sub
            menu.Add(item)
            Return item
        End Function
        Public Function AddSep(text As String) As NativeSeparatorItem
            Dim item As New NativeSeparatorItem(text)
            menu.Add(item)
            Return item
        End Function
        Public Sub AddCheckbox(title As String, description As String, t As IToggle)
            Dim item As NativeCheckboxItem = New NativeCheckboxItem(title, description, t.Enabled)
            AddHandler item.CheckboxChanged, Sub()
                                                 t.Enabled = Not t.Enabled
                                                 item.Checked = t.Enabled
                                             End Sub
            AddHandler item.Selected, Sub()
                                          item.Checked = t.Enabled
                                      End Sub
            menu.Items.Add(item)
        End Sub
        Public Sub SetSelectedIndex(index As Integer)
            menu.SelectedIndex = index
        End Sub
    End Class
    Public Class MenuDialog(Of T As {MenuDialog, New})
        Inherits MenuDialog
        Private Shared ReadOnly menu As T = New T()
        Public Sub New(title As String)
            MyBase.New(title)

        End Sub
        Public Overloads Shared Sub PopUp()
            menu.ShowDialog()
        End Sub
    End Class
    Public Class OptionMenuDialog
        Inherits MenuDialog
        Private ReadOnly options As List(Of NativeCheckboxItem) = New List(Of NativeCheckboxItem)()
        Public Sub New(title As String)
            MyBase.New(title)
        End Sub
        Public Sub AddOption(text As String, check As Boolean, click As Action, Optional description As String = "")
            Dim item As NativeCheckboxItem = New NativeCheckboxItem(text, description, check)
            menu.Add(item)
            AddHandler item.Activated, Sub()
                                           For Each e In options
                                               e.Checked = False
                                           Next
                                           item.Checked = True
                                           click.Invoke()
                                       End Sub
            options.Add(item)
        End Sub
    End Class
End Namespace

