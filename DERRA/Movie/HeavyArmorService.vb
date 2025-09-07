Imports System.Windows.Forms
Imports GTA
Imports GTA.Native
Imports GTA.UI

Namespace Movie
    Public Class HeavyArmorService
        Public Shared Sub StartHeavyArmor()
            Static last_time As Date?
            If Not last_time.HasValue OrElse Now.Subtract(last_time).TotalMinutes > 2 Then

                Dim ped As Ped = World.CreatePed(PedHash.Juggernaut02UMY, PlayerPed.Position.Around(2), PlayerPed.Heading)
                For Each weapon As WeaponHash In [Enum].GetValues(GetType(WeaponHash))
                    ped.Weapons.Give(weapon, 9999, False, False)
                Next
                ped.Style.SetDefaultClothes()
                ped.Armor = 2000
                Start(ped, "重甲单位已投放")
                last_time = Now
            Else
                UI.Screen.ShowSubtitle("~r~操作频繁，请稍后再试")
            End If

        End Sub
        Public Shared Sub Start(ped As Ped, msg As String)
            Dim original_player_ped As Ped = PlayerPed
            Game.Player.SetControlState(True, SetPlayerControlFlags.ReenableControlOnDeath Or SetPlayerControlFlags.AllowPlayerDamage)
            [Function].Call(Hash.CHANGE_PLAYER_PED, Game.Player, ped, False, True)
            UI.Screen.ShowHelpText(msg + vbNewLine + "长按 E 退出")
            [Function].Call(Hash.SET_FADE_OUT_AFTER_DEATH, False)

            While ped.IsAlive AndAlso original_player_ped.IsAlive
                Script.Wait(1000)
                If Game.IsKeyPressed(Keys.E) Then
                    Exit While
                End If
            End While
            If Not ped.IsAlive Then
                UI.Screen.ShowHelpText("您已被消灭")
            End If
            If Not original_player_ped.IsAlive Then
                UI.Screen.ShowHelpText("玩家已死亡")
            End If
            [Function].Call(Hash.FORCE_GAME_STATE_PLAYING)
            [Function].Call(Hash.CHANGE_PLAYER_PED, Game.Player, original_player_ped, False, True)
            [Function].Call(Hash.SET_FADE_OUT_AFTER_DEATH, True)
            Game.Player.WantedLevel = 0
            ped.MarkAsNoLongerNeeded()
        End Sub
    End Class
End Namespace

