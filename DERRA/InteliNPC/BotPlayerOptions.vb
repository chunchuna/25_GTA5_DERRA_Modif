Imports System.IO
Imports DERRA.InteliNPC.AI
Imports GTA
Imports GTA.Native
Imports GTA.UI

Public Class BotPlayerOptions
    Inherits Script
    Public Shared ReadOnly AutoGenerateBots As Toggle = New Toggle(False)
    Public Shared ReadOnly CanBotRegenerate As Toggle = New Toggle(True)
    Public Shared ReadOnly UseOnlineCharacterModel As Toggle = New Toggle(True)
    Public Shared ReadOnly AdaptiveBot As Toggle = New Toggle(True)

    Private Shared ReadOnly safeWeapons As WeaponHash() = {WeaponHash.CombatShotgun, WeaponHash.SniperRifle, WeaponHash.HeavySniperMk2,
                                                   WeaponHash.HeavyShotgun, WeaponHash.UnholyHellbringer, WeaponHash.SpecialCarbineMk2, WeaponHash.UpNAtomizer, WeaponHash.NavyRevolver,
                                                   WeaponHash.DoubleActionRevolver, WeaponHash.HeavyRifle, WeaponHash.MilitaryRifle, WeaponHash.ServiceCarbine,
                                                   WeaponHash.CombatMG, WeaponHash.CombatMGMk2, WeaponHash.PistolMk2, WeaponHash.HeavyPistol, WeaponHash.Revolver,
                                                   WeaponHash.RevolverMk2}
    Private Shared ReadOnly safeHeavyWeapons As WeaponHash() = {WeaponHash.GrenadeLauncher, WeaponHash.RPG, WeaponHash.HomingLauncher, WeaponHash.Widowmaker, WeaponHash.Minigun}
    Public Sub New()
        Interval = 1234
    End Sub
    Public Shared Sub Weeker()
        If safeWeapons.Count > 1 Then
            For Each bot As Bot In BotFactory.Pool
                Dim weapon As WeaponHash = Pick(safeWeapons)
                If Not bot.OwnedWeapons.Contains(weapon) Then
                    bot.OwnedWeapons.Give(weapon, Pick(75)) '由于选中的武器不可能是重型武器，所以不需要
                End If
                Dim safeHeavyWeapon As WeaponHash = Pick(safeHeavyWeapons)
                If Not bot.OwnedWeapons.Contains(safeHeavyWeapon) Then
                    bot.OwnedWeapons.Give(safeHeavyWeapon, Pick(75))
                    bot.OwnedWeapons.KnownRocketWeapons.Add(safeHeavyWeapon)
                End If
                bot.Accuracy += 15
            Next
        End If

    End Sub
    Public Shared Sub Better()
        For Each bot As Bot In BotFactory.Pool
            If Not bot.IsAlly AndAlso bot.OwnedWeapons.Count > 2 Then
                Dim item As WeaponHash = Pick(bot.OwnedWeapons.Hashes.ToArray())
                bot.OwnedWeapons.SafeRemove(item)
                'Notification.PostTicker("移除武器:" + item.ToString, True) '发行时注释掉此行
                bot.Accuracy -= 15
            End If
        Next
    End Sub
    Private Sub BotPlayerGenerator_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        If AutoGenerateBots Then
            BotFactory.Pool.Update()
            If BotFactory.Pool.Count < 50 Then
                If UseOnlineCharacterModel Then
                    BotFactory.CreateRandomOnlinePlayer() '.CreateBot()
                Else
                    BotFactory.CreateBot()
                End If
            End If
        End If
        If AdaptiveBot.Enabled AndAlso PlayerPed.IsDead AndAlso BotFactory.IsBot(PlayerPed.Killer) Then
            Better()
        End If
    End Sub
End Class
