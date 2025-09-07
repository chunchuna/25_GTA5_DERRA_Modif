Imports GTA
''' <summary>
''' 表示<see cref="Entity"/>的集合。
''' </summary>
''' <typeparam name="T">实体类型。</typeparam>
Public Class EntityCollection(Of T As Entity)
    Implements IEnumerable(Of T)
    Private ReadOnly list As List(Of T) = New List(Of T)()
    Public Property MaxDistance As Single?
    Public Sub Keep(rule As KeepRule)
        For Each entity As T In list.ToArray()
            If rule = KeepRule.MustAlive Then
                If Not entity.IsAlive Then
                    list.Remove(entity)
                    Continue For
                End If
            ElseIf rule = KeepRule.MustExists Then
                If Not entity.Exists() Then
                    list.Remove(entity)
                    Continue For
                End If
            End If
        Next
    End Sub
    Public Sub Add(item As T)
        list.Add(item)
    End Sub
    Public ReadOnly Property Count As Integer
        Get
            Return list.Count
        End Get
    End Property
    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return list.ToList.GetEnumerator()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
End Class
Public Enum KeepRule
    MustAlive = 0
    MustExists = 1
End Enum
