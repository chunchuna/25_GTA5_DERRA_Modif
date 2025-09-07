Imports System.Windows.Forms
Imports GTA
Imports GTA.Native
Imports GTA.UI

Namespace OnlinePlayer
    Public Class HealingScript
        Inherits Script
        Private tab_pressed As Boolean
        Private Sub HealingScript_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            If Not OnlinePlayerOption.EnableMPMode Then
                tab_pressed = False
                Return
            End If
            Dim weapon_wheel_visible As Boolean = Hud.IsComponentActive(HudComponent.WeaponWheel)
            If weapon_wheel_visible AndAlso e.KeyCode = Keys.C Then
                If PlayerPed.Health = PlayerPed.MaxHealth Then
                    UI.Screen.ShowHelpText("生命值已满")
                Else
                    PlayerPed.Health = Clamp(PlayerPed.Health + 20, 0, PlayerPed.MaxHealth)
                End If
            ElseIf weapon_wheel_visible AndAlso e.KeyCode = Keys.V Then
                PlayerPed.Armor = Game.Player.MaxArmor
            End If
        End Sub


        Private Sub HealingScript_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            If OnlinePlayerOption.EnableMPMode.Enabled AndAlso Hud.IsComponentActive(HudComponent.WeaponWheel) Then

                UI.Screen.ShowHelpTextThisFrame("按 ~INPUT_EAT_SNACK~ 吃一份零食~n~按 ~INPUT_USE_ARMOR~ 使用一件防弹衣", False)
            End If
        End Sub
    End Class
End Namespace

