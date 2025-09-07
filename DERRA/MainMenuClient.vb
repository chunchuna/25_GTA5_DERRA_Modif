
Imports System.Collections.ObjectModel
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar
Imports DERRA.Ineractive.War
Imports DERRA.InteliNPC.AI
Imports DERRA.InteliNPC.AI.Decisions
Imports DERRA.InteliNPC.Combat
Imports DERRA.Interactive.Horro
Imports DERRA.Menus
Imports DERRA.Movie
Imports DERRA.OnlinePlayer
Imports DERRA.Structs
Imports DERRA.Tasking
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports GTA.UI
Imports LemonUI
Imports LemonUI.Menus
<ScriptAttributes(Author:="DERRA", SupportURL:="https://space.bilibili.com/688486093")>
Public Class MainMenuClient
    Inherits Script
    Private ReadOnly pool As ObjectPool
    Private ReadOnly menu As NativeMenu
    Public Sub New()
        pool = New ObjectPool()
        menu = New NativeMenu("DERRA") With {.Description = "DERRA"}
        pool.Add(menu)
        AddMenu("chunchun modif mod", "", AddressOf AuthorInfo)
        Dim enableAntiNPCDriver As NativeCheckboxItem = New NativeCheckboxItem("防别车补丁", AntiNpcDriverPatch.Enabled)
        AddHandler enableAntiNPCDriver.CheckboxChanged, Sub()
                                                            AntiNpcDriverPatch.Enabled = Not AntiNpcDriverPatch.Enabled
                                                            enableAntiNPCDriver.Checked = AntiNpcDriverPatch.Enabled
                                                        End Sub
        menu.Items.Add(enableAntiNPCDriver)
        AddMenu("载具", "访问个人载具选项", AddressOf VehicleMenuDialog.PopUp)
        AddMenu("人机玩家", "访问人机玩家控制菜单", AddressOf BotMenu.PopUp, True)
        AddMenu("玩家选项", "", AddressOf PlayerMenu.PopUp)
        'AddMenu("~b~安保人员", "访问安保人员派遣菜单", AddressOf BotActorsDialog.PopUp)
        AddMenu("派出安保护送车辆", "派遣4名士兵前往玩家位置支援", AddressOf DispatchEmergencySquard)
        'AddMenu("投放重甲单位", "重甲单位或故事模式角色任意一方死亡将自动结束", AddressOf HeavyArmorService.StartHeavyArmor)
        AddMenu("运行实体清理", "释放所有由当前菜单创建的对象", Sub()
                                                If BotPlayerOptions.CanBotRegenerate.Enabled OrElse BotPlayerOptions.AutoGenerateBots.Enabled Then
                                                    menu.Visible = False
                                                    UI.Screen.ShowHelpText("需要先关闭~h~'人机玩家'~h~中的2个选项，然后重新~h~'运行实体清理'~h~，否则~r~可能将发生未知异常")
                                                    Return
                                                End If
                                                UI.Screen.FadeOut(1000)
                                                Wait(1000)
                                                Try
                                                    FrameTicker.PauseProcessing = True
                                                    EntityManagement.PauseProcessing = True
                                                    Wait(3000)
                                                    EntityManagement.Reset()
                                                Catch ex As Exception
                                                End Try
                                                FrameTicker.PauseProcessing = False
                                                EntityManagement.PauseProcessing = False
                                                UI.Screen.FadeIn(1000)
                                                Wait(1000)
                                                UI.Screen.ShowHelpText("清理完成")
                                                menu.Visible = False
                                            End Sub)
        'AddMenu("查看当前路径", "", Sub() Notification.PostTicker(Environment.CurrentDirectory, False))
        'AddMenu("查看服装代码", "创建一个服装代码文件保存到桌面", AddressOf ShowClothes)
        'AddMenu("保存当前位置代码", "将当前位置代码保存到桌面", AddressOf SaveLocation)
    End Sub
    Private Sub AddMenu(text As String, description As String, action As Action, Optional auto_close_menu As Boolean = False)
        Dim item As NativeItem = New NativeItem(text, description)
        AddHandler item.Activated, Sub()
                                       action.Invoke
                                       If auto_close_menu Then
                                           menu.Visible = False
                                       End If
                                   End Sub
        menu.Add(item)
    End Sub
    Private Sub MainMenu_Tick(sender As Object, e As EventArgs) Handles Me.Tick
        pool.Process()
    End Sub
    Private Sub MainMenuClient_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.B Then
            menu.Visible = Not menu.Visible
        ElseIf e.KeyCode = Keys.U Then
            UpgradeNearbyPed()
        ElseIf e.KeyCode = Keys.N Then
            BotMenu.AutoEnterVehicle()
        End If
    End Sub
    Private Shared Sub UpgradeNearbyPed()
        Dim selected_ped As Ped = ThreatDetector.GetNearlistTreat(PlayerPed.Position, 20, Function(ped)
                                                                                              Return Not ped.IsPlayer AndAlso ped.IsOnScreen AndAlso Not EntityManagement.ContainsEntity(ped) AndAlso Not ped.IsInVehicle
                                                                                          End Function)
        If selected_ped IsNot Nothing Then
            'selected_ped.BlocksAnyDamageButHasReactions = True
            If Pick({True, False}) Then
                selected_ped.Euphoria.Electrocute.Start(3000)
                Wait(3000)
                selected_ped.Task.StandStill(2000)
                Wait(2000)
                Dim clone As Ped = selected_ped.Clone(True)
                clone.Heading = selected_ped.Heading
                Wait(800)
                selected_ped.Delete()
                'selected_ped.BlocksAnyDamageButHasReactions = False
                BotFactory.UpgradePedToBot(clone)
            Else
                '[Function].Call(Hash.EXPLODE_PED_HEAD, selected_ped, CUInt(WeaponHash.StunGun))
                selected_ped.Weapons.Give(WeaponHash.StunGun, 1, False, False)
                World.ShootSingleBullet(selected_ped.AbovePosition, selected_ped.Position, 9999, WeaponHash.StunGun)
                Wait(3000)
                selected_ped.Kill()
                UI.Screen.ShowHelpText("升级失败")
            End If

        End If
    End Sub
    Private Sub DispatchEmergencySquard()
        Static last_time As Date? = Nothing
        Dim agents As EntityCollection(Of Ped) = New EntityCollection(Of Ped)()
        If Not last_time.HasValue OrElse (Now - last_time.Value).TotalMinutes > 2 Then
            Dim bus As Vehicle = CreateVehicle(Pick({VehicleHash.Baller5, VehicleHash.Granger, VehicleHash.Granger2}), 100)
            bus.Rotation = PlayerPed.Position - bus.Position
            bus.PlaceOnNextStreet()
            bus.CanTiresBurst = False
            bus.CanWheelsBreak = False
            bus.Mods.WindowTint = VehicleWindowTint.PureBlack
            bus.Mods.PrimaryColor = VehicleColor.MetallicBlack
            bus.Mods.SecondaryColor = VehicleColor.MetallicBlack
            Dim driver As Ped = bus.CreatePedOnSeat(VehicleSeat.Driver, PedHash.FreemodeMale01)
            RedHat(driver)
            driver.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
            driver.Weapons.Give(WeaponHash.SMG, 600, True, True)
            'driver.Task.DriveTo(bus, PlayerPed.Position, 50, VehicleDrivingFlags.DrivingModePloughThrough, 20)
            'driver.Task.VehicleEscort(bus, PlayerPed, VehicleEscortType.Rear, 50, VehicleDrivingFlags.DrivingModeAvoidVehicles)
            driver.Task.VehicleFollow(bus, PlayerPed, 50, VehicleDrivingFlags.DrivingModeAvoidVehicles Or VehicleDrivingFlags.StopForPeds)
            agents.Add(driver)
            EntityManagement.AddTempAgent(driver)
            For Each seat As VehicleSeat In {VehicleSeat.RightFront, VehicleSeat.RightRear, VehicleSeat.LeftRear}
                If bus.IsSeatFree(seat) Then
                    Dim passenger As Ped = bus.CreatePedOnSeat(seat, PedHash.FreemodeMale01)
                    RedHat(passenger)
                    passenger.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
                    EntityManagement.AddAgentWithBlipAndWeapon(passenger, WeaponHash.SMG)
                End If
            Next
            bus.MarkAsNoLongerNeeded()
            last_time = Now
        Else
            UI.Screen.ShowSubtitle("服务繁忙，请稍后再试")
        End If
    End Sub
    Private Sub AuthorInfo()
        Notification.PostTicker("chunchun" + vbNewLine + "modif for ", True)
    End Sub
    ''' <summary>
    ''' 保存服装代码文件
    ''' </summary>
    Private Sub ShowClothes()
        menu.Visible = False
        Dim builder As StringBuilder = New StringBuilder()
        For Each component As PedComponent In PlayerPed.Style.GetAllComponents()
            builder.AppendLine($"agent.Style.Item(PedComponentType.{component.Type}).SetVariation({component.Index},{component.TextureIndex})")
        Next
        For Each prop As PedProp In PlayerPed.Style.GetAllProps()
            builder.AppendLine($"agent.Style.Item(PedPropAnchorPoint.{prop.AnchorPoint}).SetVariation({prop.Index},{prop.TextureIndex})")
        Next
        builder.AppendLine(
        <ins>
'这里设置遗传信息，需要访问线上模式查看
'        · Males:

'ID 0 - Benjamin 

'ID 1 - Daniel

'ID 2 - Joshua

'ID 3 - Noah

'ID 4 - Andrew

'ID 5 - Joan

'ID 6 - Alex

'ID 7 - Isaac

'ID 8 - Evan

'ID 9 - Ethan

'ID 10 - Vincent

'ID 11 - Angel

'ID 12 - Diego

'ID 13 - Adrian

'ID 14 - Gabriel

'ID 15 - Michael

'ID 16 - Santiago

'ID 17 - Kevin

'ID 18 - Louis

'ID 19 - Samuel

'ID 20 - Anthony

'ID 42 - Claude

'ID 43 - Niko

'ID 44 - John

'· Females:

'ID 21 - Hannah

'ID 22 - Audrey

'ID 23 - Jasmine

'ID 24 - Giselle

'ID 25 - Amelia

'ID 26 - Isabella

'ID 2F - Zoe

'ID 28 - Ava

'ID 29 - Camilla

'ID 30 - Violet

'ID 31 - Sophia

'ID 32 - Eveline

'ID 33 - Nicole

'ID 34 - Ashley

'ID 35 - Grace

'ID 36 - Brianna

'ID 37 - Natalie

'ID 38 - Olivia

'ID 39 - Elizabeth

'ID 40 - Charlotte

'ID 41 - Emma

'ID 45 - Misty
        </ins>)
        builder.AppendLine($"{NameOf(ApplyEyeData)}(agent,{GetEyeData(PlayerPed)})")
        For i As Integer = 0 To 19
            builder.AppendLine($"{NameOf(ApplyFaceData)}(agent,{i},{GetFaceData(PlayerPed, i)})")
        Next

        Dim path As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\ped_style.txt"
        File.WriteAllText(path, builder.ToString())
        Dim blend As HeadBlendData = GetHeadBlendData(PlayerPed)
        Notification.PostTicker(builder.ToString(), True)
        UI.Screen.ShowHelpText($"已将服装代码保存至{path}")
    End Sub
    Private Sub SaveLocation()
        Static var_name As String = "location_list"
        var_name = Game.GetUserInput(var_name)
        Dim l As Vector3 = PlayerPed.Position
        FileIO.FileSystem.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\game_locations.txt",
                                        $"{var_name}.Add(New Vector3({l.X},{l.Y},{l.Z})){vbNewLine}", True)
    End Sub
    ''' <summary>
    ''' 载具菜单。
    ''' </summary>
    Private Class VehicleMenuDialog
        Inherits MenuDialog(Of VehicleMenuDialog)
        Private last_call_hydra As Date?
        Public Sub New()
            MyBase.New("载具选项")
            AddMenuItem("黑色防弹SUV", "屌王3600X", AddressOf GenerateBlackSUV, True)
            AddMenuItem("警车", "无标识巡航者", AddressOf GenerateCar, True)
            AddMenuItem("装甲车", "皮卡", AddressOf GenerateInsu)
            AddMenuItem("夜鲨", "", AddressOf GenerateNightShark)
            AddMenuItem("灭世暴徒2000", String.Empty, AddressOf GenerateRuiner2, True)
            AddMenuItem("改装当前载具", String.Empty, AddressOf MotifyPlayerCurrentVehicle)
            AddMenuItem("送回载具", "送回个人载具，不包括以上选项创建的载具", AddressOf VehicleTypeMenu.SendBackPersonalVehicle)
            AddSep("--- 全部载具 ---")
            Dim dict As Dictionary(Of VehicleClass, VehicleTypeMenu) = New Dictionary(Of VehicleClass, VehicleTypeMenu)()
            For Each model As Model In Vehicle.GetAllModels()
                Dim cls As VehicleClass = Vehicle.GetModelClass(model)
                If Not dict.ContainsKey(cls) Then
                    dict.Add(cls, New VehicleTypeMenu(cls))
                End If
                Dim menu As VehicleTypeMenu = dict.Item(cls)
                menu.AddVehicleModel(model)
            Next
            For Each menu As VehicleTypeMenu In dict.Values
                AddMenuItem(menu.VehicleClassName, String.Empty, AddressOf menu.ShowDialog)
            Next
        End Sub
        Private Sub MotifyPlayerCurrentVehicle()
            Dim v As Vehicle = PlayerPed.CurrentVehicle
            If v IsNot Nothing AndAlso v.IsAlive Then
                If v.IsInAir Then
                    UI.Screen.ShowHelpText("飞行载具必须降落才能改装")
                ElseIf v.Speed > 0 Then
                    UI.Screen.ShowHelpText("只能改装停放的载具")
                Else
                    v.Mods.InstallModKit()
                    Dim menu As VehicleMotifyMenu = New VehicleMotifyMenu(v)
                    menu.ShowDialog()
                End If
            Else
                UI.Screen.ShowHelpText("玩家似乎没有乘坐任何有效载具")
            End If
        End Sub
        Private Sub GenerateRuiner2()
            Static last_call As Date? = Nothing
            If Not last_call.HasValue OrElse Now.Subtract(last_call.Value).TotalMinutes > 10 Then
                last_call = Now
                Dim v As Vehicle = CreateVehicle(VehicleHash.Ruiner2, 20)
                v.CanTiresBurst = False
                EntityManagement.AddPersonalVehicle(v, BlipSprite.Ruiner2000)
            Else
                UI.Screen.ShowSubtitle("~r~操作频繁")
            End If

        End Sub
        Private Sub GenerateNightShark()
            Dim v As Vehicle = CreateVehicle(VehicleHash.NightShark, 50)
            v.Mods.LicensePlate = "DERRA"
            v.Mods.PrimaryColor = VehicleColor.MatteBlack
            v.Mods.SecondaryColor = VehicleColor.MatteBlack
            v.Mods.WindowTint = VehicleWindowTint.DarkSmoke
            v.CanTiresBurst = False
            EntityManagement.AddPersonalVehicle(v, name:="夜鲨")
        End Sub
        ''' <summary>
        ''' 生成一辆黑色的SUV。
        ''' </summary>
        Private Sub GenerateBlackSUV()
            Dim suv As Vehicle = World.CreateVehicle(VehicleHash.Granger2, GetNextPositionOnStreet())
            suv.PlaceOnNextStreet()
            suv.Mods.LicensePlate = "DERRA"
            suv.Mods.LicensePlateStyle = LicensePlateStyle.YellowOnBlack
            suv.Mods.PrimaryColor = VehicleColor.MatteBlack
            suv.Mods.SecondaryColor = VehicleColor.MatteBlack
            suv.Mods.PearlescentColor = VehicleColor.MetallicBlack
            suv.Mods.RimColor = VehicleColor.MatteBlack
            suv.Mods.WindowTint = VehicleWindowTint.DarkSmoke
            For Each light As VehicleNeonLight In [Enum].GetValues(GetType(VehicleNeonLight))
                suv.Mods.SetNeonLightsOn(light, True)
            Next
            suv.Mods.NeonLightsColor = Color.Green
            suv.CanTiresBurst = False
            EntityManagement.AddPersonalVehicle(suv, BlipSprite.Granger3600LX)
        End Sub
        Private Sub GenerateCar()
            Dim suv As Vehicle = World.CreateVehicle(VehicleHash.Police4, GetNextPositionOnStreet())
            suv.PlaceOnNextStreet()
            suv.Mods.LicensePlate = "DERRA"
            suv.Mods.LicensePlateStyle = LicensePlateStyle.YellowOnBlack
            suv.Mods.PrimaryColor = VehicleColor.MatteBlack
            suv.Mods.SecondaryColor = VehicleColor.MatteBlack
            suv.Mods.PearlescentColor = VehicleColor.MetallicBlack
            suv.Mods.RimColor = VehicleColor.MatteBlack
            suv.Mods.WindowTint = VehicleWindowTint.DarkSmoke
            For Each light As VehicleNeonLight In [Enum].GetValues(GetType(VehicleNeonLight))
                suv.Mods.SetNeonLightsOn(light, True)
            Next
            suv.Mods.NeonLightsColor = Color.Green

            suv.CanTiresBurst = False
            EntityManagement.AddPersonalVehicle(suv, BlipSprite.PolicePlayer, name:=suv.LocalizedName)
        End Sub
        Private Sub GenerateInsu()
            Dim v As Vehicle = CreateVehicle(VehicleHash.Insurgent, 50)
            v.Mods.LicensePlate = "DERRA"
            v.Mods.PrimaryColor = VehicleColor.MatteBlack
            v.Mods.SecondaryColor = VehicleColor.MatteBlack
            v.Mods.WindowTint = VehicleWindowTint.DarkSmoke
            v.CanTiresBurst = False
            EntityManagement.AddPersonalVehicle(v, BlipSprite.GunCar)
        End Sub
        Private Class VehicleTypeMenu
            Inherits MenuDialog
            Private Shared usingVehicle As Vehicle
            Public Sub New(type As VehicleClass)
                MyBase.New(Game.GetLocalizedString(Vehicle.GetClassDisplayName(type)))
            End Sub
            Public ReadOnly Property VehicleClassName As String
                Get
                    Return menu.BannerText.Text
                End Get
            End Property
            Public Sub AddVehicleModel(hash As Model)
                AddMenuItem(Game.GetLocalizedString(Vehicle.GetModelMakeName(hash)) + " " + Game.GetLocalizedString(Vehicle.GetModelDisplayName(hash)), "", Sub() GenerateVehicle(hash), True)
            End Sub
            Public Shared Sub SendBackPersonalVehicle()
                If usingVehicle IsNot Nothing AndAlso usingVehicle.IsAlive Then
                    If False Then
                        UI.Screen.ShowSubtitle("无法送回")
                    ElseIf PlayerPed.IsInVehicle(usingVehicle) Then
                        UI.Screen.ShowSubtitle("您正在驾驶个人载具，无法送回")
                    ElseIf usingVehicle.PassengerCount > 0 OrElse usingVehicle.Driver?.IsAlive Then
                        UI.Screen.ShowSubtitle("个人载具内存在乘客，无法送回")
                    Else
                        usingVehicle.Delete()
                    End If
                End If
            End Sub
            Private Sub GenerateVehicle(hash As VehicleHash)
                If usingVehicle IsNot Nothing AndAlso usingVehicle.IsAlive Then
                    If usingVehicle.Position.DistanceTo(PlayerPed.Position) < 70 Then
                        UI.Screen.ShowSubtitle("您的个人载具已在附近")
                        Return
                    ElseIf PlayerPed.IsInVehicle(usingVehicle) Then
                        UI.Screen.ShowSubtitle("您正在驾驶个人载具，无法送回")
                        Return
                    ElseIf usingVehicle.PassengerCount > 0 OrElse usingVehicle.Driver?.IsAlive Then
                        UI.Screen.ShowSubtitle("个人载具内存在乘客，无法送回")
                        Return
                    Else
                        usingVehicle.Delete()
                    End If
                End If
                Dim model As Model = hash
                If model.IsPlane OrElse model.IsHelicopter Then
                    '查找最近的机场
                    Dim player_location As Vector3 = PlayerPed.Position
                    Dim nearby_airport As Minimize(Of Vector3) = New Minimize(Of Vector3)()
                    For Each airport As Vector3 In Map.AirportLocations
                        nearby_airport.TryNext(airport.DistanceTo(player_location), airport)
                    Next
                    Dim fly As Vehicle = World.CreateVehicle(model, World.GetNextPositionOnStreet(nearby_airport.Selected, True))
                    If fly IsNot Nothing Then
                        With fly.AddBlip
                            .Sprite = If(model.IsPlane, BlipSprite.Plane, BlipSprite.Helicopter)
                            .Name = fly.LocalizedName
                        End With
                        EntityManagement.AddController(New PersonalVehicleController2(fly))
                        usingVehicle = fly
                    End If
                Else
                    Dim vehicle As Vehicle = CreateVehicle(hash, 5)
                    If vehicle IsNot Nothing Then
                        vehicle.LockStatus = VehicleLockStatus.Unlocked
                        With vehicle.AddBlip
                            .Sprite = BlipSprite.PersonalVehicleCar
                            .Name = vehicle.LocalizedName
                        End With
                        'PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver)
                        EntityManagement.AddController(New PersonalVehicleController2(vehicle))
                        usingVehicle = vehicle
                    Else
                        UI.Screen.ShowSubtitle("创建载具失败")
                    End If
                End If

            End Sub
        End Class
        Private Class PersonalVehicleController2
            Implements IEntityController
            Private ReadOnly vehicle As Vehicle
            Public Sub New(vehicle As Vehicle)
                Me.vehicle = vehicle
            End Sub

            Public ReadOnly Property Target As Entity Implements IEntityController.Target
                Get
                    Return vehicle
                End Get
            End Property

            Public Sub OnTick() Implements IEntityController.OnTick
                Dim blip As Blip = vehicle.AttachedBlip
                If blip?.Exists Then
                    If PlayerPed.IsInVehicle(vehicle) Then
                        blip.DisplayType = BlipDisplayType.NoDisplay
                    Else
                        blip.DisplayType = BlipDisplayType.BothMapSelectable
                    End If
                End If
            End Sub

            Public Sub Disposing() Implements IEntityController.Disposing

            End Sub

            Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
                Return vehicle.IsDead
            End Function
        End Class
    End Class
    ''' <summary>
    ''' AI演员选项。
    ''' </summary>
    Private Class BotActorsDialog
        Inherits MenuDialog(Of BotActorsDialog)
        Public Sub New()
            MyBase.New("保镖菜单")
            AddCheckbox("保镖日志输出", "是否输出AI日志", PedHelper.LogEvents)
            AddCheckbox("敌我识别系统", "取消勾选后AI无法主动识别敌人", ThreatDetector.Detection)
            'AddCheckbox("AI托管", "AI托管玩家行动", Agent.Toggle)
            'AddCheckbox("AI托管默认逃脱", "AI托管玩家行动时是否默认逃脱", Agent.Flee)
            'AddMenuItem("派出终结者保镖", "派出一个AI保镖", AddressOf Actors.cal.CreateFemaleBodyGuard, True)
            AddMenuItem("空降士兵保镖 花费$2500", "空降一个士兵AI保镖", AddressOf Actors.cal.CreateRedHatBodyGuard, True)
            AddMenuItem("~b~复制玩家模型作为模板", "用于‘从模板创建保镖’选项", AddressOf Actors.cal.MarkCloneBodyguard)
            AddMenuItem("从模板创建保镖", "", AddressOf Actors.cal.CreateCloneBodyguard)
            AddMenuItem("移除所有保镖", "移除所有保镖", AddressOf RemoveAllBodyGuards)
            'AddMenuItem("~r~将模板设为玩家皮肤", "容易导致任务异常", AddressOf Actors.cal.ChangePlayerToClone)
            'AddMenuItem("~g~恢复玩家模型", "hui", AddressOf Actors.cal.RecoverOriginalModel)
            'AddMenuItem("将模板设为玩家皮肤并由AI托管", "该功能理论上不影响游戏稳定性", Sub()
            '                                                     Agent.StartProxy(Actors.cal.Model)
            '                                                 End Sub)
            'AddMenuItem("创建演员模仿玩家动作", "", AddressOf Actors.cal.CreateSyncActorFromModel)
        End Sub
        Private Sub RemoveAllBodyGuards()
            For Each controller As IEntityController In EntityManagement.Controllers
                TryCast(controller, PrivateSecurityController)?.BeginDispose()
            Next
            UI.Screen.ShowSubtitle("所有保镖已移除")
        End Sub
    End Class
    Private Class BotMenu
        Inherits MenuDialog(Of BotMenu)
        Public Sub New()
            MyBase.New("Bots")
            AddCheckbox("自动补充人机玩家", "当存活人机玩家数量过低时自动补充新的人机玩家", BotPlayerOptions.AutoGenerateBots)
            AddCheckbox("使用线上玩家模型", "启用后自动补充的人机玩家将使用线上玩家模型，不影响已经创建的人机玩家。", BotPlayerOptions.UseOnlineCharacterModel)
            AddCheckbox("允许重生", "允许死亡的人机玩家重生", BotPlayerOptions.CanBotRegenerate)
            'AddCheckbox("AI难度自适应", "当玩家击杀任意AI后，全体AI免费获得随机武器；若玩家被AI击杀，全体AI移除随机武器", BotPlayerOptions.AdaptiveBot)
            AddMenuItem("升级路人(U)", "将路人转化为人机玩家（快捷键U）", AddressOf UpgradeNearbyPed)
            AddMenuItem("向最近的人机玩家派出刺杀小队", "", AddressOf DispatchOps, True)
            AddMenuItem("玩家列表", "查看AI玩家的详细信息与结盟选项", AddressOf ShowBotList, False)
            AddMenuItem("与盟友乘坐同一个载具(N)", "快捷键N", AddressOf AutoEnterVehicle)
            AddMenuItem("设置当前角色名称", "不管你的角色是导演模式还是故事模式角色，还是用修改器刷出来的", AddressOf ChangePlayerName)
            AddMenuItem("设置当前角色名称为 ~h~Social Club~h~ 用户名称", "将玩家名称设置为R星用户名", AddressOf SetSocialClubName)
            'AddMenuItem("Debug男性", "发布时注释掉本行", Sub() Debug(PedHash.FreemodeMale01, TorsoCombonations.Male.LongClothes))

            'AddMenuItem("Debug女性", "发布时注释掉本行", Sub() Debug(PedHash.FreemodeFemale01, TorsoCombonations.Female.LongClothes))
        End Sub
        Private Sub ChangePlayerName()
            PlayerName.DisplayName = Game.GetUserInput(WindowTitle.CustomTeamName, PlayerName.DisplayName, 20)
        End Sub
        Private Sub SetSocialClubName()
            PlayerName.DisplayName = Game.Player.Name
        End Sub
        Private Sub Debug(model As PedHash, combo As TorsoCombonations)
            Dim storyModeModel As Model = PlayerPed.Model
            Game.Player.ChangeModel(model)
            For Each torso2 As Integer In combo.ClothIndeces
                'PlayerPed.Style.Item(PedComponentType.Torso).SetVariation(torso)
                PlayerPed.Style.Item(PedComponentType.Torso2).SetVariation(torso2)
                UI.Screen.ShowSubtitle($"手臂:default,上衣{torso2}")
                Wait(1000)
            Next
            Game.Player.ChangeModel(storyModeModel)
        End Sub
        Private Sub RequestAllienceToAllBots()
            BotFactory.Pool.Update()
            For Each bot As Bot In BotFactory.Pool
                RequestAllience(bot, False)
                Wait(500)
            Next
        End Sub
        Public Shared Sub AutoEnterVehicle()
            If PlayerPed.IsInVehicle() Then
                BotMenu.RequestBotEnterPlayerVehicle()
            Else
                BotMenu.RequestEnterBotVehicle()
            End If
        End Sub
        Public Shared Sub RequestEnterBotVehicle()
            Dim friend_bot As Minimize(Of Bot) = New Minimize(Of Bot)()
            For Each controller As IEntityController In EntityManagement.Controllers
                If TypeOf controller Is Bot Then
                    Dim bot As Bot = controller
                    If controller.Target.IsAlive AndAlso Not CType(controller.Target, Ped).IsInFlyingVehicle AndAlso
                        controller.Target.Position.DistanceTo(PlayerPed.Position) < 30 AndAlso bot.Ped.IsInVehicle() AndAlso
                        CType(controller, Bot).IsAlly Then
                        friend_bot.TryNext(controller.Target.Position.DistanceTo(PlayerPed.Position), controller)
                    End If
                End If
            Next
            If friend_bot.HasSelectAny Then
                friend_bot.Selected.ForceStartNewDecision(New WaitForPlayerEnterVehicleDecision())
                PlayerPed.Task.EnterVehicle(friend_bot.Selected.Ped.CurrentVehicle, VehicleSeat.Passenger, speed:=2, timeout:=10000, flag:=EnterVehicleFlags.DontJackAnyone)
            Else
                UI.Screen.ShowSubtitle("附近没有其他乘坐载具的盟友")
            End If
        End Sub
        Public Shared Sub RequestBotEnterPlayerVehicle()
            Dim player_vehicle As Vehicle = PlayerPed.CurrentVehicle
            If player_vehicle Is Nothing Then
                Return
            End If
            For Each controller As IEntityController In EntityManagement.Controllers
                If TypeOf controller Is Bot Then
                    If controller.Target.IsAlive AndAlso Not CType(controller.Target, Ped).IsInFlyingVehicle AndAlso
                        controller.Target.Position.DistanceTo(PlayerPed.Position) < 30 AndAlso PlayerPed.IsInVehicle() AndAlso
                        CType(controller, Bot).IsAlly Then
                        Dim bot As Bot = controller
                        If bot.Ped.IsInVehicle(player_vehicle) Then
                            Continue For
                        End If
                        bot.ForceStartNewDecision(New EnterPlayerVehicleDecision())
                    End If
                End If
            Next
        End Sub
        ''' <summary>
        ''' 召集所有盟友。
        ''' </summary>
        Private Sub GatherAllAllies()
            Dim count As Integer = 0
            For Each bot As Bot In BotFactory.Pool
                If bot.IsAlly Then
                    RequestMeeting(bot)
                    count += 1
                    Wait(500)
                End If
            Next
            If count = 0 Then
                UI.Screen.ShowHelpText("当前战局中没有存活的盟友")
            Else
                'UI.Screen.ShowHelpText("已召集 " + CStr(count) + " 名盟友")
            End If
        End Sub
        Private Sub GatherAllBots()
            Dim count As Integer = 0
            For Each bot As Bot In BotFactory.Pool
                If bot.Ped.IsAlive Then
                    RequestMeeting(bot)
                End If
            Next
        End Sub
        Private Sub ShowBotList()
            Dim dialog As MenuDialog = New MenuDialog("玩家")
            BotFactory.Pool.Update()
            dialog.AddMenuItem("随机创建AI玩家", "随机创建一个具有线上模式玩家模型的AI玩家", AddressOf BotFactory.CreateRandomOnlinePlayer)
            dialog.AddMenuItem("随机创建路人AI玩家", "随机创建一个路人模型的AI玩家", AddressOf BotFactory.CreateBot)
            dialog.AddMenuItem("批量发送~o~结盟请求", "", AddressOf RequestAllienceToAllBots, True)
            dialog.AddMenuItem("召集战局中所有~b~盟友", "某些玩家可能由于正在执行其他更重要的事情而忽略你的请求", AddressOf GatherAllAllies, True)
            dialog.AddMenuItem("召集战局中所有玩家", "向战局中所有玩家发出召见请求", AddressOf GatherAllBots)
            dialog.AddSep("--- 玩家列表 ---")
            For Each bot As Bot In BotFactory.Pool
                If bot.Ped.IsAlive Then
                    Dim color As String = String.Empty
                    If bot.IsAlly Then
                        color = "~b~"
                    ElseIf bot.WantedAmount.HasValue Then
                        color = " ~r~"
                    End If
                    dialog.AddMenuItem(color + "[电脑] " + bot.Name, If(bot.IsAlly, $"您与 {bot.Name} 处于结盟状态", ""), Sub()
                                                                                                                    If bot.IsAlly Then
                                                                                                                        ShowAllyDialog(bot)
                                                                                                                    Else
                                                                                                                        ShowRequestAllyDialog(bot)
                                                                                                                    End If
                                                                                                                End Sub, False)
                End If
            Next
            dialog.AddSep($"--- 战局中共 {BotFactory.Pool.Count} 名其他玩家 ---")


            dialog.ShowDialog()
        End Sub
        Public Sub RequestMeeting(bot As Bot)
            If bot.Ped.IsInVehicle() Then
                'Notification.PostTicker($"已向 ~h~{bot.Name}~h~ 发送召见请求", True)
                bot.ForceStartNewDecision(New VisitPlayerDecision())
                Wait(Pick({800, 1000, 2000}))
            Else
                'Notification.PostTicker($"~h~{bot.Name}~h~ 已拒绝召见请求", True)
            End If

        End Sub
        Private Sub ShowAllyDialog(bot As Bot)
            If Not bot.Ped.IsAlive Then
                UI.Screen.ShowHelpText($"{bot} 已死亡，无法获取详细信息")
                Return
            End If
            Dim dialog As MenuDialog = New MenuDialog(bot.Name)
            dialog.AddMenuItem("召见 " + bot.Name, "让盟友前往您的当前位置(它可能由于执行其他更重要的事情忽略你的请求)", Sub() RequestMeeting(bot))
            AddBotInfo(dialog, bot)
            'dialog.AddMenuItem("~r~宣战",$"将与{bot.Name}的关系设置为宣战状态",Sub() bot.IsAlly=False)
            dialog.ShowDialog()
        End Sub
        Private Sub ShowRequestAllyDialog(bot As Bot)
            If Not bot.Ped.IsAlive Then
                UI.Screen.ShowHelpText($"{bot} 已死亡，无法获取详细信息")
                Return
            End If
            Dim dialog As MenuDialog = New MenuDialog("玩家选项")
            dialog.AddMenuItem("发送结盟请求", "注意，若目标玩家处于交战状态则将拒绝你的结盟请求", Sub() RequestAllience(bot), True)
            'dialog.AddMenuItem("派出刺杀小队", "", Sub() DispatchOps(bot.Ped, AddressOf RedHat, VehicleHash.Granger))
            AddBotInfo(dialog, bot)
            dialog.ShowDialog()
        End Sub
        Private Sub AddBotInfo(menu As MenuDialog, bot As Bot)
            menu.AddSep("名称: " + bot.Name)
            menu.AddSep("当前任务:" + bot.CurrentActionName)
            menu.AddSep("游戏币: ~y~$" + CStr(bot.Money))
            menu.AddSep($"位置: {World.GetStreetName(bot.Ped.Position)}, {World.GetZoneLocalizedName(bot.Ped.Position)}")
            menu.AddSep($"距离: {World.CalculateTravelDistance(PlayerPed.Position, bot.Ped.Position):F0}m")
            menu.AddSep("拥有的载具数量: " + CStr(bot.OwnedVehicles.Count))
            menu.AddSep("拥有的武器数量: " + CStr(bot.OwnedWeapons.Count))
            menu.AddSep($"被玩家杀死次数：{bot.KilledByPlayer}/{bot.MaxBeKilledTimes}")
            If bot.WantedAmount.HasValue Then
                menu.AddSep("悬赏:$ " + CStr(bot.WantedAmount))
            End If
            Dim currentVehicle As Vehicle = bot.Ped.CurrentVehicle
            If currentVehicle IsNot Nothing Then
                menu.AddSep("乘坐载具: ~b~" + currentVehicle.ClassLocalizedName + " ~w~" + currentVehicle.LocalizedName)
            Else
                menu.AddSep("步行")
            End If
        End Sub
        Private Sub RequestAllience(bot As Bot, Optional show_fail_message As Boolean = True)
            If bot.Ped.IsDead Then
                If show_fail_message Then Notification.PostTicker($"发送请求失败: ~h~{bot.Name}~h~ 已死亡或退出游戏", True)
            ElseIf bot.Ped.IsInFlyingVehicle OrElse bot.Ped.IsInCombat Then
                If show_fail_message Then Notification.PostTicker("~h~" + bot.Name + "~h~ 拒绝了您的结盟请求(由于对方处于交战状态)", True)
            ElseIf bot.WantedAmount.HasValue Then
                If show_fail_message Then Notification.PostTicker("~h~" + bot.Name + "~h~ 处于被悬赏的状态，无法结盟", True)
            ElseIf bot.IsAlly Then
                Return
            Else
                If System.Math.Abs(bot.GetHashCode()) Mod 14 > 5 Then
                    bot.IsAlly = True
                    Notification.PostTicker($"您 已与 ~h~{bot.Name}~h~ 结盟", True)
                Else
                    If show_fail_message Then Notification.PostTicker($"~h~{bot.Name}~h~ ~r~拒绝与您结盟", True)
                End If

            End If
        End Sub
        Private Sub DispatchOps()
            Static last_time As Date?
            If Not last_time.HasValue OrElse Now - last_time > TimeSpan.FromMinutes(2) Then
                last_time = Now
                Dim enermy As Minimize(Of Ped) = New Minimize(Of Ped)()
                For Each controller As IEntityController In EntityManagement.Controllers
                    If TypeOf controller Is Bot Then
                        If controller.Target.IsAlive AndAlso Not CType(controller.Target, Ped).IsInFlyingVehicle Then
                            enermy.TryNext(controller.Target.Position.DistanceTo(PlayerPed.Position), controller.Target)
                        End If
                    End If
                Next
                If enermy.HasSelectAny Then
                    For i As Integer = 1 To 3
                        DispatchOps(enermy.Selected, AddressOf RedHat, Pick({VehicleHash.Baller5, VehicleHash.Granger, VehicleHash.Granger2}))
                    Next
                    UI.Screen.ShowSubtitle($"已向 ~r~{enermy.Selected.AttachedBlip?.Name}~w~ 派出刺杀小队")
                Else
                    UI.Screen.ShowSubtitle("附近没有人机玩家")
                End If
            Else
                UI.Screen.ShowSubtitle("操作频繁，请稍后再试")
            End If
        End Sub
        Private Sub DispatchOps(target As Ped, style As Action(Of Ped), vehicleModel As VehicleHash?)
            Dim vehicle As Vehicle = CreateVehicle(vehicleModel, 100)
            vehicle.Mods.PrimaryColor = VehicleColor.MatteBlack
            vehicle.Mods.SecondaryColor = VehicleColor.MatteBlack
            vehicle.Mods.WindowTint = VehicleWindowTint.PureBlack
            vehicle.Mods.NeonLightsColor = Color.Green
            For Each light As VehicleNeonLight In [Enum].GetValues(GetType(VehicleNeonLight))
                vehicle.Mods.SetNeonLightsOn(light, True)
            Next
            For Each seat As VehicleSeat In {VehicleSeat.Driver, VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear}
                If vehicle.IsSeatFree(seat) Then
                    Dim op As Ped = vehicle.CreatePedOnSeat(seat, PedHash.FreemodeMale01)
                    style(op)
                    op.SetFleeAttributes(FleeAttributes.UseVehicle, True)
                    op.SetFleeAttributes(FleeAttributes.UseCover, True)
                    op.SetFleeAttributes(FleeAttributes.WanderAtEnd, True)
                    op.SetFleeAttributes(FleeAttributes.CanScream, False)
                    op.SetCombatAttribute(CombatAttributes.DisableAllRandomsFlee, True)
                    op.SetCombatAttribute(CombatAttributes.DisableFleeFromCombat, True)
                    op.SetCombatAttribute(CombatAttributes.MoveToLocationBeforeCoverSearch, True)
                    op.SetCombatAttribute(CombatAttributes.RequiresLosToAim, False)
                    op.SetCombatAttribute(CombatAttributes.UseMaxSenseRangeWhenReceivingEvents, True)
                    op.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, True)
                    op.RelationshipGroup = PrivateSecurityController.SecurityRelationshipGroup
                    'op.Weapons.Give(weapon, 1000, True, True).Components.GetFlashLightComponent().Active = True
                    op.CombatAbility = CombatAbility.Professional
                    'op.DecisionMaker = DecisionMakerTypeHash.Swat
                    op.KeepTaskWhenMarkedAsNoLongerNeeded = True
                    op.Task.Combat(target)
                    With op.AddBlip()
                        .IsFriendly = True
                        .Scale = 0.7
                        .Name = "刺杀小队"
                    End With
                    Dim controller As OperativeController = New OperativeController(op, target)
                    EntityManagement.AddController(controller)
                End If
            Next
            vehicle.MarkAsNoLongerNeeded()
        End Sub
    End Class
    Private Class PlayerMenu
        Inherits MenuDialog(Of PlayerMenu)
        Public Sub New()
            MyBase.New("玩家")
            AddSep("线上模式模拟器")
            AddCheckbox("启用线上模式角色技能", "如吃零食，复活", OnlinePlayerOption.EnableMPMode)
            AddCheckbox("削弱通缉", "启用后玩家更容易摆脱LSPD通缉", RealCop.Enable)
            AddCheckbox("复仇", "任何NPC对玩家造成伤害都将在地图上显示", RevengeScript.Toggle)
            'AddCheckbox("丧尸感染", "启动丧尸感染", ZombieScript.Infection)
            SetSelectedIndex(1)
        End Sub
    End Class

    Private Class VehicleMotifyMenu
        Inherits MenuDialog
        Public Sub New(veh As Vehicle)
            MyBase.New(veh.LocalizedName)
            Dim model As Model = veh.Model
            For Each mod_type As VehicleModType In [Enum].GetValues(GetType(VehicleModType))
                If mod_type <> VehicleModType.None Then
                    Dim m As VehicleMod = veh.Mods.Item(mod_type)
                    If m.Count > 0 Then
                        Dim menu As VehicleModMotifyMenu = New VehicleModMotifyMenu(m)
                        AddMenuItem(menu.Title, "", AddressOf menu.ShowDialog)
                    End If
                End If
            Next
        End Sub
        Private Class VehicleModMotifyMenu
            Inherits OptionMenuDialog
            Private Shared ReadOnly _hornNames As ReadOnlyDictionary(Of Integer, Tuple(Of String, String)) = New ReadOnlyDictionary(Of Integer, Tuple(Of String, String))(New Dictionary(Of Integer, Tuple(Of String, String)) From {
        {-1, New Tuple(Of String, String)("CMOD_HRN_0", "Stock Horn")},
        {0, New Tuple(Of String, String)("CMOD_HRN_TRK", "Truck Horn")},
        {1, New Tuple(Of String, String)("CMOD_HRN_COP", "Cop Horn")},
        {2, New Tuple(Of String, String)("CMOD_HRN_CLO", "Clown Horn")},
        {3, New Tuple(Of String, String)("CMOD_HRN_MUS1", "Musical Horn 1")},
        {4, New Tuple(Of String, String)("CMOD_HRN_MUS2", "Musical Horn 2")},
        {5, New Tuple(Of String, String)("CMOD_HRN_MUS3", "Musical Horn 3")},
        {6, New Tuple(Of String, String)("CMOD_HRN_MUS4", "Musical Horn 4")},
        {7, New Tuple(Of String, String)("CMOD_HRN_MUS5", "Musical Horn 5")},
        {8, New Tuple(Of String, String)("CMOD_HRN_SAD", "Sad Trombone")},
        {9, New Tuple(Of String, String)("HORN_CLAS1", "Classical Horn 1")},
        {10, New Tuple(Of String, String)("HORN_CLAS2", "Classical Horn 2")},
        {11, New Tuple(Of String, String)("HORN_CLAS3", "Classical Horn 3")},
        {12, New Tuple(Of String, String)("HORN_CLAS4", "Classical Horn 4")},
        {13, New Tuple(Of String, String)("HORN_CLAS5", "Classical Horn 5")},
        {14, New Tuple(Of String, String)("HORN_CLAS6", "Classical Horn 6")},
        {15, New Tuple(Of String, String)("HORN_CLAS7", "Classical Horn 7")},
        {16, New Tuple(Of String, String)("HORN_CNOTE_C0", "Scale Do")},
        {17, New Tuple(Of String, String)("HORN_CNOTE_D0", "Scale Re")},
        {18, New Tuple(Of String, String)("HORN_CNOTE_E0", "Scale Mi")},
        {19, New Tuple(Of String, String)("HORN_CNOTE_F0", "Scale Fa")},
        {20, New Tuple(Of String, String)("HORN_CNOTE_G0", "Scale Sol")},
        {21, New Tuple(Of String, String)("HORN_CNOTE_A0", "Scale La")},
        {22, New Tuple(Of String, String)("HORN_CNOTE_B0", "Scale Ti")},
        {23, New Tuple(Of String, String)("HORN_CNOTE_C1", "Scale Do (High)")},
        {24, New Tuple(Of String, String)("HORN_HIPS1", "Jazz Horn 1")},
        {25, New Tuple(Of String, String)("HORN_HIPS2", "Jazz Horn 2")},
        {26, New Tuple(Of String, String)("HORN_HIPS3", "Jazz Horn 3")},
        {27, New Tuple(Of String, String)("HORN_HIPS4", "Jazz Horn Loop")},
        {28, New Tuple(Of String, String)("HORN_INDI_1", "Star Spangled Banner 1")},
        {29, New Tuple(Of String, String)("HORN_INDI_2", "Star Spangled Banner 2")},
        {30, New Tuple(Of String, String)("HORN_INDI_3", "Star Spangled Banner 3")},
        {31, New Tuple(Of String, String)("HORN_INDI_4", "Star Spangled Banner 4")},
        {32, New Tuple(Of String, String)("HORN_LUXE2", "Classical Horn Loop 1")},
        {33, New Tuple(Of String, String)("HORN_LUXE1", "Classical Horn 8")},
        {34, New Tuple(Of String, String)("HORN_LUXE3", "Classical Horn Loop 2")},
        {35, New Tuple(Of String, String)("HORN_LUXE2", "Classical Horn Loop 1")},
        {36, New Tuple(Of String, String)("HORN_LUXE1", "Classical Horn 8")},
        {37, New Tuple(Of String, String)("HORN_LUXE3", "Classical Horn Loop 2")},
        {38, New Tuple(Of String, String)("HORN_HWEEN1", "Halloween Loop 1")},
        {39, New Tuple(Of String, String)("HORN_HWEEN1", "Halloween Loop 1")},
        {40, New Tuple(Of String, String)("HORN_HWEEN2", "Halloween Loop 2")},
        {41, New Tuple(Of String, String)("HORN_HWEEN2", "Halloween Loop 2")},
        {42, New Tuple(Of String, String)("HORN_LOWRDER1", "San Andreas Loop")},
        {43, New Tuple(Of String, String)("HORN_LOWRDER1", "San Andreas Loop")},
        {44, New Tuple(Of String, String)("HORN_LOWRDER2", "Liberty City Loop")},
        {45, New Tuple(Of String, String)("HORN_LOWRDER2", "Liberty City Loop")},
        {46, New Tuple(Of String, String)("HORN_XM15_1", "Festive Loop 1")},
        {47, New Tuple(Of String, String)("HORN_XM15_2", "Festive Loop 2")},
        {48, New Tuple(Of String, String)("HORN_XM15_3", "Festive Loop 3")}
    })

            Public Sub New(part As VehicleMod)
                MyBase.New(part.LocalizedTypeName)
                Dim count As Integer = part.Count
                Dim index As Integer = part.Index
                For i As Integer = 0 To count - 1
                    Dim i2 As Integer = i
                    AddOption(GetModName(part.Type, i, count, part.Vehicle), i = index, Sub() part.Index = i2)
                Next
            End Sub
            Public ReadOnly Property Title As String
                Get
                    Return menu.BannerText.Text
                End Get
            End Property
            Private Function GetModName(type As VehicleModType, index As Integer, count As Integer, v As Vehicle) As String
                If count = 0 Then
                    Return String.Empty
                End If

                If index < -1 OrElse index >= count Then
                    Return String.Empty
                End If

                If Not [Function].[Call](Of Boolean)(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, "mod_mnu", 10) Then
                    [Function].[Call](Hash.CLEAR_ADDITIONAL_TEXT, 10, True)
                    [Function].[Call](Hash.REQUEST_ADDITIONAL_TEXT, "mod_mnu", 10)
                End If

                If type = VehicleModType.Horns Then

                    If Not _hornNames.ContainsKey(index) Then
                        Return String.Empty
                    End If

                    If String.IsNullOrEmpty(Game.GetLocalizedString(_hornNames(index).Item1)) Then
                        Return _hornNames(index).Item2
                    End If

                    Return Game.GetLocalizedString(_hornNames(index).Item1)
                End If

                If type = VehicleModType.FrontWheel OrElse type = VehicleModType.RearWheel Then

                    If index = -1 Then

                        If Not v.Model.IsBike AndAlso v.Model.IsBicycle Then
                            Return Game.GetLocalizedString("CMOD_WHE_0")
                        End If

                        Return Game.GetLocalizedString("CMOD_WHE_B_0")
                    End If

                    If index >= count / 2 Then
                        Return Game.GetLocalizedString("CHROME") & " " + Game.GetLocalizedString([Function].[Call](Of String)(Hash.GET_MOD_TEXT_LABEL, v.Handle, CInt(type), index))
                    End If

                    Return Game.GetLocalizedString([Function].[Call](Of String)(Hash.GET_MOD_TEXT_LABEL, v.Handle, CInt(type), index))
                End If

                Select Case type
                    Case VehicleModType.Armor
                        Return Game.GetLocalizedString("CMOD_ARM_" & (index + 1))
                    Case VehicleModType.Brakes
                        Return Game.GetLocalizedString("CMOD_BRA_" & (index + 1))
                    Case VehicleModType.Engine

                        If index = -1 Then
                            Return Game.GetLocalizedString("CMOD_ARM_0")
                        End If

                        Return Game.GetLocalizedString("CMOD_ENG_" & (index + 2))
                    Case VehicleModType.Suspension
                        Return Game.GetLocalizedString("CMOD_SUS_" & (index + 1))
                    Case VehicleModType.Transmission
                        Return Game.GetLocalizedString("CMOD_GBX_" & (index + 1))
                    Case Else

                        If index > -1 Then
                            Dim entry As String = [Function].[Call](Of String)(Hash.GET_MOD_TEXT_LABEL, v.Handle, CInt(type), index)

                            If Not String.IsNullOrEmpty(Game.GetLocalizedString(entry)) Then
                                entry = Game.GetLocalizedString(entry)

                                If entry = "" OrElse entry = "NULL" Then
                                    Return Title & " " + (index + 1)
                                End If

                                Return entry
                            End If

                            Return Title & " " + CStr(index + 1)
                        End If

                        Select Case type
                            Case VehicleModType.AirFilter

                                If Not (v.Model = VehicleHash.Tornado) Then
                                End If

                            Case VehicleModType.Struts
                                Dim vehicleHash As VehicleHash = v.Model

                                If vehicleHash = VehicleHash.Banshee2 OrElse vehicleHash = VehicleHash.Banshee OrElse vehicleHash = VehicleHash.SultanRS Then
                                    Return Game.GetLocalizedString("CMOD_COL5_41")
                                End If

                                Exit Select
                        End Select

                        Return Game.GetLocalizedString("CMOD_DEF_0")
                End Select
            End Function
        End Class
    End Class
End Class

