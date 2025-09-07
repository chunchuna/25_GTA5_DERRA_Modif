Imports GTA
Imports GTA.Math
Imports GTA.UI
Imports System

Namespace InteliNPC.AI.Decisions
    Public Class PlayerCommandedDecision
        Inherits BotDecision
        Public Sub New()
            MyBase.New("(特殊任务)")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return False
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Throw New NotImplementedException()
        End Function
    End Class
    Public Class AttackDecision
        Inherits PlayerCommandedDecision
        Private ReadOnly target As Ped
        Public Sub New(target As Ped)
            Me.target = target
        End Sub

        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            If bot.Ped.IsInVehicle() Then
                Return True
            Else
                Return bot.Ped.Position.DistanceTo(target.Position) < 50
            End If
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New AttackAction(target)
        End Function
        Private Class AttackAction
            Inherits BotAction
            Implements ITickProcessable

            Private ReadOnly target As Ped
            Private time As Integer ' For checking if stuck

            Private Enum CombatState
                Shooting
                Strafing
            End Enum

            Private currentState As CombatState
            Private stateEndTimestamp As Integer
            Private Shared ReadOnly Rng As New Random()

            Public Sub New(target As Ped)
                Me.target = target
            End Sub

            Public Overrides Sub Run()
                FrameTicker.Add(Me)
                ' Start with shooting
                SwitchToState(CombatState.Shooting)
            End Sub

            Private Sub SwitchToState(newState As CombatState)
                currentState = newState

                If Not target.Exists() OrElse target.IsDead OrElse Not Invoker.Ped.Exists() Then
                    Return
                End If

                Invoker.Ped.Task.ClearAll() ' Clear previous tasks

                Select Case currentState
                    Case CombatState.Shooting
                        ' Shoot one bullet, then wait a little
                        stateEndTimestamp = Game.GameTime + 750 ' Total time in this state
                        Invoker.Ped.Task.ShootAt(target, 250, FiringPattern.SingleShot)
                    Case CombatState.Strafing
                        ' Strafe for 1 to 2 seconds
                        stateEndTimestamp = Game.GameTime + Rng.Next(1000, 2001)
                        ' Alternate left and right
                        Dim strafeDirection = If(Rng.Next(2) = 0, Invoker.Ped.RightVector, -Invoker.Ped.RightVector)
                        Dim strafePosition = Invoker.Ped.Position + strafeDirection * 5.0F
                        Invoker.Ped.Task.RunTo(strafePosition)
                End Select
            End Sub

            Public Sub Process() Implements ITickProcessable.Process
                If Not target.Exists() OrElse target.IsDead OrElse Not Invoker.Ped.Exists() Then
                    Return
                End If

                ' Keep facing target while strafing
                If currentState = CombatState.Strafing Then
                    If target.Exists() Then
                        Dim dir As Vector3 = (target.Position - Invoker.Ped.Position)
                        dir.Normalize()
                        Invoker.Ped.Heading = dir.ToHeading()
                    End If
                End If

                If Game.GameTime >= stateEndTimestamp Then
                    ' Time to switch state
                    If currentState = CombatState.Shooting Then
                        SwitchToState(CombatState.Strafing)
                    Else
                        SwitchToState(CombatState.Shooting)
                    End If
                End If
            End Sub

            Public Overrides Sub Dispose()
                ' FrameTicker removes automatically via CanBeRemoved
                If Invoker.Ped.Exists() Then
                    Invoker.Ped.Task.ClearAll()
                End If
            End Sub

            Public Function CanBeRemoved() As Boolean Implements ITickProcessable.CanBeRemoved
                Return IsCompleted()
            End Function

            Public Overrides Function IsCompleted() As Boolean
                If target Is Nothing OrElse Not target.Exists() OrElse target.IsDead Then
                    Return True
                End If

                If Not Invoker.Ped.Exists() OrElse Not Invoker.Ped.IsAlive OrElse Not Invoker.Ped.IsInCombat Then
                    Return True
                End If

                ' If the ped gets stuck, end the action.
                If Not Invoker.PositionChanged Then
                    time += 1
                    If time > 60 Then ' stuck for about 1 second (assuming 60fps)
                        Return True
                    End If
                Else
                    time = 0
                End If

                Return False
            End Function
        End Class
    End Class
    ''' <summary>
    ''' 玩家召见。
    ''' </summary>
    Public Class VisitPlayerDecision
        Inherits PlayerCommandedDecision
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return False
        End Function
        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New VisitPlayerAction()
        End Function
        Private Class VisitPlayerAction
            Inherits BotAction
            Private position_not_changed As Integer
            Private destination As Vector3?
            Public Overrides Sub Run()
                If Invoker.Ped.Position.DistanceTo(PlayerPed.Position) > 100 Then
                    Invoker.Ped.Position = World.GetNextPositionOnStreet(PlayerPed.Position.Around(100))
                End If
                destination = World.GetNextPositionOnStreet(PlayerPed.Position.Around(20))
                Invoker.Ped.Task.ClearAll()
                Invoker.Ped.Task.GoToPointAnyMeans(destination, PedMoveBlendRatio.Run, Invoker.Ped.CurrentVehicle, True, VehicleDrivingFlags.DrivingModePloughThrough)
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                If Invoker.Ped.Position.DistanceTo(destination) < 20 Then
                    Notification.PostTicker(Invoker.Name + " 已抵达玩家召见的位置", False)
                    Return True
                End If
                If Not Invoker.PositionChanged Then
                    If Invoker.Ped.IsOnFoot Then
                        Notification.PostTicker(Invoker.Name + " 途中发生意外，取消任务", False)
                        Return True
                    End If
                    position_not_changed += 1
                    If position_not_changed > 3 Then
                        'Notification.PostTicker(Invoker.Name + " 途中发生意外，取消任务", False)
                        Run()
                        Return True
                    Else
                        Return False
                    End If

                Else
                    Return False
                End If
            End Function
        End Class
    End Class
    Public Class WaitForPlayerEnterVehicleDecision
        Inherits PlayerCommandedDecision
        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New WaitAction()
        End Function
        Private Class WaitAction
            Inherits BotAction
            Private waited_times As Integer = 0
            Public Overrides Sub Run()
                If Invoker.Ped.IsInVehicle() Then
                    Invoker.Ped.Task.ClearAll()
                    Invoker.Ped.CurrentVehicle.IsPositionFrozen = True
                End If
            End Sub
            Public Overrides Sub Dispose()
                Dim v = Invoker.Ped.CurrentVehicle
                If v IsNot Nothing Then
                    v.IsPositionFrozen = False
                End If
            End Sub
            Public Overrides Function IsCompleted() As Boolean
                waited_times += 1
                If waited_times > 20 Then
                    Notification.PostTicker(Invoker.Name + " 等待超时", True)
                    Return True
                ElseIf Not Invoker.Ped.IsInVehicle() Then
                    Notification.PostTicker(Invoker.Name + " 离开载具", True)
                    Return True
                ElseIf PlayerPed.IsInVehicle(Invoker.Ped.CurrentVehicle) Then
                    Notification.PostTicker("玩家已进入 " + Invoker.Name + " 的载具", True)
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class
    End Class
End Namespace