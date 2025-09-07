Imports System.IO
Imports System.Windows.Forms
Imports GTA
Imports GTA.Native
Imports GTA.UI

Public Class PlayerStyle
    Inherits Script
    Private show_cash As Boolean
    Private display_player_name As String
    Public Sub New()
        Interval = 1
    End Sub
    Private Sub PlayerStyle_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Z Then
            show_cash = Not show_cash
            display_player_name = Not display_player_name
            [Function].Call(Hash.DISPLAY_PLAYER_NAME_TAGS_ON_BLIPS, display_player_name)
            'Notification.PostTicker("余额:$" & Game.Player.Money, True)
        End If
    End Sub

    Private Sub PlayerStyle_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If show_cash Then
            UI.Hud.ShowComponentThisFrame(HudComponent.Cash)
        End If
    End Sub
End Class
