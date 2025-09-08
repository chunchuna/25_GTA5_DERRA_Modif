Imports GTA
Imports GTA.Native
Imports System.Windows.Forms

''' <summary>
''' Manages the online map display functionality
''' </summary>
Public Class OnlineMapManager
    Inherits Script

    ''' <summary>
    ''' Constructor for the OnlineMapManager class
    ''' </summary>
    Public Sub New()
        ' Set up event handlers
        AddHandler Me.Tick, AddressOf OnTick
        AddHandler Me.KeyDown, AddressOf OnKeyDown

        ' Apply online map mode on startup
        Map.OnlineMapMode.ApplyMapMode()
    End Sub

    ''' <summary>
    ''' Called every game tick
    ''' </summary>
    Private Sub OnTick(sender As Object, e As EventArgs)
        ' Ensure map mode settings are applied periodically
        Static lastReapplyTime As DateTime = DateTime.Now
        
        ' Reapply every 10 seconds to ensure settings persist
        If (DateTime.Now - lastReapplyTime).TotalSeconds >= 10 Then
            Map.OnlineMapMode.ApplyMapMode()
            lastReapplyTime = DateTime.Now
        End If
    End Sub

    ''' <summary>
    ''' Handles key press events
    ''' </summary>
    Private Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        ' Check for Shift+M key combination to toggle expanded map
        If e.KeyCode = Keys.M AndAlso e.Shift Then
            Map.OnlineMapMode.ShowFullMap = Not Map.OnlineMapMode.ShowFullMap
            
            If Map.OnlineMapMode.ShowFullMap Then
                UI.Screen.ShowSubtitle("Expanded map view enabled", 2000)
            Else
                UI.Screen.ShowSubtitle("Normal map view enabled", 2000)
            End If
        End If
    End Sub
End Class 