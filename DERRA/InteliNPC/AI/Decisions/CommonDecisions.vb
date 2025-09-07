Imports System.Drawing
Imports System.Threading
Imports DERRA.Structs
Imports DERRA.Tasking
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports GTA.UI

Namespace InteliNPC.AI.Decisions
    ''' <summary>
    ''' 直接逃跑。
    ''' </summary>
    Public Class FleeDecision
        Inherits BotDecision
        Public Sub New()
            MyBase.New("闲逛")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return True
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New FleeAction()
        End Function
        Private Class FleeAction
            Inherits BotAction
            Private tick As Integer = 0
            Public Overrides Sub Run()
                If Invoker.Ped.IsInVehicle() Then
                    Invoker.Ped.Task.CruiseWithVehicle(Invoker.Ped.CurrentVehicle, 60, VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless)
                Else
                    Invoker.Ped.SetFleeAttributes(GTA.FleeAttributes.CanScream, False)
                    Invoker.Ped.SetFleeAttributes(GTA.FleeAttributes.DisableCover, True)
                    Invoker.Ped.SetFleeAttributes(GTA.FleeAttributes.DisableExitVehicle, True)
                    Invoker.Ped.SetFleeAttributes(GTA.FleeAttributes.UseVehicle, True)
                    Invoker.Ped.Task.FleeFrom(Invoker.Ped.Position, 999, 99999)
                End If
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                tick += 1
                If tick > 60 Then
                    Return True '超时
                ElseIf Not Invoker.PositionChanged AndAlso Not Invoker.Ped.IsInCombat AndAlso Invoker.Ped.VehicleTryingToEnter Is Nothing Then
                    '呆呆的站着
                    Bot.Log(Invoker.Name, "Ped空闲，结束任务", "o")
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class
    End Class
    ''' <summary>
    ''' 呼叫并进入个人载具。
    ''' </summary>
    Public Class CallAndEnterPersonalVehicleDecision
        Inherits BotDecision
        Private ReadOnly need_weaponized As Boolean
        Public Sub New(need_weaponized As Boolean)
            MyBase.New("呼叫个人载具 - " + If(need_weaponized, "武装载具", "普通载具"))
            Me.need_weaponized = need_weaponized
        End Sub

        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            If need_weaponized Then
                If bot.UsingVechie IsNot Nothing AndAlso bot.UsingVechie.IsAlive AndAlso bot.Ped.IsInVehicle(bot.UsingVechie) Then
                    Return False '已经坐在载具中了，我也不想判断是不是武装载具
                Else
                    Return True
                End If
            Else
                If bot.Ped.IsInVehicle() Then
                    Return False '凑合着用行了
                Else
                    Return True
                End If
            End If
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New EnterVehicleAction(need_weaponized)
        End Function
        Private Class EnterVehicleAction
            Inherits BotAction
            Private ReadOnly needs_weaponized As Boolean
            Private enermy As Ped
            Public Sub New(needs_weaponized As Boolean)
                Me.needs_weaponized = needs_weaponized
            End Sub
            Private Sub DeleteCurrentVehicle()
                If Invoker.UsingVechie IsNot Nothing Then
                    If Invoker.UsingVechie.Exists() Then
                        '先把司机送出去
                        If Invoker.UsingVechie.Driver IsNot Nothing OrElse Invoker.UsingVechie.PassengerCount > 0 Then
                            Invoker.UsingVechie.MarkAsNoLongerNeeded()
                        Else
                            Invoker.UsingVechie.Delete()
                        End If
                    End If
                End If
            End Sub
            Public Overrides Sub Run()
                enermy = Invoker.Ped.CombatTarget
                [Function].Call(Hash.SET_PED_KEEP_TASK, Invoker.Ped, True)
                If Invoker.UsingVechie Is Nothing OrElse Invoker.UsingVechie.IsDead OrElse Invoker.UsingVechie.Position.DistanceTo(Invoker.Ped.Position) > 50 Then
                    '重新呼叫一辆载具
                    'Invoker.UsingVechie?.Delete()
                    DeleteCurrentVehicle()
                    Invoker.UsingVechie = CreateVehicle(Invoker.OwnedVehicles.GetRandomVehicleModel(needs_weaponized), 20, Invoker.Ped)
                    If Invoker.UsingVechie Is Nothing Then
                        Return
                    End If
                    Invoker.UsingVechie.Mods.PrimaryColor = PickEnum(VehicleColor.Blue)
                    Invoker.UsingVechie.Mods.SecondaryColor = PickEnum(VehicleColor.Blue)
                    Invoker.UsingVechie.Mods.LicensePlateStyle = PickEnum(LicensePlateStyle.BlueOnWhite3)
                    Invoker.UsingVechie.Mods.WindowTint = PickEnum(VehicleWindowTint.Invalid)
                    Invoker.UsingVechie.Mods.WheelType = Pick(Invoker.UsingVechie.Mods.AllowedWheelTypes)
                    Invoker.UsingVechie.Mods.TireSmokeColor = Pick({Color.White, Color.Green, Color.Aqua, Color.Pink, Color.Orange, Color.Yellow})
                    If Pick({True, False}) Then '有几率搞这个NeonLight
                        For Each neonlight As VehicleNeonLight In [Enum].GetValues(GetType(VehicleNeonLight))
                            Invoker.UsingVechie.Mods.SetNeonLightsOn(neonlight, True)
                        Next
                    End If
                    If Invoker.UsingVechie.Model <> VehicleHash.Khanjari Then '因为似乎AI不知道怎么使用电磁炮，总是蓄力不发射。
                        Invoker.UsingVechie.Mods.InstallModKit()
                        For Each mod_type As VehicleModType In [Enum].GetValues(GetType(VehicleModType))
                            If mod_type = VehicleModType.None OrElse mod_type = VehicleModType.FrontWheel OrElse mod_type = VehicleModType.RearWheel OrElse mod_type = VehicleModType.SteeringWheels Then
                                Continue For
                            End If
                            Dim v_mod As VehicleMod = Invoker.UsingVechie.Mods.Item(mod_type)
                            If v_mod.Count > 0 Then
                                v_mod.Index = Pick(v_mod.Count - 1)
                            End If
                        Next
                    End If
                End If
                If CanPlayerSee(Invoker.Ped) OrElse CanPlayerSee(Invoker.UsingVechie) Then
                    '无法作弊，老老实实走过去
                    Invoker.Ped.Task.EnterVehicle(Invoker.UsingVechie, VehicleSeat.Driver, speed:=2)
                Else
                    '直接传送进去
                    Invoker.Ped.SetIntoVehicle(Invoker.UsingVechie, VehicleSeat.Driver)
                End If
            End Sub
            Public Overrides Function IsCompleted() As Boolean
                If Invoker.Ped.IsInVehicle() Then
                    If enermy IsNot Nothing Then
                        Invoker.Ped.Task.Combat(enermy)
                    End If
                    Return True
                ElseIf Not Invoker.PositionChanged Then
                    Bot.Log(Invoker.Name, "进入载具卡死,取消动作", "o")
                    If enermy IsNot Nothing Then
                        Invoker.Ped.Task.Combat(enermy)
                    End If
                    Return True
                Else
                    Return False
                End If
            End Function
            Public Overrides Sub Dispose()
                [Function].Call(Hash.SET_PED_KEEP_TASK, Invoker.Ped, False)
            End Sub
        End Class
    End Class
    Public Class VisitDecision
        Inherits BotDecision
        Private ReadOnly destinations As Vector3()
        Private ReadOnly arrival As Action(Of Bot)
        Public Sub New(name As String, destinations As Vector3(), arrival As Action(Of Bot))
            MyBase.New(name)
            Me.destinations = destinations
            Me.arrival = arrival
        End Sub
        Private Function SelectDestination(bot As Bot) As Minimize(Of Vector3)
            Dim destination As Minimize(Of Vector3) = New Minimize(Of Vector3)()
            For Each op As Vector3 In destinations
                destination.TryNext(bot.Ped.Position.DistanceTo2D(op), op)
            Next
            Return destination
        End Function
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Dim destination = SelectDestination(bot)
            If destination.HasSelectAny Then
                Dim distance As Single = destination.MinScore
                If distance > 50 AndAlso Not bot.Ped.IsInVehicle Then
                    Return False '你飞过去吗
                Else
                    Return True
                End If
            Else
                Return False
            End If
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New VisitAction(arrival, SelectDestination(bot).Selected)
        End Function
        Private Class VisitAction
            Inherits BotAction
            Private ReadOnly arrival As Action(Of Bot)
            Private ReadOnly destination As Vector3

            Public Sub New(arrival As Action(Of Bot), destination As Vector3)
                Me.arrival = arrival
                Me.destination = destination
            End Sub

            Public Overrides Sub Run()
                If destination.DistanceTo(Invoker.Ped.Position) < 50 Then
                    If Invoker.Ped.Position.DistanceTo(PlayerPed.Position) < 200 Then
                        Invoker.Ped.Task.RunTo(destination)
                    Else
                        Invoker.Ped.Position = destination
                    End If

                Else
                    Invoker.Ped.Task.DriveTo(Invoker.Ped.CurrentVehicle, World.GetNextPositionOnStreet(destination), 50.0F, VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless, 20)
                End If
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                Dim distance As Single = destination.DistanceTo(Invoker.Ped.Position)
                If distance < 10 Then

                    Bot.Log(Invoker.Name, "抵达目的地")
                    arrival.Invoke(Invoker)
                    Return True
                Else
                    If Not Invoker.PositionChanged Then
                        If Invoker.Ped.IsInVehicle() AndAlso distance < 50 Then
                            Invoker.Ped.Task.RunTo(destination)
                            Return False
                        Else
                            Bot.Log(Invoker.Name, "未知原因Ped空闲", "o")
                            Return True
                        End If
                    Else
                        Return False
                    End If
                End If
            End Function
        End Class
    End Class
    Public Class WeaponShopDecision
        Inherits BotDecision
        Private ReadOnly weaponHash As WeaponHash
        Private ReadOnly price As Integer
        Private ReadOnly shot_range As Single
        Private ReadOnly is_rocket As Boolean
        Public Sub New(weapon As WeaponHash, price As Integer, shot_range As Single, Optional is_rocket As Boolean = False)
            MyBase.New("购买 " + GTA.Weapon.GetHumanNameFromHash(weapon))
            Me.weaponHash = weapon
            Me.price = price
            Me.shot_range = shot_range
            Me.is_rocket = is_rocket
        End Sub

        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            If bot.OwnedWeapons.Contains(weaponHash) Then
                Return False
            ElseIf bot.Money < price Then
                Return False
            End If
            For Each location As Vector3 In Map.GunShops
                If bot.Ped.Position.DistanceTo(location) < If(PlayerPed.Position.DistanceTo(bot.Ped.Position) < 200, 10, 100) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Bot.Log(bot.Name, "购买 ~r~" + Weapon.GetHumanNameFromHash(weaponHash))
            If is_rocket Then
                bot.OwnedWeapons.KnownRocketWeapons.Add(weaponHash)
            End If
            Return New WeaponPurchaceAction(weaponHash, price, shot_range)
        End Function
        Private Class WeaponPurchaceAction
            Inherits BotAction
            Private ReadOnly weapon As WeaponHash
            Private ReadOnly price As Integer
            Private ReadOnly shot_range As Single
            Public Sub New(weapon As WeaponHash, price As Integer, shot_range As Single)
                Me.weapon = weapon
                Me.price = price
                Me.shot_range = shot_range
            End Sub

            Public Overrides Sub Run()
                Invoker.Money -= price
                Invoker.OwnedWeapons.Give(weapon, shot_range)
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                Return True
            End Function
        End Class
    End Class
    Public Class RobDecision
        Inherits BotDecision
        Public Sub New()
            MyBase.New("抢劫路人")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return True
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New RobAction()
        End Function
        Private Class RobAction
            Inherits BotAction
            Private tick As Integer = 0
            Public Overrides Sub Run()
            End Sub
            Private Function IsTargetDead(ped As Ped) As Boolean
                If ped.IsDead Then
                    Return True
                ElseIf ped.Health < 10 Then
                    Return True
                Else
                    Return False
                End If
            End Function
            Public Overrides Function IsCompleted() As Boolean
                tick += 1
                If tick > 60 Then
                    Return True
                Else
                    Dim is_in_combat As Boolean = Invoker.Ped.IsInCombat
                    Dim combat_target As Ped = Invoker.Ped.CombatTarget
                    Dim combat_target_dead As Boolean = is_in_combat AndAlso combat_target IsNot Nothing AndAlso IsTargetDead(combat_target)

                    If Not is_in_combat OrElse combat_target_dead Then
                        Bot.Log(Invoker.Name, $"需要更换目标，is_in_combat={is_in_combat},combat_target_dead={combat_target_dead}")
                        Dim enermy As Ped = ThreatDetector.GetNearlistTreat(Invoker.Ped.Position, 50, AddressOf CanBeRobed)
                        If enermy IsNot Nothing Then
                            If Not enermy.IsPlayer AndAlso Not EntityManagement.ContainsEntity(enermy) AndAlso Not enermy.IsInVehicle Then
                                enermy.Task.HandsUp(5000)
                            End If
                            Bot.Log(Invoker.Name, "目标切换为:" + Hex(enermy.Handle))
                            Invoker.Ped.Task.ClearAll()
                            Invoker.Ped.Task.Combat(enermy)
                        Else
                            If Not Invoker.PositionChanged Then
                                Invoker.Ped.Task.Wander()
                                Invoker.Money += 1000
                            End If
                        End If
                    End If
                    Return False
                End If
            End Function
            Private Function CanBeRobed(ped As Ped) As Boolean
                If Invoker.IsAlly Then
                    If ped.IsPlayer OrElse ped.RelationshipGroup = Invoker.Ped.RelationshipGroup Then
                        Return False
                    End If
                End If
                Return ped <> Invoker.Ped AndAlso ped.IsHuman AndAlso ped.Health > 10
            End Function
        End Class
    End Class
    Public Class KillerDecision
        Inherits BotDecision
        Private ReadOnly vehicleModel As Model?
        Private ReadOnly goonModel As Model
        Private ReadOnly goonWeapon As WeaponHash

        Public Sub New(vehicleModel As Model?, goonModel As Model, goonWeapon As WeaponHash, name As String)
            MyBase.New($"暗杀任务 - {name}")
            Me.vehicleModel = vehicleModel
            Me.goonModel = goonModel
            Me.goonWeapon = goonWeapon
        End Sub

        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return bot.Ped.IsInVehicle()
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New KillerAction(vehicleModel, goonModel, goonWeapon)
        End Function
        Private Class KillerAction
            Inherits BotAction
            Private ReadOnly vehicleModel As Model?
            Private ReadOnly goonModel As Model
            Private ReadOnly goonWeapon As WeaponHash
            Private primary_target As Ped
            Private ReadOnly goons As List(Of Ped) = New List(Of Ped)()
            Private tick As Integer = 0
            Public Sub New(vehicleModel As Model?, goonModel As Model, goonWeapon As WeaponHash)
                Me.vehicleModel = vehicleModel
                Me.goonModel = goonModel
                Me.goonWeapon = goonWeapon
            End Sub

            Public Overrides Sub Run()
                Dim vehicle As Vehicle = CreateVehicle(vehicleModel, 800)
                For Each seat As VehicleSeat In [Enum].GetValues(GetType(VehicleSeat))
                    If vehicle.IsSeatFree(seat) Then
                        Dim goon As Ped = vehicle.CreatePedOnSeat(seat, goonModel)
                        If goon Is Nothing Then
                            Continue For
                        End If
                        goon.Accuracy = 10
                        goon.RelationshipGroup = EnermyRelationshipGroup
                        goon.Weapons.Give(goonWeapon, 900, True, True)
                        goon.KeepTaskWhenMarkedAsNoLongerNeeded = True
                        goons.Add(goon)
                    End If
                Next
                vehicle.Driver?.Task?.Combat(Invoker.Ped) 'CruiseWithVehicle(vehicle, 20, VehicleDrivingFlags.DrivingModeAvoidVehiclesObeyLights)
                vehicle.MarkAsNoLongerNeeded()
                primary_target = vehicle.Driver
                Invoker.Ped.Task.Combat(vehicle.Driver)
            End Sub
            Public Overrides Sub Dispose()
                For Each ped As Ped In goons
                    ped.MarkAsNoLongerNeeded()
                Next
            End Sub
            Public Overrides Function IsCompleted() As Boolean
                If primary_target Is Nothing OrElse primary_target.IsDead Then
                    Invoker.Money += 50000
                    Return True
                Else
                    If Not CanPlayerSee(Invoker.Ped) Then
                        tick += 1
                    End If
                    If Not Invoker.Ped.IsInCombat Then 'Ped主动取消任务
                        Bot.Log(Invoker.Name, "Ped主动取消暗杀任务")
                        Invoker.Money += 50000
                        Return True
                    ElseIf Invoker.Ped.Position.DistanceTo(primary_target.Position) > 50 AndAlso Not Invoker.Ped.IsInVehicle() Then
                        Bot.Log(Invoker.Name, "暗杀任务已无法完成")
                        Invoker.Money += 50000
                        Return True '任务已无法完成
                    ElseIf tick > 60 Then
                        Bot.Log(Invoker.Name, "暗杀任务已超时")
                        Invoker.Money += 50000
                        Return True
                    Else
                        Return False
                    End If

                End If
            End Function
        End Class
    End Class
    Public Class VehiclePurchaceDecision
        Inherits BotDecision
        Private ReadOnly price As Integer
        Private ReadOnly vehicleModel As VehicleHash
        Private ReadOnly is_weaponized As Boolean
        Private ReadOnly model As Model
        Private Shared ReadOnly model_support As Dictionary(Of Model, Boolean) = New Dictionary(Of Model, Boolean)()
        Public Sub New(price As Integer, vehicleModel As VehicleHash, is_weaponized As Boolean)
            MyBase.New("购买载具 - " + Vehicle.GetModelDisplayName(vehicleModel))
            model = vehicleModel
            Me.price = price
            Me.vehicleModel = vehicleModel
            Me.is_weaponized = is_weaponized
        End Sub
        Private Sub RegModel()
            model.Request()
            Dim watch As Stopwatch = New Stopwatch()
            watch.Start()
            While Not model.IsLoaded
                Script.Wait(10)
                model.Request()
                If watch.Elapsed.TotalSeconds > 1 Then
                    model_support.Item(model) = False
                    UI.Notification.PostTicker("警告:您的游戏不包含DLC载具:" + vehicleModel.ToString() + "。", True)
                    Return
                End If
            End While
            model_support.Item(model) = True
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            If Not model_support.ContainsKey(model) Then
                RegModel()
            End If
            Return model_support(model) AndAlso bot.Money >= price
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New VehiclePurchaceAction(price, vehicleModel, is_weaponized)
        End Function
        Private Class VehiclePurchaceAction
            Inherits BotAction
            Private ReadOnly price As Integer
            Private ReadOnly vehicleModel As VehicleHash
            Private ReadOnly is_weaponized As Boolean

            Public Sub New(price As Integer, vehicleModel As VehicleHash, is_weaponized As Boolean)
                Me.price = price
                Me.vehicleModel = vehicleModel
                Me.is_weaponized = is_weaponized
            End Sub

            Public Overrides Sub Run()
                Invoker.Money -= price
                Invoker.OwnedVehicles.Add(vehicleModel, is_weaponized)
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                Return True
            End Function
        End Class
    End Class
    Public Class JetDecision
        Inherits BotDecision
        Public Sub New()
            MyBase.New("驾驶战斗机")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            '统计地图上的飞机数量，超过一定数量将无法创建新的飞机
            Dim planes As Integer = 0
            For Each item As Bot In BotFactory.Pool
                If item.Ped.IsAlive AndAlso item.Ped.IsInFlyingVehicle Then
                    planes += 1
                End If
            Next
            Return planes < 6 AndAlso Not bot.IsAlly AndAlso bot.Ped.Position.DistanceTo(PlayerPed.Position) > 1000 AndAlso bot.Money > 30000 '后期加入其他限制条件
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New JetAction()
        End Function
        Private Class JetAction
            Inherits BotAction
            Implements ITickProcessable
            Private jet As Vehicle
            Private enermy As Ped
            Private Sub PerformAttack()
                If enermy Is Nothing OrElse enermy.IsDead Then
                    Dim enermy As Minimize(Of Ped) = New Minimize(Of Ped)()
                    If Not Invoker.IsAlly AndAlso Game.Player.IsAlive OrElse PlayerPed.IsAlive Then
                        enermy.TryNext(PlayerPed.Position.DistanceTo2D(Invoker.Ped.Position), PlayerPed)
                    End If
                    For Each controller As IEntityController In EntityManagement.Controllers
                        If controller.Target.IsAlive AndAlso controller.Target.EntityType = EntityType.Ped AndAlso controller.Target <> Invoker.Ped Then
                            enermy.TryNext(controller.Target.Position.DistanceTo2D(Invoker.Ped.Position), controller.Target)
                        End If
                    Next
                    If enermy.HasSelectAny Then
                        Me.enermy = enermy.Selected
                        Invoker.Ped.Task.StartPlaneMission(jet, enermy.Selected, VehicleMissionType.Attack, 100, 250, 100, 50)
                    End If
                End If
            End Sub
            Public Overrides Sub Run()
                Invoker.CustomBlipDisplayType = BlipDisplayType.NoDisplay
                Dim ground_position As Vector3 = Invoker.Ped.Position
                Dim styles As Dictionary(Of VehicleHash, BlipSprite) = New Dictionary(Of VehicleHash, BlipSprite)()
                'styles.Add(VehicleHash.Besra, BlipSprite.Jet)
                styles.Add(VehicleHash.Strikeforce, BlipSprite.B11StrikeForce)
                styles.Add(VehicleHash.Starling, BlipSprite.Starling)
                styles.Add(VehicleHash.Lazer, BlipSprite.Jet)
                styles.Add(VehicleHash.Molotok, BlipSprite.V65Molotok)
                'styles.Add(VehicleHash.Luxor, BlipSprite.Plane)
                styles.Add(VehicleHash.Luxor2, BlipSprite.Plane)
                'styles.Add(VehicleHash.Oppressor2, BlipSprite.OppressorMkII)
                Dim style = Pick(styles.ToArray())
                jet = World.CreateVehicle(style.Key, New Vector3(ground_position.X, ground_position.Y, ground_position.Z + 800), Vector3.RandomXY().ToHeading())
                If jet Is Nothing Then
                    Script.Wait(1000)
                    Run()
                    Return
                End If
                With jet.AddBlip()
                    .Sprite = style.Value
                    .Name = Invoker.Name
                    .Color = Invoker.Ped.AttachedBlip.Color
                    .CategoryType = BlipCategoryType.OtherPlayers
                    If Invoker.IsAlly Then
                        .ShowsFriendIndicator = True
                        .ShowsCrewIndicator = True
                    End If
                End With
                Invoker.Ped.SetIntoVehicle(jet, VehicleSeat.Driver)
                Invoker.Ped.AttachedBlip.DisplayType = BlipDisplayType.NoDisplay
                FrameTicker.Add(Me)
                PerformAttack()
            End Sub

            Public Sub Process() Implements ITickProcessable.Process
                If jet IsNot Nothing Then
                    If jet.IsAlive AndAlso jet.AttachedBlip?.Exists() Then
                        jet.AttachedBlip.RotationFloat = jet.Heading
                    End If
                End If
            End Sub
            Public Overrides Sub Dispose()
                Invoker.CustomBlipDisplayType = Nothing
                jet?.MarkAsNoLongerNeeded()
                jet?.AttachedBlip?.Delete()
                'Invoker.Ped.AttachedBlip.DisplayType = BlipDisplayType.Default
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                If CanBeRemoved() Then
                    Return True
                Else
                    PerformAttack()
                    Return False
                End If
            End Function

            Public Function CanBeRemoved() As Boolean Implements ITickProcessable.CanBeRemoved
                If jet Is Nothing Then
                    Return False
                End If
                Return jet.IsDead OrElse Not Invoker.Ped.IsInVehicle(jet)
            End Function
        End Class
    End Class
    Public Class EnterPlayerVehicleDecision
        Inherits BotDecision
        Public Sub New()
            MyBase.New("进入玩家乘坐的载具")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return bot.IsAlly AndAlso bot.Ped.Position.DistanceTo(PlayerPed.Position) < 10 AndAlso PlayerPed.CurrentVehicle IsNot Nothing
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New EnterSameVehicleAction()
        End Function
        Private Class EnterSameVehicleAction
            Inherits BotAction

            Public Overrides Sub Run()
                Dim player_car As Vehicle = PlayerPed.CurrentVehicle
                If player_car IsNot Nothing Then
                    [Function].Call(Hash.SET_PED_KEEP_TASK, Invoker.Ped, True)
                    Invoker.Ped.Task.EnterVehicle(PlayerPed.CurrentVehicle, VehicleSeat.Passenger, timeout:=10 * 1000, speed:=2, flag:=EnterVehicleFlags.JackAnyone)
                End If

            End Sub
            Public Overrides Sub Dispose()
                [Function].Call(Hash.SET_PED_KEEP_TASK, Invoker.Ped, False)
            End Sub
            Public Overrides Function IsCompleted() As Boolean
                If Invoker.Ped.VehicleTryingToEnter IsNot Nothing Then
                    If Not Invoker.PositionChanged Then
                        Notification.PostTicker(Invoker.Name + " 无法进入玩家的载具", True)
                        Return True
                    Else
                        Return False
                    End If
                Else
                    Dim player_vehicle As Vehicle = PlayerPed.CurrentVehicle
                    If player_vehicle IsNot Nothing AndAlso Invoker.Ped.IsInVehicle(player_vehicle) Then
                        Return False
                    Else
                        Return True
                    End If
                End If
            End Function
        End Class
    End Class
    Public Class BeWantedDecision
        Inherits BotDecision
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return Not bot.WantedAmount.HasValue AndAlso Not bot.IsAlly AndAlso BotFactory.Pool.AsEnumerable().Count(Function(item) item.WantedAmount.HasValue) < 3
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New BeWantedAction()
        End Function
        Private Class BeWantedAction
            Inherits BotAction
            Public Overrides Sub Run()
                Invoker.WantedAmount = Pick({2000, 3000, 5000, 7000, 6500, 10000})
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                Return True
            End Function
        End Class
    End Class
    Public Class EmptyDecision
        Inherits BotDecision
        Public Shared ReadOnly [Default] As EmptyDecision = New EmptyDecision()
        Public Sub New()
            MyBase.New("准备就绪")
        End Sub
        Public Overrides Function IsAvaliableFor(bot As Bot) As Boolean
            Return False
        End Function

        Public Overrides Function GetAction(bot As Bot) As BotAction
            Return New EmptyAction() With {.DecisionName = Name, .Invoker = bot}
        End Function
        Private Class EmptyAction
            Inherits BotAction
            Public Overrides Sub Run()
                Pass()
            End Sub

            Public Overrides Function IsCompleted() As Boolean
                Return True
            End Function
        End Class
    End Class
End Namespace

