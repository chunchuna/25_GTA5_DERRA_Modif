Imports GTA
Imports GTA.Chrono
Imports GTA.UI

Namespace Movie
    ''' <summary>
    ''' 托管玩家自动作战。
    ''' </summary>
    Public Class PlayerCombatAgent
        Inherits Script
        Public Shared ReadOnly Property Active As IToggle = New CombatOption()
        Public Shared ReadOnly Property AttackAnyPed As Toggle = New Toggle(True)
        Private last_target As Ped
        Private ticks As Integer = 0
        Public Sub New()
            Interval = 2000
        End Sub
        Private Sub PlayerCombatAgent_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            If Active.Enabled Then
                If Not PlayerPed.IsInCombat Then
                    '搜索敌人
                    SearchAndMarkEnermy()
                Else
                    '正在攻击敌方，但不知道敌方是否死亡
                    If PlayerPed.CombatTarget IsNot Nothing Then
                        Dim combat_target As Ped = PlayerPed.CombatTarget
                        If combat_target.IsDead Then '如果目标已死亡
                            Notification.PostTicker("~r~目标~w~已死亡，重新搜索目标", False)
                            SearchAndMarkEnermy()
                        ElseIf PlayerPed.IsShooting OrElse PlayerPed.IsAiming Then
                            'Notification.PostTicker("~r~使用爆炸子弹技能", False)
                            PlayerPed.Task.ShootAt(combat_target.Position, 800, FiringPattern.SingleShot)
                            World.AddExplosion(combat_target.Position, ExplosionType.ExplosiveAmmo, 1, 0, aubidble:=True)
                            Wait(800)
                            'Notification.PostTicker("~g~使用技能后重新搜索目标", False)
                            SearchAndMarkEnermy()
                        End If
                    End If
                End If
            End If

        End Sub
        ''' <summary>
        ''' 搜索并标注敌人。如果没有找到敌人则逃离此地。
        ''' </summary>
        ''' <returns></returns>
        Private Function SearchAndMarkEnermy() As Boolean
            Dim game_time = GameClock.TimeOfDay
            Dim time As String = $"{game_time.Hour}:{game_time.Minute}"
            Dim target_found As Boolean = False
            For Each ped As Ped In World.GetNearbyPeds(PlayerPed, 50)
                If ped.IsAlive AndAlso ped.RelationshipGroup <> PlayerPed.RelationshipGroup Then
                    If PlayerPed.Position.Z - ped.Position.Z > 10 Then
                        Continue For '不攻击埋在地底下的敌人
                    End If
                    If (AttackAnyPed.Enabled AndAlso Game.Player.WantedLevel <= 3) OrElse ped.IsInCombat OrElse {Relationship.Hate, Relationship.Dislike}.Contains(PlayerPed.GetRelationshipWithPed(ped)) OrElse ped.Model.IsGangPed Then
                        PlayerPed.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Hate)
                        target_found = True
                    End If
                End If
            Next
            If Not target_found Then
                '没有找到合适的敌人，撤离
                If Not PlayerPed.IsFleeing Then
                    PlayerPed.Task.FleeFrom(PlayerPed.Position, 1000, 99999, False)
                    Notification.PostTicker(time + " 附近没有敌人，撤离该区域", True)
                End If
            Else
                Notification.PostTicker(time + " 发现~r~敌人", True)
                PlayerPed.Task.CombatHatedTargetsAroundPed(50, TaskCombatFlags.UseSurprisedAimIntro)
            End If
            Return target_found
        End Function
        Private Class CombatOption
            Inherits Toggle
            Public Sub New()
                MyBase.New(False)
            End Sub
            Public Overrides Property Enabled As Boolean
                Get
                    Return MyBase.Enabled
                End Get
                Set(value As Boolean)
                    If value Then
                        '自动作战
                        Game.Player.SetControlState(False, SetPlayerControlFlags.PreventEverybodyBackOff Or SetPlayerControlFlags.LeaveCameraControlOn) '关闭手动控制角色
                        PlayerPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, False)
                        PlayerPed.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
                        PlayerPed.SetFleeAttributes(FleeAttributes.UseVehicle, True)
                        PlayerPed.SetFleeAttributes(FleeAttributes.WanderAtEnd, True)
                        PlayerPed.SetCombatAttribute(CombatAttributes.Aggressive, True)
                        PlayerPed.SetCombatAttribute(CombatAttributes.AlwaysFight, True)
                        PlayerPed.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, True)
                        PlayerPed.SetCombatAttribute(CombatAttributes.CanUseCover, False)
                        PlayerPed.CombatAbility = CombatAbility.Professional
                        PlayerPed.CombatRange = CombatRange.Far
                        PlayerPed.TargetLossResponse = TargetLossResponse.ExitTask
                        PlayerPed.HearingRange = 50
                        PlayerPed.SeeingRange = 50
                        Notification.PostTicker("已开启自动作战", True)
                    Else
                        '关闭自动作战
                        Game.Player.SetControlState(True) '开启手动控制角色
                        PlayerPed.Task.ClearAll()
                    End If
                    MyBase.Enabled = value
                End Set
            End Property
        End Class
    End Class
End Namespace

