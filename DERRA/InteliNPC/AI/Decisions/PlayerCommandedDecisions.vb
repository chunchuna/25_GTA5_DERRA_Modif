Imports GTA
Imports GTA.Math
Imports GTA.UI

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
            Private ReadOnly target As Ped
            Private time As Integer
            Public Sub New(target As Ped)
                Me.target = target
            End Sub

            Public Overrides Sub Run()
                Invoker.Ped.Task.Combat(target)
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                If target.IsDead Then
                    Return True
                ElseIf Not Invoker.Ped.IsInCombat Then
                    Return True
                ElseIf Not Invoker.PositionChanged Then
                    time += 1
                    Return time > 3
                Else
                    Return False
                End If
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