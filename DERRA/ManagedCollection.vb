Public Class ManagedCollection(Of T)
    Implements IEnumerable(Of T)
    Private ReadOnly list As List(Of T) = New List(Of T)()
    Private ReadOnly keep As Predicate(Of T)

    Public Sub New(keep As Predicate(Of T))
        Me.keep = keep
    End Sub
    Public ReadOnly Property Count As Integer
        Get
            Return list.Count
        End Get
    End Property
    Public Sub Update()
        For Each entity As T In list.ToArray()
            If Not keep(entity) Then
                list.Remove(entity)
            End If
        Next
    End Sub
    Public Sub Add(item As T)
        list.Add(item)
    End Sub
    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return list.ToList.GetEnumerator()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
End Class
