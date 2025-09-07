Imports DERRA.InteliNPC.AI
Imports GTA
Imports GTA.Math

Namespace Tasking
    ''' <summary>
    ''' 私人安保控制器。
    ''' </summary>
    Public Class PrivateSecurityController
        Inherits PedHelperBase(Of GuardTask)
        Implements IEntityController
        Private Shared security_relationship_group As RelationshipGroup?
        Private DisposingRequested As Boolean
        Public Sub New(guard As Ped)
            MyBase.New(guard)
        End Sub
        Public Shared ReadOnly Property SecurityRelationshipGroup As RelationshipGroup
            Get
                If Not security_relationship_group.HasValue Then
                    security_relationship_group = World.AddRelationshipGroup("DERRA SEC")
                    security_relationship_group.Value.SetRelationshipBetweenGroups(PlayerPed.RelationshipGroup, Relationship.Companion)
                End If
                Return security_relationship_group
            End Get
        End Property

        Public ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return TargetPed
            End Get
        End Property
        Public Sub BeginDispose()
            DisposingRequested = True
        End Sub
        Private Sub PrivateSecurityController_StateChanged(sender As Object, e As PedStateChangedEventArgs) Handles Me.StateChanged
            If e.NewState = GuardTask.Combat Then
                Dim target As Ped = ThreatDetector.GetNearlistTreat(TargetPed.Position, 50, AddressOf IsThreat)
                If target IsNot Nothing Then
                    TargetPed.LeaveGroup()
                    PlayerPed.PedGroup.Remove(TargetPed)
                    TargetPed.Task.ClearAll()
                    TargetPed.Task.Combat(target, taskThreatResponseFlags:=TaskThreatResponseFlags.CanFightArmedPedsWhenNotArmed)
                Else
                    'Warn("拔剑四顾心茫然")
                End If
            ElseIf e.NewState = GuardTask.FollowOwnerOnFoot Then
                If Not TargetPed.IsInGroup Then
                    PlayerPed.PedGroup.Add(TargetPed, False)
                End If
                TargetPed.Task.FollowToOffsetFromEntity(PlayerPed, Vector3.Zero.Around(5), 2, distanceToFollow:=10)
            ElseIf e.NewState = GuardTask.StayInVehicleWithOwner Then
                If TargetPed.VehicleTryingToEnter Is Nothing Then
                    TargetPed.Task.ClearAll()
                End If
                If Not TargetPed.IsInGroup Then
                    PlayerPed.PedGroup.Add(TargetPed, False)
                End If
                'TargetPed.Task.FollowToOffsetFromEntity(PlayerPed, Vector3.Zero, 2, distanceToFollow:=10)
            ElseIf e.NewState = GuardTask.GuardPosition Then
                TargetPed.Task.ClearAll()
            ElseIf e.NewState = GuardTask.TeleportToOwner Then
                Dim target_position As Vector3 = PlayerPed.Position.Around(5)
                Dim height As Single
                If World.GetGroundHeight(target_position, height) Then
                    TargetPed.Position = New Vector3(target_position.X, target_position.Y, height)
                Else
                    TargetPed.Position = target_position
                End If
            ElseIf e.NewState = GuardTask.OnGround Then
                TargetPed.Weapons.Remove(WeaponHash.Parachute)
            End If
        End Sub

        Public Overrides Function UpdateCurrentState() As GuardTask
            If TargetPed.IsInAir AndAlso Not TargetPed.IsInVehicle Then
                Return GuardTask.Landing
            ElseIf LastState = GuardTask.Landing AndAlso (Not TargetPed.IsInAir OrElse TargetPed.IsInVehicle) Then
                Return GuardTask.OnGround
            End If
            Dim distance_to_owner As Single = TargetPed.Position.DistanceTo(PlayerPed.Position)
            If distance_to_owner > 14 AndAlso Not PlayerPed.IsInVehicle Then
                If distance_to_owner > 50 Then
                    Return GuardTask.TeleportToOwner
                Else
                    Return GuardTask.FollowOwnerOnFoot
                End If
            ElseIf distance_to_owner < 5 AndAlso Not PlayerPed.IsInVehicle AndAlso Not TargetPed.IsInCombat Then
                Return GuardTask.GuardPosition
            ElseIf PlayerPed.IsInVehicle() Then
                Return GuardTask.StayInVehicleWithOwner
            Else
                If Not TargetPed.IsInCombat AndAlso Not TargetPed.IsInMeleeCombat Then
                    If ThreatDetector.FindThreat(TargetPed.Position, 50, AddressOf IsThreat) Then
                        Return GuardTask.Combat
                    ElseIf Not TargetPed.IsWalking AndAlso Not TargetPed.IsRunning Then
                        Return GuardTask.FollowOwnerOnFoot
                    Else
                        Return LastState
                    End If
                Else
                    '已经处于战斗状态，无需重新确定目标
                    Return GuardTask.Combat
                End If
            End If
        End Function

        Public Overrides Function VerifyCurrentState(state As GuardTask) As Boolean
            If state = GuardTask.StayInVehicleWithOwner Then
                If TargetPed.IsInVehicle() AndAlso PlayerPed.IsInVehicle() AndAlso TargetPed.CurrentVehicle <> PlayerPed.CurrentVehicle Then
                    Return False '保镖和玩家都在车上，但不在同一辆车上。
                ElseIf Not TargetPed.IsInVehicle() AndAlso PlayerPed.IsInVehicle() AndAlso Not IsLocationChanged Then
                    '保镖不在车上，玩家在车上，保镖距离玩家太远
                    Dim vehicle As Vehicle = PlayerPed.CurrentVehicle
                    For Each seat As VehicleSeat In [Enum].GetValues(GetType(VehicleSeat))
                        If vehicle.IsSeatFree(seat) Then
                            TargetPed.SetIntoVehicle(vehicle, seat)
                            Exit For
                        End If
                    Next
                    Warn("已瞬移至玩家载具")
                    Return True
                Else
                    Return True
                End If
            ElseIf state = GuardTask.TeleportToOwner Then
                If TargetPed.Position.DistanceTo(PlayerPed.Position) > 50 Then
                    Return True
                Else
                    Return False
                End If
            ElseIf state = GuardTask.OnGround Then
                If TargetPed.ParachuteState = ParachuteState.FreeFalling Then
                    TargetPed.Weapons.Remove(WeaponHash.Parachute)
                    Return False
                End If
            End If
            Return True
        End Function
        Public Shared Function IsThreat(ped As Ped) As Boolean
            If ThreatDetector.IsCommonThreat(ped) Then
                Return True
            Else
                Dim bot As Bot = BotFactory.GetBotByPed(ped)
                If bot IsNot Nothing AndAlso Not bot.IsAlly Then
                    Return True
                Else
                    Return False
                End If
            End If
        End Function

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Return DisposingRequested OrElse TargetPed.IsDead
        End Function

        Public Sub OnTick() Implements IEntityController.OnTick
            Refresh()
            If TargetPed.Health < TargetPed.MaxHealth Then
                TargetPed.Health = TargetPed.MaxHealth
            End If
            Dim blip As Blip = TargetPed.AttachedBlip
            If blip IsNot Nothing AndAlso blip.Exists() Then
                If IsInSameVehicle(TargetPed, PlayerPed) Then
                    blip.DisplayType = BlipDisplayType.NoDisplay
                Else
                    blip.DisplayType = BlipDisplayType.MiniMapOnly
                End If
            End If

        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
        End Sub

        Public Enum GuardTask
            GuardPosition = 0
            FollowOwnerOnFoot = 1
            StayInVehicleWithOwner = 2
            Combat = 3
            TeleportToOwner = 4
            Landing = 5
            OnGround = 6
        End Enum
    End Class
End Namespace

