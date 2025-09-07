''' <summary>
''' 实现<see cref="IToggle"/>接口。
''' </summary>
Public Class Toggle
    Implements IToggle
    Private value As Boolean
    Public Property OnEnable As Action
    Public Property OnDisable As Action
    Public Sub New(value As Boolean)
        Me.value = value
    End Sub

    Public Overridable Property Enabled As Boolean Implements IToggle.Enabled
        Get
            Return value
        End Get
        Set(value As Boolean)
            If value = True Then
                OnEnable?.Invoke()
            Else
                OnDisable?.Invoke()
            End If
            Me.value = value
        End Set
    End Property
    Public Shared Operator Not(t As Toggle) As Boolean
        Return Not t.Enabled
    End Operator
    Public Shared Narrowing Operator CType(t As Toggle) As Boolean
        Return t.Enabled
    End Operator
End Class
