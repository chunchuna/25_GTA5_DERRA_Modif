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

    ' 降低AI上限数量，避免性能问题
    Public Shared ReadOnly MaxBotCount As Integer = 50
    
    ' 添加AI活动监控变量
    Private Shared ReadOnly inactivityTimers As New Dictionary(Of Bot, Integer)()
    Private Shared ReadOnly maxInactivityTime As Integer = 300 ' 5分钟不活动则重置

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
    
    ' 检查AI是否长时间不活动
    Private Sub CheckForInactiveAIs()
        ' 更新所有AI的活动状态
        For Each bot As Bot In BotFactory.Pool.ToArray()
            If Not bot.PositionChanged Then
                ' 如果AI位置没有改变，增加不活动计时器
                If Not inactivityTimers.ContainsKey(bot) Then
                    inactivityTimers(bot) = 0
                Else
                    inactivityTimers(bot) += 1
                End If
                
                ' 如果AI超过最大不活动时间，尝试重置或移除
                If inactivityTimers(bot) > maxInactivityTime Then
                    ' 如果AI在载具中且不动，可能卡住了
                    If bot.Ped.IsInVehicle() Then
                        Try
                            ' 尝试让AI离开载具
                            bot.Ped.Task.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen)
                            inactivityTimers(bot) = 0 ' 重置计时器
                        Catch ex As Exception
                            ' 如果失败，标记为退出游戏
                            bot.ExitGame()
                        End Try
                    Else
                        ' 如果AI不在载具中但长时间不动，尝试重置其行为
                        Try
                            ' 强制AI开始一个新的决策
                            Dim randomDecision = bot.GetAvailableDecision()
                            If randomDecision IsNot Nothing Then
                                bot.ForceStartNewDecision(randomDecision)
                                inactivityTimers(bot) = 0 ' 重置计时器
                            Else
                                ' 如果没有可用的决策，标记为退出游戏
                                bot.ExitGame()
                            End If
                        Catch ex As Exception
                            ' 如果失败，标记为退出游戏
                            bot.ExitGame()
                        End Try
                    End If
                End If
            Else
                ' 如果AI位置有变化，重置不活动计时器
                inactivityTimers(bot) = 0
            End If
        Next
        
        ' 清理已不存在的AI的计时器
        For Each bot In inactivityTimers.Keys.ToArray()
            If Not BotFactory.Pool.Contains(bot) Then
                inactivityTimers.Remove(bot)
            End If
        Next
    End Sub
    
    Private Sub BotPlayerGenerator_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        ' 检查不活动的AI
        CheckForInactiveAIs()
        
        If AutoGenerateBots Then
            BotFactory.Pool.Update()
            If BotFactory.Pool.Count < MaxBotCount Then
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
