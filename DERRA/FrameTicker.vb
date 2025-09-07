Imports System.Collections.Concurrent
Imports GTA
Imports GTA.Native

Public Class FrameTicker
    Inherits Script
    Private Shared ReadOnly Items As List(Of ITickProcessable) = New List(Of ITickProcessable)()
    Private Shared ReadOnly unadded As ConcurrentQueue(Of ITickProcessable) = New ConcurrentQueue(Of ITickProcessable)()
    ''' <summary>
    ''' 获取或设置一个<see cref="Boolean"/>值，指示<see cref="Tick"/>是否暂停处理<see cref="Items"/>。
    ''' </summary>
    Public Shared Property PauseProcessing As Boolean = False
    Public Sub New()
        Interval = 10
    End Sub
    Public Shared Sub Add(item As ITickProcessable)
        unadded.Enqueue(item)
    End Sub
    Private Sub FrameTicker_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If PauseProcessing Then
            Return
        End If
        For Each item As ITickProcessable In Items.ToArray()
            If item.CanBeRemoved() Then
                Items.Remove(item)
            Else
                item.Process()
            End If
        Next
        While unadded.Count > 0
            Dim item As ITickProcessable = Nothing
            If unadded.TryDequeue(item) Then
                If Not IsNothing(item) Then
                    Items.Add(item)
                End If
            Else
                Exit While
            End If
        End While
    End Sub
End Class
Public Interface ITickProcessable
    Sub Process()
    Function CanBeRemoved() As Boolean
End Interface
