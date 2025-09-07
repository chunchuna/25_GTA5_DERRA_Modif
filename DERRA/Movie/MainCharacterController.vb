Imports GTA
Imports GTA.Chrono
Imports GTA.UI

Namespace Movie
    Public Class MainCharacterController
        Implements IEntityController
        Private ReadOnly character As Ped
        Public Shared ReadOnly Active As Toggle = New Toggle(False)
        Public Shared ReadOnly AttackAnyPed As Toggle = New Toggle(True)
        Public Shared ReadOnly ActiveSkills As Toggle = New Toggle(False)
        Public Sub New(character As Ped)
            Me.character = character
        End Sub

        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return character
            End Get
        End Property
        ''' <summary>
        ''' 搜索并标注敌人。如果没有找到敌人则逃离此地。
        ''' </summary>
        ''' <returns></returns>
        Private Function SearchAndMarkEnermy() As Boolean
            'Dim game_time = GameClock.TimeOfDay
            Dim time As String = Now
            Dim target_found As Boolean = False
            Dim target_ped As Ped = Nothing, target_distance As Single = Single.PositiveInfinity
            For Each ped As Ped In World.GetNearbyPeds(character, 50)
                If ped.IsAlive AndAlso ped.RelationshipGroup <> character.RelationshipGroup Then
                    If character.Position.Z - ped.Position.Z > 10 Then
                        Continue For '不攻击埋在地底下的敌人
                    End If
                    If AttackAnyPed.Enabled OrElse ped.IsInCombat OrElse {Relationship.Hate, Relationship.Dislike}.Contains(character.GetRelationshipWithPed(ped)) OrElse ped.Model.IsGangPed Then
                        Dim distance As Single = character.Position.DistanceTo(ped.Position)
                        target_found = True
                        If distance < target_distance Then
                            target_ped = ped
                            target_distance = distance
                        End If
                    End If
                End If
            Next
            If Not target_found Then
                '没有找到合适的敌人，撤离
                If Not character.IsFleeing Then
                    character.Task.FleeFrom(PlayerPed.Position, 1000, 99999, False)
                    Notification.PostTicker(time + " 附近没有敌人，撤离该区域", True)
                End If
            Else
                Notification.PostTicker(time + " 发现~r~敌人", True)
                character.Task.Combat(target_ped)
            End If
            Return target_found
        End Function
        Public Sub OnTick() Implements IEntityController.OnTick
            If Active.Enabled Then
                If Not character.IsInCombat Then
                    '搜索敌人
                    Dim game_time = GameClock.TimeOfDay
                    Dim time As String = $"{game_time.Hour}:{game_time.Minute}"
                    Notification.PostTicker(time + " 非战斗状态，搜索目标", True)
                    SearchAndMarkEnermy()
                Else
                    '正在攻击敌方，但不知道敌方是否死亡
                    If character.CombatTarget IsNot Nothing Then
                        Dim combat_target As Ped = character.CombatTarget
                        If combat_target.IsDead Then '如果目标已死亡
                            Notification.PostTicker("~r~目标~w~已死亡，重新搜索目标", False)
                            SearchAndMarkEnermy()
                        ElseIf character.IsShooting OrElse character.IsAiming Then
                            If ActiveSkills.Enabled Then
                                Notification.PostTicker(Now + "~r~使用爆炸子弹技能", False)
                                character.Task.ShootAt(combat_target.Position, 800, FiringPattern.SingleShot)
                                World.AddExplosion(combat_target.Position, ExplosionType.ExplosiveAmmo, 1, 0, aubidble:=True)
                                Script.Wait(800)
                                Notification.PostTicker(Now + "~g~使用技能后重新搜索目标", False)
                                SearchAndMarkEnermy()
                            End If
                        End If
                    End If
                End If
            End If
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Return character.IsDead
        End Function

        Public Sub Disposing() Implements IEntityController.Disposing
        End Sub
    End Class
End Namespace

