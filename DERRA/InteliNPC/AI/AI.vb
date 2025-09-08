Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports DERRA.EntityLabelDisplay
Imports DERRA.InteliNPC.AI.Decisions
Imports DERRA.InteliNPC.Combat
Imports DERRA.Tasking
Imports GTA
Imports GTA.Graphics
Imports GTA.Math
Imports GTA.Native
Imports GTA.UI
Imports LemonUI.Elements
Imports LemonUI.Menus

Namespace InteliNPC.AI
    ''' <summary>
    ''' 表示AI的决策。
    ''' </summary>
    Public MustInherit Class BotDecision
        Private ReadOnly m_name As String
        ''' <summary>
        ''' 使用决策的名称初始化<see cref="BotDecision"/>类的新实例。
        ''' </summary>
        ''' <param name="name">决策的名称。</param>
        Public Sub New(name As String)
            m_name = name
        End Sub
        ''' <summary>
        ''' 使用类型名称作为决策的名称初始化<see cref="BotDecision"/>类的新实例。
        ''' </summary>
        Public Sub New()
            Me.New(Nothing)
        End Sub
        ''' <summary>
        ''' 获取当前决策的名称。可能会在马尔可夫矩阵中作为键名使用。
        ''' </summary>
        ''' <returns>决策的名称。</returns>
        Public ReadOnly Property Name As String
            Get
                Return If(m_name, TypeName(Me) + Hex(System.Math.Abs(GetHashCode())))
            End Get
        End Property
        ''' <summary>
        ''' 在派生类重写中，获取一个<see cref="Boolean"/>值，指示决策是否能被执行。
        ''' </summary>
        ''' <param name="bot">需要测试的<see cref="Bot"/>。</param>
        ''' <returns></returns>
        Public MustOverride Function IsAvaliableFor(bot As Bot) As Boolean
        ''' <summary>
        ''' 在派生类重写中，若决策引擎选择此决策，获取该决策对应的动作。
        ''' 无需修改<see cref="BotAction.Invoker"/>和<see cref="BotAction.DecisionName"/>，决策引擎将统一修改。
        ''' </summary>
        ''' <param name="bot">目标<see cref="Bot"/>。</param>
        ''' <returns>当前<see cref="BotDecision"/>对应的动作。</returns>
        Public MustOverride Function GetAction(bot As Bot) As BotAction
    End Class
    ''' <summary>
    ''' 表示<see cref="Bot"/>执行决策的动作。
    ''' </summary>
    Public MustInherit Class BotAction
        ''' <summary>
        ''' 获取或设置执行当前<see cref="BotAction"/>的<see cref="Bot"/>。
        ''' </summary>
        ''' <returns>执行当前<see cref="BotAction"/>的<see cref="Bot"/></returns>
        Public Property Invoker As Bot
        ''' <summary>
        ''' 获取或设置产生当前动作的决策名称。
        ''' </summary>
        ''' <returns>创建当前<see cref="BotAction"/>的<see cref="BotDecision.Name"/>。</returns>
        Public Property DecisionName As String
        ''' <summary>
        ''' 在派生类重写中，实现执行动作的程序。
        ''' </summary>
        Public MustOverride Sub Run()
        ''' <summary>
        ''' 在派生类重写中，获取一个<see cref="Boolean"/>值，指示当前动作是否执行完成，无论成功与否。
        ''' </summary>
        ''' <returns>当前动作是否执行完成。</returns>
        Public MustOverride Function IsCompleted() As Boolean
        ''' <summary>
        ''' 在派生类重写中，实现释放当前动作占用的资源。基类不执行任何操作。
        ''' </summary>
        Public Overridable Sub Dispose()
        End Sub
    End Class
    ''' <summary>
    ''' 表示决策矩阵。
    ''' </summary>
    Public Class DecisionMatrix
        Private Shared ReadOnly rng As Random = New Random()
        Private ReadOnly choices As List(Of Choice) = New List(Of Choice)()
        ''' <summary>
        ''' 使用已知的<see cref="BotDecision"/>集合随机初始化决策矩阵。
        ''' </summary>
        ''' <param name="known_decisions">已知的决策集合。</param>
        Public Sub New(known_decisions As IEnumerable(Of BotDecision))
            For Each x As BotDecision In known_decisions
                For Each y As BotDecision In known_decisions
                    If x.Name = y.Name Then
                        Continue For
                    End If
                    Dim choice As Choice = New Choice(x.Name, y.Name, rng.NextDouble())
                    choices.Add(choice)
                Next
            Next
        End Sub
        Public Sub Avoid(other As DecisionMatrix)
            For i As Integer = 0 To System.Math.Min(choices.Count - 1, other.choices.Count - 1)
                Dim my_choice As Choice = choices(i)
                Dim other_choice As Choice = other.choices(i)
                Dim new_weight As Single = Clamp(CSng(rng.NextDouble() * 2 * (my_choice.Weight - other_choice.Weight) + my_choice.Weight), 1.0F, 2.0F)
                choices(i) = New Choice(my_choice.CurrentDecisionName, my_choice.NextDecisionName, new_weight)
            Next
        End Sub
        Public Sub Learn(other As DecisionMatrix)
            For i As Integer = 0 To System.Math.Min(choices.Count - 1, other.choices.Count - 1)
                Dim my_choice As Choice = choices(i)
                Dim other_choice As Choice = other.choices(i)
                Dim new_weight As Single = Clamp(CSng(rng.NextDouble() * 2 * (my_choice.Weight - other_choice.Weight) + my_choice.Weight), 1.0F, 2.0F)
                choices(i) = New Choice(my_choice.CurrentDecisionName, my_choice.NextDecisionName, new_weight)
            Next
        End Sub
        Public Sub CopyFrom(other As DecisionMatrix)
            For i As Integer = 0 To System.Math.Min(choices.Count - 1, other.choices.Count - 1)
                Dim my_choice As Choice = choices(i)
                Dim other_choice As Choice = other.choices(i)
                Dim new_weight As Single = Clamp(CSng(rng.NextDouble() * 2 * (my_choice.Weight - other_choice.Weight) + my_choice.Weight), 1.0F, 2.0F)
                choices(i) = New Choice(my_choice.CurrentDecisionName, my_choice.NextDecisionName, new_weight)
            Next
        End Sub
        Public Function GetNextDecisions(current_name As String) As List(Of Choice)
            Dim list As List(Of Choice) = New List(Of Choice)()
            For Each choice As Choice In choices
                If choice.CurrentDecisionName = current_name Then
                    list.Add(choice)
                End If
            Next
            Return list
        End Function
        Public Shared Function Pick(choices As IList(Of Choice)) As String
            If IsNothing(choices) OrElse choices.Count = 0 Then
                Throw New ArgumentException("任务数组不能为空。")
            End If

            ' 过滤掉权重为0的任务并计算总权重
            Dim validTasks = choices.Where(Function(t) t.Weight > 0).ToArray()
            If validTasks.Length = 0 Then
                Throw New InvalidOperationException("所有有效任务的权重之和必须大于0。")
            End If

            Dim totalWeight As Single = validTasks.Sum(Function(t) t.Weight)
            Dim randomValue As Single = CSng(rng.NextDouble() * totalWeight)

            Dim currentSum As Single = 0
            For Each task In validTasks
                currentSum += task.Weight
                If currentSum >= randomValue Then
                    Return task.NextDecisionName
                End If
            Next
            Notification.PostTicker("错误", True)
            ' 理论上不会执行到这里（由于总权重>0）
            Return Nothing
        End Function
        Public Structure Choice
            Public ReadOnly CurrentDecisionName As String
            Public ReadOnly NextDecisionName As String
            Public ReadOnly Weight As Single

            Public Sub New(currentDecisionName As String, nextDecisionName As String, weight As Single)
                Me.CurrentDecisionName = currentDecisionName
                Me.NextDecisionName = nextDecisionName
                Me.Weight = weight
            End Sub
            Public Overrides Function ToString() As String
                Return $"{CurrentDecisionName} -> {NextDecisionName}({Weight:p2})"
            End Function
        End Structure
    End Class
    Public Class Bot
        Implements IEntityController, ITickProcessable
        Private Shared ReadOnly Rng As New Random()
        Private Shared ReadOnly BlipColorMap As New Dictionary(Of BlipColor, (Drawing As System.Drawing.Color, Prefix As String))
        Shared Sub New()
            BlipColorMap = New Dictionary(Of BlipColor, (Drawing As Drawing.Color, Prefix As String)) From {
                {BlipColor.WhiteNotPure, (Drawing.Color.WhiteSmoke, "~s~")},
                {BlipColor.NetPlayer1, (Drawing.Color.FromArgb(114, 204, 224), "~b~")},
                {BlipColor.NetPlayer2, (Drawing.Color.FromArgb(148, 224, 114), "~g~")},
                {BlipColor.NetPlayer3, (Drawing.Color.FromArgb(224, 114, 204), "~q~")},
                {BlipColor.NetPlayer4, (Drawing.Color.FromArgb(240, 180, 88), "~o~")},
                {BlipColor.NetPlayer5, (Drawing.Color.FromArgb(224, 148, 114), "~o~")},
                {BlipColor.NetPlayer6, (Drawing.Color.FromArgb(114, 224, 148), "~g~")},
                {BlipColor.NetPlayer7, (Drawing.Color.FromArgb(204, 114, 224), "~p~")},
                {BlipColor.NetPlayer8, (Drawing.Color.FromArgb(224, 204, 114), "~y~")},
                {BlipColor.NetPlayer9, (Drawing.Color.FromArgb(114, 148, 224), "~b~")},
                {BlipColor.NetPlayer10, (Drawing.Color.FromArgb(148, 114, 224), "~b~")},
                {BlipColor.NetPlayer11, (Drawing.Color.FromArgb(224, 114, 148), "~r~")},
                {BlipColor.NetPlayer12, (Drawing.Color.FromArgb(114, 224, 204), "~g~")},
                {BlipColor.NetPlayer13, (Drawing.Color.FromArgb(204, 224, 114), "~y~")},
                {BlipColor.NetPlayer14, (Drawing.Color.FromArgb(224, 114, 114), "~r~")},
                {BlipColor.NetPlayer15, (Drawing.Color.FromArgb(114, 224, 114), "~g~")},
                {BlipColor.NetPlayer16, (Drawing.Color.FromArgb(114, 180, 224), "~b~")},
                {BlipColor.NetPlayer17, (Drawing.Color.FromArgb(224, 114, 180), "~q~")},
                {BlipColor.NetPlayer18, (Drawing.Color.FromArgb(180, 224, 114), "~g~")},
                {BlipColor.NetPlayer19, (Drawing.Color.FromArgb(224, 180, 114), "~o~")},
                {BlipColor.NetPlayer20, (Drawing.Color.FromArgb(180, 114, 224), "~p~")},
                {BlipColor.NetPlayer21, (Drawing.Color.FromArgb(114, 224, 180), "~g~")},
                {BlipColor.NetPlayer22, (Drawing.Color.FromArgb(224, 114, 224), "~q~")},
                {BlipColor.NetPlayer23, (Drawing.Color.FromArgb(114, 204, 224), "~b~")},
                {BlipColor.NetPlayer24, (Drawing.Color.FromArgb(204, 114, 224), "~p~")},
                {BlipColor.NetPlayer25, (Drawing.Color.FromArgb(224, 204, 114), "~y~")}
            }
        End Sub
        Private ReadOnly m_ped As Ped
        Private m_money As Integer
        Private ReadOnly known_decisions As Dictionary(Of String, BotDecision)
        Public ReadOnly Logic As DecisionMatrix
        Private current_action As BotAction
        Private ReadOnly m_name As String
        Private m_positionChanged As Boolean
        Private last_location As Vector3
        Public ReadOnly OwnedWeapons As NpcWeaponCollection
        Public ReadOnly OwnedVehicles As VehicleCollection = New VehicleCollection()
        Private m_usingVehicle As Vehicle
        Private m_wantedAmount As Integer?
        Private m_isFriend As Boolean
        Private hasExitGame As Boolean
        Private m_accuracy As Integer
        Private ReadOnly nameLabel As PedLabel
        Private m_nameColor As System.Drawing.Color
        Private m_nameColorPrefix As String
        Private ReadOnly m_combatBehavior As OnlineCombatBehavior

        Public Function GetNameColor() As System.Drawing.Color
            ' Return color with 75% opacity (192/255)
            Return Color.FromArgb(192, m_nameColor)
        End Function

        Public ReadOnly Property ColoredName As String
            Get
                Return $"{m_nameColorPrefix}{Name}~s~"
            End Get
        End Property
        ''' <summary>
        ''' 获取或设置自定义<see cref="BlipDisplayType"/>，具有最高优先级。
        ''' </summary>
        ''' <returns>自定义<see cref="BlipDisplayType"/>。</returns>
        Public Property CustomBlipDisplayType As BlipDisplayType?
        ''' <summary>
        ''' 获取或设置最大死亡次数（仅被玩家杀死的次数）。超过该次数<see cref="Bot"/>直接破防退游。
        ''' </summary>
        ''' <returns>最大死亡次数.</returns>
        Public Property MaxBeKilledTimes As Integer = Pick(12, 2)
        Public Property KilledByPlayer As Integer = 0
        Public Shared ReadOnly FriendRelationshipGroup As Lazy(Of RelationshipGroup) = New Lazy(Of RelationshipGroup)(Function()
                                                                                                                          Dim r = World.AddRelationshipGroup("d-bot-friend")
                                                                                                                          r.SetRelationshipBetweenGroups(PlayerPed.RelationshipGroup, Relationship.Respect)
                                                                                                                          Return r
                                                                                                                      End Function)
        Public Sub New(ped As Ped, name As String, known_decisions As IEnumerable(Of BotDecision), default_action As BotAction)
            m_ped = ped
            m_name = name
            nameLabel = New PedLabel(ped, name) With {.Visible = False}
            PedLabelProcessor.BeginProcess(nameLabel)
            ped.Health = 300
            ped.MaxHealth = 300
            ped.SetRagdollBlockingFlags(RagdollBlockingFlags.BulletImpact)
            ped.SetRagdollBlockingFlags(RagdollBlockingFlags.PlayerImpact)
            ped.SetRagdollBlockingFlags(RagdollBlockingFlags.RubberBullet)
            ped.SetConfigFlag(PedConfigFlagToggles.DisableExplosionReactions, True)
            [Function].Call(Hash.STOP_PED_SPEAKING, ped, True)
            [Function].Call(Hash.DISABLE_PED_PAIN_AUDIO, ped, True)
            ped.CombatAbility = CombatAbility.Professional
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, False)
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, True)
            ped.SetCombatAttribute(CombatAttributes.CanUseDynamicStrafeDecisions, True)
            ped.SetCombatAttribute(CombatAttributes.AlwaysFight, True)
            ped.SetCombatAttribute(CombatAttributes.JustSeekCover, False)
            ped.SetCombatAttribute(CombatAttributes.RequiresLosToShoot, False)
            ped.SetCombatAttribute(CombatAttributes.RequiresLosToAim, False)
            ped.SetCombatAttribute(CombatAttributes.UseProximityFiringRate, False)
            ped.SetCombatAttribute(CombatAttributes.CanShootWithoutLos, True)

            ped.SetCombatAttribute(CombatAttributes.UseProximityAccuracy, False)
            ped.SetCombatAttribute(CombatAttributes.MaintainMinDistanceToTarget, False)
            ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, False)
            ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, True)
            ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, False)
            ped.SetCombatAttribute(CombatAttributes.SwitchToAdvanceIfCantFindCover, True) '找不到掩体 则推进
            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, False)
            ped.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, False) '仅对载具使用火箭筒

            ' 禁用寻找掩体行为，让AI更像线上玩家
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, False)
            ped.SetCombatAttribute(CombatAttributes.JustSeekCover, False)
            ped.BlockPermanentEvents = True ' 阻止AI自动寻找掩体

            ' 设置为激进的战斗风格
            ped.CombatMovement = CombatMovement.WillAdvance ' 使用可用的枚举值
            
            ' 使用可用的API设置射击频率
            ped.ShootRate = 1000 ' 使用ShootRate属性
            
            ' 设置精准度波动，模拟玩家的不稳定射击
            Accuracy = Pick({35, 45, 55, 65, 75})
            
            ' 设置战斗范围为中等距离，不会过远也不会太近
            ped.CombatRange = CombatRange.Medium
            
            ' 设置为全自动射击模式
            ped.FiringPattern = FiringPattern.FullAuto
            
            ' 增强对玩家的敌对性
            ' 默认设置为非友好，增加与玩家的敌对性
            ped.RelationshipGroup = MissionHelper.EnermyRelationshipGroup
            
            ' 增加听觉和视觉范围，使AI能更远距离发现玩家
            ped.SeeingRange = 150.0F
            ped.HearingRange = 100.0F
            
            ' 防止AI受到伤害时中断行为
            ped.PedConfigFlags.SetConfigFlag(PedConfigFlagToggles.DisableHurt, True)
            
            OwnedWeapons = New NpcWeaponCollection(ped)
            Me.known_decisions = New Dictionary(Of String, BotDecision)()
            For Each decision As BotDecision In known_decisions
                Me.known_decisions.Add(decision.Name, decision)
            Next
            Logic = New DecisionMatrix(Me.known_decisions.Values)
            current_action = default_action
            With ped.AddBlip()
                .ShowsHeadingIndicator = True
                Dim blipColors As List(Of BlipColor) = New List(Of BlipColor)()
                blipColors.Add(BlipColor.NetPlayer1)
                blipColors.Add(BlipColor.NetPlayer2)
                blipColors.Add(BlipColor.NetPlayer3)
                blipColors.Add(BlipColor.NetPlayer4)
                blipColors.Add(BlipColor.NetPlayer5)
                blipColors.Add(BlipColor.NetPlayer6)
                blipColors.Add(BlipColor.NetPlayer7)
                blipColors.Add(BlipColor.NetPlayer8)
                blipColors.Add(BlipColor.NetPlayer9)
                blipColors.Add(BlipColor.NetPlayer10)
                blipColors.Add(BlipColor.NetPlayer11)
                blipColors.Add(BlipColor.NetPlayer12)
                blipColors.Add(BlipColor.NetPlayer13)
                blipColors.Add(BlipColor.NetPlayer14)
                blipColors.Add(BlipColor.NetPlayer15)
                blipColors.Add(BlipColor.NetPlayer16)
                blipColors.Add(BlipColor.NetPlayer17)
                blipColors.Add(BlipColor.NetPlayer18)
                blipColors.Add(BlipColor.NetPlayer19)
                blipColors.Add(BlipColor.NetPlayer20)
                blipColors.Add(BlipColor.NetPlayer21)
                blipColors.Add(BlipColor.NetPlayer22)
                blipColors.Add(BlipColor.NetPlayer23)
                blipColors.Add(BlipColor.NetPlayer24)
                blipColors.Add(BlipColor.NetPlayer25)
                If Rng.Next(3) = 0 Then
                    .Color = Pick(blipColors)
                Else
                    .Color = BlipColor.WhiteNotPure
                End If
                .Name = name
                .CategoryType = BlipCategoryType.OtherPlayers

                Dim colorInfo = BlipColorMap(.Color)
                m_nameColor = colorInfo.Drawing
                m_nameColorPrefix = colorInfo.Prefix
            End With
            UpdateBlipDisplayStyle()
            [Function].Call(Hash.SHOW_HEIGHT_ON_BLIP, ped.AttachedBlip, False)

            ' 初始化在线战斗行为
            m_combatBehavior = New OnlineCombatBehavior(Me)

            '[Function].Call(Hash.SET_ALL_MP_GAMER_TAGS_VISIBILITY, gamer_tag_id, True)
            'If Pick({1, 2, 3, 4, 5, 6, 7, 8, 9, 10}) = 1 Then
            '    '1/6的概率被悬赏
            '    With ped.AttachedBlip
            '        .Color = BlipColor.Red2
            '        .Sprite = BlipSprite.BountyHit
            '        .Color = BlipColor.Red2
            '        .SecondaryColor = Color.Red
            '    End With
            '    WantedAmount = Pick({2000, 5000, 7000, 7500, 10000})
            'End If
        End Sub
        Public Sub ExitGame()
            hasExitGame = True
            'Notification.PostTicker($" {Name}  <font size='9' color='rgba(255,255,255,0.7)'>已离开。</font>", False)
        End Sub
        Public ReadOnly Property CurrentActionName As String
            Get
                Return current_action?.DecisionName
            End Get
        End Property
        Public Property WantedAmount As Integer?
            Get
                Return m_wantedAmount
            End Get
            Set(value As Integer?)
                m_wantedAmount = value
                If value.HasValue Then
                    Notification.PostTicker($" {Name}  <font size='11'>遭到悬赏 , 悬赏金为${value}</font>", False)
                    Ped.AttachedBlip.Sprite = BlipSprite.BountyHit
                End If
            End Set
        End Property
        Public Property IsAlly As Boolean
            Get
                Return m_isFriend
            End Get
            Set(value As Boolean)
                m_isFriend = value
                If value Then
                    Ped.RelationshipGroup = FriendRelationshipGroup.Value
                Else
                    Ped.RelationshipGroup = RelationshipGroupHash.NoRelationship
                End If
            End Set
        End Property
        Public Property UsingVechie As Vehicle
            Get
                Return m_usingVehicle
            End Get
            Set(value As Vehicle)
                m_usingVehicle = value
            End Set
        End Property
        Public Property Accuracy As Integer
            Get
                Return Ped.Accuracy
            End Get
            Set(value As Integer)
                value = Clamp(value, 0, 100)
                Ped.SetCombatAttribute(CombatAttributes.PerfectAccuracy, value >= 100)
                Ped.Accuracy = value
            End Set
        End Property
        ''' <summary>
        ''' 获取一个<see cref="Boolean"/>值，指示最近一次循环<see cref="Ped"/>的位置是否改变过
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property PositionChanged As Boolean
            Get
                Return m_positionChanged
            End Get
        End Property
        Public ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property
        Public ReadOnly Property Ped As Ped
            Get
                Return m_ped
            End Get
        End Property
        Public Property Money As Integer
            Get
                Return m_money
            End Get
            Set(value As Integer)
                If value < 0 Then
                    m_money = 0
                Else
                    m_money = value
                End If
                Ped.Money = 0 ' Always set to 0 to prevent money drops
            End Set
        End Property
        Private ReadOnly Property Target As Entity Implements IEntityController.Target
            Get
                Return Ped
            End Get
        End Property
        Public Shared Sub Log(name As String, message As String, Optional color As String = "w")
            'Notification.PostTicker("~c~[" + Now.ToShortTimeString + "]" + name + ": ~" + color + "~" + message, False)
            'Script.Wait(1500)
        End Sub
        Public Sub UpdateBlipDisplayStyle()
            Try
                If CustomBlipDisplayType.HasValue Then
                    Ped.AttachedBlip.DisplayType = CustomBlipDisplayType
                    Return
                End If
                Dim attached_blip As Blip = Ped.AttachedBlip
                If attached_blip IsNot Nothing Then
                    If Ped.IsAlive Then
                        If IsInSameVehicle(PlayerPed, Ped) Then
                            attached_blip.DisplayType = BlipDisplayType.NoDisplay
                        Else
                            If Ped.Position.DistanceTo2D(PlayerPed.Position) < 1000 Then
                                attached_blip.DisplayType = BlipDisplayType.BothMapSelectable
                            Else
                                attached_blip.DisplayType = BlipDisplayType.MainMapSelectable
                            End If
                        End If

                    Else
                        attached_blip.DisplayType = BlipDisplayType.NoDisplay
                    End If

                End If
            Catch ex As Exception
            End Try
        End Sub
        <Obsolete()>
        Public Sub DisplayBotNameOnScreen()
            If Ped.IsDead Then
                Return
            End If
            Dim head_position As Vector3 = Ped.Bones.Item(Bone.IKHead).Position ' + (Ped.Velocity * Game.FPS)
            [Function].Call(Hash.SET_DRAW_ORIGIN, head_position.X, head_position.Y, head_position.Z + 0.6, 0)
            Dim sizeOffset As Single = System.Math.Max(1.0F - ((GameplayCamera.Position - Ped.Position).Length() / 30.0F), 0.3F)
            Dim text As ScaledText = New ScaledText(New PointF(0, 0), Name, 0.4 * sizeOffset, UI.Font.ChaletLondon)
            text.Outline = True
            text.Alignment = Alignment.Center
            text.Color = Drawing.Color.WhiteSmoke
            text.Draw()
            'GC.SuppressFinalize(text)
            [Function].Call(Hash.CLEAR_DRAW_ORIGIN)
        End Sub
        Public Sub DisplayHealthBar()
            If Ped.IsDead Then
                Return
            End If
            If Ped.IsInVehicle() Then
                Return
            End If
            Dim head_position As Vector3 = Ped.Bones.Item(Bone.IKHead).Position
            Dim margin As Single = 2
            Dim health_bar_width As Single = 55 * 0.8 ' 修改为原来的0.8倍
            Dim health_bar_height As Single = 5
            [Function].Call(Hash.SET_DRAW_ORIGIN, head_position.X, head_position.Y, head_position.Z + 0.3, 0)
            Dim outline As ScaledRectangle = New ScaledRectangle(New PointF(-health_bar_width / 2 - margin, -margin), New SizeF(health_bar_width + margin * 2, (health_bar_height + margin * 2)))
            outline.Color = Color.Black
            Dim bar_total As ScaledRectangle = New ScaledRectangle(New Drawing.PointF(-health_bar_width / 2, 0), New Drawing.SizeF(health_bar_width, health_bar_height))
            bar_total.Color = Drawing.Color.DarkGray
            Dim healthColor As Drawing.Color = Color.FromArgb(CInt(255 * 0.85), GetNameColor()) ' 使用与名字相同的颜色，透明度为0.85
            Dim bar_health As ScaledRectangle = New ScaledRectangle(New Drawing.PointF(-health_bar_width / 2, 0), New Drawing.SizeF(health_bar_width * Ped.Health / Ped.MaxHealth, health_bar_height))
            outline.Draw()
            bar_total.Draw()
            bar_health.Color = healthColor ' 设置血条颜色
            bar_health.Draw()
            'GC.SuppressFinalize(bar_total)
            'GC.SuppressFinalize(bar_health)
            [Function].Call(Hash.CLEAR_DRAW_ORIGIN)
        End Sub
        Public Function ForceStartNewDecision(decision As BotDecision) As Boolean
            current_action.Dispose()
            Dim action As BotAction = decision.GetAction(Me)
            action.Invoker = Me
            action.DecisionName = decision.Name
            current_action = action
            action.Run()
            Return True
        End Function
        Public Sub OnTick() Implements IEntityController.OnTick
            '登记位置是否改变
            If Ped.Position = last_location Then
                m_positionChanged = False
            Else
                m_positionChanged = True
                last_location = Ped.Position
            End If
            '自动吃零食
            If Not Ped.IsGettingUp AndAlso Not Ped.IsRagdoll Then
                Ped.Health = Clamp(Ped.Health + 50, 0, Ped.MaxHealth)
            End If
            'UI.Screen.ShowSubtitle($"{Ped.Health}/{Ped.MaxHealth}")
            '每次循环检测动作是否完成。
            If current_action.IsCompleted() Then
                '动作完成，释放资源。
                current_action.Dispose()
                '根据马尔科夫链选择下一个动作

                Dim next_choices = Logic.GetNextDecisions(current_action.DecisionName)
                'Bot.Log(Name, "理论上下一个动作的可能为：" & next_choices.Count)
                Dim next_avaliable_choices As New List(Of DecisionMatrix.Choice)(next_choices.Count)
                For Each choice In next_choices
                    Dim decision As BotDecision = known_decisions.Item(choice.NextDecisionName)
                    If decision.IsAvaliableFor(Me) Then
                        'Bot.Log(Name, decision.Name + "合适:" + choice.ToString())
                        next_avaliable_choices.Add(choice)
                    Else
                        'Bot.Log(Name, decision.Name + "不合适:" + choice.ToString())
                    End If
                Next
                Dim next_decision_name As String = DecisionMatrix.Pick(next_avaliable_choices)
                Log(Name, $"选择下一个动作{next_decision_name}({next_avaliable_choices.Count}/{next_choices.Count})")
                '执行下一个动作
                Dim action = known_decisions.Item(next_decision_name).GetAction(Me)
                '设置属性
                action.Invoker = Me
                action.DecisionName = next_decision_name
                current_action = action
                'Log(Name, "执行 " + current_action.DecisionName)
                action.Run()
            End If
            'Ped.AttachedBlip.Name = Name + ":" + current_action.DecisionName

            ' 处理在线战斗行为
            m_combatBehavior.Process()

            '作战智能
            If Not Ped.IsInVehicle() Then
                Dim combat_target As Ped = Ped.CombatTarget
                If combat_target IsNot Nothing AndAlso combat_target.IsAlive Then
                    Dim best_weapon As WeaponHash = OwnedWeapons.GetBestWeapon(Ped.Position.DistanceTo(combat_target.Position), combat_target.IsInVehicle OrElse combat_target.IsInCover)
                    If Ped.Weapons.Current.Hash <> best_weapon Then
                        Log(Name, "切换合适的武器:" + Weapon.GetHumanNameFromHash(best_weapon))
                        If Ped.Weapons.HasWeapon(best_weapon) Then
                            Ped.Weapons.Select(best_weapon)
                        Else
                            Ped.Weapons.Give(best_weapon, 10, True, True).Tint = PickEnum(Of WeaponTint)()
                        End If

                    End If
                End If
            End If

        End Sub

        Public Sub Disposing() Implements IEntityController.Disposing
            current_action?.Dispose()
            UsingVechie?.MarkAsNoLongerNeeded()
            '击杀提示
            If hasExitGame Then
                'Notification.PostTicker($" {Name}  已离开", False)
            Else
                Dim killer As Entity = Ped.Killer
                If killer?.EntityType = EntityType.Vehicle Then
                    killer = TryCast(killer, Vehicle)?.Driver
                End If
                If killer?.AttachedBlip?.Exists() Then
                    Dim killer_name As String = BotFactory.GetBotNameByPed(killer)
                    If killer_name = Name Then
                        Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(139, 139, 139, 0.7)'>自杀了。</font>", False)
                    Else
                        If String.IsNullOrWhiteSpace(killer_name) Then
                            Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(255,255,255,0.7)'>死了。</font>", False)
                        Else
                            Dim killerBot = BotFactory.GetBotByPed(killer)
                            If killerBot IsNot Nothing Then
                                Notification.PostTicker($" {killerBot.ColoredName}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {Me.ColoredName}。 ", False)
                            Else
                                Notification.PostTicker($" {killer_name}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {Me.ColoredName}。", False)
                            End If
                        End If
                    End If
                ElseIf killer = PlayerPed Then
                    '统计数量，检测是否需要破防退游
                    KilledByPlayer += 1
                    If KilledByPlayer >= MaxBeKilledTimes Then
                        ExitGame()
                    Else
                        Notification.PostTicker($" {PlayerName.DisplayName}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {ColoredName}。 ", False)
                        Versus.PlayerScore(Name) += 1
                        Versus.ShowScore(Ped, Name)
                        IsAlly = False
                    End If


                    '如果AI难度自适应的话就增加全体AI难度
                    If BotPlayerOptions.AdaptiveBot Then
                        BotPlayerOptions.Weeker()
                    End If
                Else
                    Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(255,255,255,0.7)'>死了。</font>", False)
                End If
                If WantedAmount.HasValue Then
                    If Ped.Killer?.Exists() Then
                        If Ped.Killer = PlayerPed Then
                            Game.Player.Money += WantedAmount.Value
                        ElseIf Ped.Killer.AttachedBlip?.Exists() Then
                            Dim killer_bot As Bot = BotFactory.GetBotByPed(Ped.Killer)
                            If killer_bot IsNot Nothing Then
                                Notification.PostTicker($"<font size='11'>对</font>  {ColoredName}  <font size='11'>发起的悬赏追杀的悬赏金 ${WantedAmount} 已由</font> {killer_bot.ColoredName} <font size='11'>夺得</font>", False)
                            Else
                                Dim killer_name As String = Ped.Killer?.AttachedBlip?.Name
                                If Not String.IsNullOrWhiteSpace(killer_name) Then
                                    Notification.PostTicker($"<font size='11'>对</font>  {ColoredName}  <font size='11'>发起的悬赏追杀的悬赏金 ${WantedAmount} 已由</font>  {killer_name}  <font size='11'>夺得</font>", False)
                                End If
                            End If

                        End If
                    End If
                End If
                '重生
                If Not hasExitGame AndAlso BotPlayerOptions.CanBotRegenerate.Enabled AndAlso BotFactory.Pool.Count > 1 Then
                    If Ped.Exists() Then
                        Dim bot As Bot = BotFactory.Regenerate(Ped.Model, Ped.Position.Around(100), Name)
                        bot.Logic.CopyFrom(Logic)
                        bot.Logic.Learn(Pick(BotFactory.Pool.ToArray).Logic)
                        OwnedVehicles.CloneTo(bot.OwnedVehicles)
                        OwnedWeapons.CloneTo(bot.OwnedWeapons)
                        Ped.CloneToTarget(bot.Ped)
                        bot.Ped.ClearBloodDamage()
                        bot.Ped.ClearVisibleDamage()
                        bot.IsAlly = IsAlly
                        bot.MaxBeKilledTimes = MaxBeKilledTimes
                        bot.KilledByPlayer = KilledByPlayer
                        bot.Accuracy = Accuracy
                        If killer IsNot Nothing AndAlso killer.EntityType = EntityType.Ped Then
                            Dim decision As AttackDecision = New AttackDecision(killer)
                            If killer = PlayerPed Then
                                bot.ForceStartNewDecision(decision)
                                'Notification.PostTicker($" {Name}  将对玩家开展报复行动", False) '后期可删除
                            ElseIf (killer.IsOnScreen OrElse bot.Ped.IsOnScreen) AndAlso bot.Ped.Position.DistanceTo(GameplayCamera.Position) < 100 Then
                                bot.ForceStartNewDecision(decision)
                                'Dim killer_name As String = BotFactory.GetBotNameByPed(killer)
                                'Notification.PostTicker($" {Name}  将对  {killer_name}  开展报复行动", False)
                            End If
                        End If

                    Else
                        Notification.PostTicker(Name + " ~o~重生失败", True)
                        'Dim bot As Bot = BotFactory.CreateBot()
                        'bot.Logic.CopyFrom(Logic)
                        'bot.Logic.Learn(Pick(BotFactory.Pool.ToArray).Logic)
                    End If

                    'Notification.PostTicker($" {Name}  已重生", True)
                End If
            End If
            '群体智能
            BotFactory.Pool.Update()
            If BotFactory.Pool.Count > 0 Then
                For Each bot As Bot In BotFactory.Pool
                    If bot IsNot Me Then
                        bot.Logic.Avoid(Logic)
                    End If
                Next
                'Notification.PostTicker($" {Name}  已向其他Bot广播死亡经验", True)
            End If
        End Sub

        Public Function CanDispose() As Boolean Implements IEntityController.CanDispose
            Return hasExitGame OrElse Ped.IsDead
        End Function

        Public Sub Process() Implements ITickProcessable.Process
            UpdateBlipDisplayStyle()
            nameLabel.LabelColor = GetNameColor()
            If Ped.IsOnScreen AndAlso Ped.Position.DistanceTo(PlayerPed.Position) < 80 Then
                nameLabel.Visible = True
            Else
                nameLabel.Visible = False
            End If
            If Game.Player.IsTargeting(Ped) Then
                DisplayHealthBar()
            End If
            '如果Bot乘坐载具则更新Blip
            Dim current_vehicle As Vehicle = Ped.CurrentVehicle
            If current_vehicle IsNot Nothing AndAlso Not WantedAmount.HasValue Then
                Dim model As UInteger = [Function].Call(Of UInteger)(Hash.GET_ENTITY_MODEL, current_vehicle.Handle)
                If VehicleBlipDisplay.Table.ContainsKey(model) Then
                    Dim display As BlipDisplay = VehicleBlipDisplay.Table.Item(model)
                    Dim ped_blip As Blip = Ped.AttachedBlip
                    If ped_blip IsNot Nothing Then
                        ped_blip.ShowsHeadingIndicator = False
                        '如果不一样就更新图标样式
                        If display.Sprite <> ped_blip.Sprite Then
                            Dim ceo_color As BlipColor = ped_blip.Color
                            ped_blip.Sprite = display.Sprite
                            ped_blip.Color = ceo_color
                            ped_blip.Name = Name
                        End If
                        '更新图标方向
                        If display.UpdateRotation Then
                            ped_blip.Rotation = Ped.Heading
                        End If
                        ShowAllyIndicatorIfIsFriend(ped_blip)
                    End If
                Else '恢复默认样式
                    SetDefaultBlip()
                End If
            Else
                SetDefaultBlip()
            End If
            '连发火箭筒
            Dim currentWeapon As Weapon = Ped.Weapons.Current
            If OwnedWeapons.KnownRocketWeapons.Contains(currentWeapon.Hash) AndAlso PlayerPed.Position.DistanceTo(Ped.Position) < 100 Then
                currentWeapon.InfiniteAmmoClip = True
            End If
        End Sub
        Private Sub ShowAllyIndicatorIfIsFriend(blip As Blip)
            If IsAlly Then
                blip.ShowsFriendIndicator = True
                blip.ShowsCrewIndicator = True
            End If
        End Sub
        Private Sub SetDefaultBlip()
            Dim ped_blip As Blip = Ped.AttachedBlip
            If ped_blip IsNot Nothing Then
                ped_blip.ShowsHeadingIndicator = True
                If WantedAmount.HasValue Then
                    ped_blip.Color = BlipColor.Red2
                    ped_blip.Sprite = BlipSprite.BountyHit
                    ped_blip.Color = BlipColor.Red2
                    ped_blip.SecondaryColor = Color.Red
                    ped_blip.IsFriendly = False
                    ped_blip.Name = "悬赏中: $" + WantedAmount.ToString() + " " + Name
                    ped_blip.ShowsHeadingIndicator = False
                Else
                    Dim ceo_color As BlipColor = ped_blip.Color
                    ped_blip.Sprite = BlipSprite.Standard
                    ped_blip.ShowsHeadingIndicator = True
                    ped_blip.Name = Name
                    ped_blip.Color = ceo_color
                    [Function].Call(Hash.SHOW_HEIGHT_ON_BLIP, ped_blip, False)
                End If
                ShowAllyIndicatorIfIsFriend(ped_blip)
            End If
        End Sub
        Public Function CanBeRemoved() As Boolean Implements ITickProcessable.CanBeRemoved
            If Ped.Exists AndAlso Ped.IsDead Then
                Dim blip As Blip = Ped.AttachedBlip
                If blip IsNot Nothing Then
                    blip.DisplayType = BlipDisplayType.NoDisplay
                End If
            End If
            Return CanDispose()
        End Function
    End Class
    Public Class BotFactory
        Private Shared ReadOnly Rng As New System.Random()
        Private Shared common_decisions As List(Of BotDecision)
        Public Shared ReadOnly Property Pool As ManagedCollection(Of Bot) = New ManagedCollection(Of Bot)(Function(bot) bot.Ped.IsAlive)
        Public Shared Function GetBotNameByPed(ped As Entity) As String
            For Each bot As Bot In Pool
                If bot.Ped = ped Then
                    Return bot.Name
                End If
            Next
            Return Nothing
        End Function
        Public Shared Function IsBot(ped As Entity) As Boolean
            If ped Is Nothing Then
                Return False
            Else
                For Each bot As Bot In Pool
                    If bot.Ped = ped Then
                        Return True
                    End If
                Next
                Return False
            End If
        End Function
        Public Shared Function GetBotByPed(ped As Entity) As Bot
            For Each bot As Bot In Pool
                If bot.Ped = ped Then
                    Return bot
                End If
            Next
            Return Nothing
        End Function
        Public Shared ReadOnly Iterator Property ExistingBotNames As IEnumerable(Of String)
            Get
                For Each bot As Bot In Pool
                    Yield bot.Name
                Next
            End Get
        End Property
        Private Shared Function GetCommonDecisions() As List(Of BotDecision)
            If common_decisions Is Nothing Then
                common_decisions = New List(Of BotDecision)()
                common_decisions.Add(EmptyDecision.Default) '非常重要
                common_decisions.Add(New PlayerCommandedDecision()) '非常重要，不加的话玩家就无法向Bot指派特殊任务
                common_decisions.Add(New EnterPlayerVehicleDecision())
                common_decisions.Add(New CallAndEnterPersonalVehicleDecision(False))
                common_decisions.Add(New CallAndEnterPersonalVehicleDecision(True))
                common_decisions.Add(New RobDecision())
                common_decisions.Add(New BeWantedDecision())

                common_decisions.Add(New FleeDecision())
                common_decisions.Add(New JetDecision())
                '----- 武器商店 -----
                '前往武器商店
                common_decisions.Add(New VisitDecision("前往武装国度", Map.GunShops, AddressOf Pass))
                '购买武器决策
                common_decisions.Add(New WeaponShopDecision(WeaponHash.APPistol, 4500, 30))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.CarbineRifle, 14500, 55))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.AssaultShotgun, 17500, 15))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.SniperRifle, 2500, 200))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.HeavySniper, 15000, 400))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.RPG, 5000, 125, True))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.Minigun, 15000, 90, True))
                common_decisions.Add(New WeaponShopDecision(WeaponHash.Railgun, 1000, 50, True))
                'common_decisions.Add(New WeaponShopDecision(WeaponHash.RailgunXmas3, 1000, 50, True))
                '暗杀任务
                common_decisions.Add(New KillerDecision(VehicleHash.Baller5, PedHash.Highsec02SMM, WeaponHash.CombatPistol, 1))
                common_decisions.Add(New KillerDecision(VehicleHash.Cavalcade, PedHash.Goons01GMM, WeaponHash.SMG, 2))
                common_decisions.Add(New KillerDecision(Nothing, PedHash.CiaSec01SMM, WeaponHash.CombatPistol, 3))

                '----- 载具商店 -----
                Dim ten_k As Integer = 10000
                common_decisions.Add(New VehiclePurchaceDecision(4 * ten_k, VehicleHash.Oppressor2, False))
                VehicleBlipDisplay.Table.Item(VehicleHash.Oppressor2) = New BlipDisplay(BlipSprite.OppressorMkII, False)

                common_decisions.Add(New VehiclePurchaceDecision(3 * ten_k, VehicleHash.Rhino, True))
                VehicleBlipDisplay.Table.Item(VehicleHash.Rhino) = New BlipDisplay(BlipSprite.Tank, True)

                common_decisions.Add(New VehiclePurchaceDecision(1 * ten_k, VehicleHash.NightShark, True))
                common_decisions.Add(New VehiclePurchaceDecision(1 * ten_k, VehicleHash.Kuruma2, False))

                common_decisions.Add(New VehiclePurchaceDecision(2 * ten_k, VehicleHash.Ruiner2, True))
                VehicleBlipDisplay.Table.Item(VehicleHash.Ruiner2) = New BlipDisplay(BlipSprite.Ruiner2000, False)

                common_decisions.Add(New VehiclePurchaceDecision(5 * ten_k, VehicleHash.CogCabrio, False))
                common_decisions.Add(New VehiclePurchaceDecision(5000, VehicleHash.Infernus, False))
                common_decisions.Add(New VehiclePurchaceDecision(6000, VehicleHash.Zentorno, False))
                common_decisions.Add(New VehiclePurchaceDecision(2000, VehicleHash.Penumbra, False))
                common_decisions.Add(New VehiclePurchaceDecision(1000, VehicleHash.Schafter2, False))

                common_decisions.Add(New VehiclePurchaceDecision(6000, VehicleHash.Zhaba, True))
                VehicleBlipDisplay.Table.Item(VehicleHash.Zhaba) = New BlipDisplay(BlipSprite.Zhaba, False)

                common_decisions.Add(New VehiclePurchaceDecision(10 * ten_k, VehicleHash.MiniTank, True))
                VehicleBlipDisplay.Table.Item(VehicleHash.MiniTank) = New BlipDisplay(BlipSprite.RCTank, True)

                common_decisions.Add(New VehiclePurchaceDecision(10 * ten_k, VehicleHash.Khanjari, True))
                VehicleBlipDisplay.Table.Item(VehicleHash.Khanjari) = New BlipDisplay(BlipSprite.Khanjali, True)

                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Ignus, False))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Zorrusso, False))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Osiris, False))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Reaper, False))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Tyrant, False))
                common_decisions.Add(New VehiclePurchaceDecision(3 * ten_k, VehicleHash.Scramjet, True))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Brickade, True))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Brickade2, True))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Formula, False))
                common_decisions.Add(New VehiclePurchaceDecision(ten_k, VehicleHash.Bruiser2, True))
                common_decisions.Add(New VehiclePurchaceDecision(3 * ten_k, VehicleHash.Pounder2, True))
                '----- 服装店 -----
                common_decisions.Add(New VisitDecision("前往服装店", Map.ClothesShops, Sub(bot)
                                                                                      'bot.Ped.Style.RandomizeOutfit()
                                                                                      bot.Ped.Style.RandomizeProps()
                                                                                  End Sub
                                                                                      ))
            End If
            Return common_decisions
        End Function
        Private Shared Function GetNewBotName() As String
            Dim name As String = BotNames.PickOne
            Dim index As ULong = 1
            While ExistingBotNames.Contains(name)
                name = BotNames.PickOne + Hex(index)
                index *= 17
                index += 29
            End While
            Return name
        End Function
        ''' <summary>
        ''' 将路人升级为<see cref="Bot"/>。
        ''' </summary>
        ''' <param name="ped">需要升级的路人。</param>
        Public Shared Sub UpgradePedToBot(ped As Ped)
            'ped.DecisionMaker = DecisionMaker.op_Implicit(DecisionMakerTypeHash.Empty)
            'ped.MarkAsMissionEntity(True)
            Dim bot As Bot = New Bot(ped, GetNewBotName(), GetCommonDecisions(), EmptyDecision.Default.GetAction(Nothing))
            bot.Ped.CombatAbility = CombatAbility.Professional
            bot.Ped.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
            bot.OwnedWeapons.Give(WeaponHash.Pistol, 2000)
            bot.Money = 0 ' Set to 0 to prevent money drops
            bot.Ped.Money = 0 ' Explicitly set Ped.Money to 0
            EntityManagement.AddController(bot)
            FrameTicker.Add(bot)
            bot.IsAlly = True
            Pool.Add(bot)
        End Sub
        Private Shared Sub SetRandomFace(ped As Ped)
            Dim father As Integer = Pick(45, 0)
            Dim mother As Integer = Pick(45, 0)
            Dim skin_mix As Single = Pick(100) / 100
            Dim shape_mix As Single = Pick(100) / 100
            PedStyles.SetHeadBlendData(ped, New HeadBlendData(father, mother, father, father, mother, mother, skin_mix, shape_mix, shape_mix))
        End Sub
        Private Shared Sub SetRandomOutfit(ol_character As Style, gender As Gender)
            ol_character.Item(PedComponentType.Head).SetVariation(Pick({0, 0, Pick(249)}))
            ol_character.Item(PedComponentType.Face).SetVariation(Pick(46))

            ' 90% chance to apply a mask
            If Rng.NextDouble() < 0.9 Then
                Dim maskComponent = ol_character.Item(CType(1, PedComponentType)) ' Mask component ID is 1
                If maskComponent.HasVariations Then
                    maskComponent.SetVariation(Rng.Next(0, maskComponent.Count))
                End If
            End If

            ' Always set a random hairstyle. This also fixes baldness if a mask was applied.
            Dim hairComponent As PedComponent = ol_character.Item(PedComponentType.Hair)
            If hairComponent.HasVariations Then
                hairComponent.SetVariation(Rng.Next(0, hairComponent.Count))
            End If

            With ol_character
                Dim combo As TorsoCombonations
                If gender = Gender.Male Then
                    combo = Pick({TorsoCombonations.Male.LongClothes})
                Else
                    combo = Pick({TorsoCombonations.Female.LongClothes})
                End If
                .Item(PedComponentType.Torso).SetVariation(Pick(combo.TorsoIndeces))
                If gender = Gender.Female Then
                    .Item(PedComponentType.Legs).SetVariation(Pick((Enumerable.Range(0, 197).Except({13, 32, 40, 46, 90, 100, 101, 114, 115, 116, 117, 122, 127, 132, 136, 164, 169})).ToList))
                Else
                    .Item(PedComponentType.Legs).SetVariation(Pick(Enumerable.Range(0, 107).Except({11, 38, 39, 40, 44, 46, 57, 72, 74, 87, 91, 97, 101, 107}).ToList()))
                End If

                .Item(PedComponentType.Shoes).SetVariation(Pick(Enumerable.Range(0, 25).Except({12, 15, 13}).ToList))
                .Item(PedComponentType.Special2).SetVariation(Pick(170))
                .Item(PedComponentType.Torso2).SetVariation(Pick(combo.ClothIndeces)) '.SetVariation(Pick(324))
                .Item(PedComponentType.Special1).SetVariation(0, 0)
                .Item(PedComponentType.Hands).SetVariation(0, 0)
                .Item(PedComponentType.Special3).SetVariation(0, 0)
                .Item(PedComponentType.Textures).SetVariation(0, 0)
            End With
            For Each component As PedComponentType In {PedComponentType.Torso, PedComponentType.Legs, PedComponentType.Shoes, PedComponentType.Special2, PedComponentType.Torso2}
                Dim item As PedComponent = ol_character.Item(component)
                If Not item.HasVariations Then
                    item.SetVariation(0)
                End If
            Next
            For Each component As PedComponent In ol_character.GetAllComponents()
                ol_character.PreloadVariationData(component.Type, component.Index, component.TextureIndex)
            Next
        End Sub
        ''' <summary>
        ''' 创建一个高级的<see cref="Bot"/>，拥有初始武器和载具。
        ''' </summary>
        ''' <param name="ped">需要控制的<see cref="Ped"/>。</param>
        ''' <param name="name"><see cref="Bot.Name"/>，如果未指定则将从<see cref="BotNames.PickOne"/>随机选择一个。</param>
        ''' <returns>创建的<see cref="Bot"/>。</returns>
        Private Shared Function CreateAdvancedBot(ped As Ped, Optional name As String = Nothing) As Bot
            Dim bot As Bot = New Bot(ped, If(name, GetNewBotName()), GetCommonDecisions(), EmptyDecision.Default.GetAction(Nothing))
            'bot.Ped.CombatAbility = CombatAbility.Professional
            'bot.Ped.SetCombatAttribute(CombatAttributes.PerfectAccuracy, True)
            bot.Money = 0 ' Set to 0 to prevent money drops, but internal tracking will still work
            '初始化武器
            Dim vehicle_weapon As WeaponHash = Pick({WeaponHash.HeavyPistol, WeaponHash.PistolMk2, WeaponHash.APPistol, WeaponHash.MiniSMG, WeaponHash.TacticalSMG})
            bot.OwnedWeapons.Give(vehicle_weapon, 99999) '基本上这些武器不在载具上是不会选的
            bot.OwnedWeapons.Give(WeaponHash.AssaultShotgun, 20)
            Dim onfoot_weapon As WeaponHash = Pick({WeaponHash.HeavyRifle, WeaponHash.CarbineRifle, WeaponHash.AssaultRifle, WeaponHash.CarbineRifleMk2, WeaponHash.AssaultrifleMk2, WeaponHash.ServiceCarbine, WeaponHash.MilitaryRifle})
            bot.OwnedWeapons.Give(onfoot_weapon, 31)

            'rocker武器
            Dim rocket_weapon As WeaponHash = Pick({WeaponHash.Widowmaker, WeaponHash.Minigun, WeaponHash.RPG, WeaponHash.Railgun, WeaponHash.GrenadeLauncher})
            bot.OwnedWeapons.Give(rocket_weapon, 45)
            bot.OwnedWeapons.KnownRocketWeapons.Add(rocket_weapon)

            bot.OwnedWeapons.Give(WeaponHash.HeavySniper, 50)
            '初始化载具
            'bot.OwnedVehicles.Add(VehicleHash.Rhino, True)
            'VehicleBlipDisplay.Table.Item(VehicleHash.Rhino) = New BlipDisplay(BlipSprite.Tank, True)

            bot.OwnedVehicles.Add(VehicleHash.Vigilante, False)

            'bot.OwnedVehicles.Add(VehicleHash.Kuruma2, True)
            bot.OwnedVehicles.Add(VehicleHash.Dukes2, True)



            bot.OwnedVehicles.Add(VehicleHash.Scarab3, False)
            VehicleBlipDisplay.Table.Item(VehicleHash.Scarab3) = New BlipDisplay(BlipSprite.Scarab, True)

            bot.OwnedVehicles.Add(VehicleHash.Phantom2, False)
            VehicleBlipDisplay.Table.Item(VehicleHash.Phantom2) = New BlipDisplay(BlipSprite.PhantomWedge, False)
            EntityManagement.AddController(bot)
            FrameTicker.Add(bot)
            Pool.Add(bot)
            Return bot
        End Function
        Public Shared Function CreateRandomOnlinePlayer() As Bot
            Dim raw_location As Vector3 = PlayerPed.Position.Around(Pick({800, 1000, 1200, 1500}))
            Dim swamp_location As Vector3
            Dim model As PedHash = Pick({PedHash.FreemodeFemale01, PedHash.FreemodeMale01})
            Dim ped As Ped
            If Not World.GetSafePositionForPed(raw_location, swamp_location, GetSafePositionFlags.Default) Then
                'UI.Screen.ShowSubtitle("无法找到生成位置，使用载具代替")
                Dim v As Vehicle = CreateVehicle(Nothing, Pick({800, 1000, 1200, 1500}))
                ped = v.CreatePedOnSeat(VehicleSeat.Driver, model)
                v.MarkAsNoLongerNeeded()
            Else
                ped = World.CreatePed(model, swamp_location)
            End If
            If ped Is Nothing Then
                Script.Wait(1000)
                Return CreateRandomOnlinePlayer()
            End If
            SetRandomOutfit(ped.Style, ped.Gender)
            SetRandomFace(ped)
            Return CreateAdvancedBot(ped)
        End Function
        ''' <summary>
        ''' 创建<see cref="Bot"/>。
        ''' </summary>
        Public Shared Function CreateBot() As Bot
            Dim raw_location As Vector3 = PlayerPed.Position.Around(Pick({800, 1000, 1200, 1500}))
            Dim swamp_location As Vector3
            Dim ped As Ped
            If Not World.GetSafePositionForPed(raw_location, swamp_location, GetSafePositionFlags.OnlyNetworkSpawn) Then
                'UI.Screen.ShowSubtitle("无法找到生成位置，使用载具代替")
                Dim v As Vehicle = CreateVehicle(Nothing, Pick({800, 1000, 1200, 1500}))
                ped = v.CreateRandomPedOnSeat(VehicleSeat.Driver)
                v.MarkAsNoLongerNeeded()
            Else
                ped = World.CreateRandomPed(swamp_location)
            End If
            If ped Is Nothing Then
                Script.Wait(1000)
                Return CreateBot()
            End If
            Return CreateAdvancedBot(ped)
        End Function
        Public Shared Function Regenerate(ped_model As Model, approximate_location As Vector3, name As String) As Bot
            Dim swamp_location As Vector3
            Dim ped As Ped
            If Not World.GetSafePositionForPed(approximate_location, swamp_location, GetSafePositionFlags.OnlyNetworkSpawn) Then
                'UI.Screen.ShowSubtitle("无法找到生成位置，使用载具代替")
                Dim v As Vehicle = CreateVehicle(Nothing, Pick({800, 1000, 1200, 1500}))
                ped = v.CreatePedOnSeat(VehicleSeat.Driver, ped_model) 'CreateRandomPedOnSeat(VehicleSeat.Driver)
                v.MarkAsNoLongerNeeded()
            Else
                ped = World.CreatePed(ped_model, swamp_location)
            End If
            If ped Is Nothing Then
                Script.Wait(1000)
                Return Regenerate(ped_model, approximate_location, name)
            End If
            Return CreateAdvancedBot(ped, name)
        End Function
    End Class

    ' 实现类似线上模式玩家的战斗行为
    Public Class OnlineCombatBehavior
        Private ReadOnly m_bot As Bot
        Private ReadOnly m_rng As New Random()
        
        ' 战斗行为配置
        Private Const STRAFE_DISTANCE As Single = 5.0F ' 左右位移距离
        Private Const STRAFE_SPEED As Single = 10.0F ' 位移速度
        Private Const STRAFE_INTERVAL_MIN As Integer = 500 ' 最短位移间隔(毫秒)
        Private Const STRAFE_INTERVAL_MAX As Integer = 2000 ' 最长位移间隔(毫秒)
        Private Const JUMP_CHANCE As Single = 0.02F ' 每次更新跳跃的概率
        Private Const COMBAT_DISTANCE_THRESHOLD As Single = 50.0F ' 激活战斗行为的距离阈值
        Private Const ROLL_CHANCE As Single = 0.01F ' 每次更新翻滚的概率
        Private Const RANDOM_SHOOT_CHANCE As Single = 0.05F ' 随机射击的概率
        Private Const WEAPON_SWITCH_CHANCE As Single = 0.005F ' 随机切换武器的概率

        ' 状态变量
        Private m_lastStrafeTime As Integer = 0
        Private m_nextStrafeTime As Integer = 0
        Private m_strafeDirection As Integer = 0 ' 0=无位移, 1=左, 2=右
        Private m_targetPos As Vector3 = Vector3.Zero
        Private m_isStrafing As Boolean = False
        Private m_combatTarget As Ped = Nothing
        Private m_lastRollTime As Integer = 0
        Private Const ROLL_COOLDOWN As Integer = 5000 ' 翻滚冷却时间(毫秒)

        Public Sub New(bot As Bot)
            m_bot = bot
            ResetStrafeTimer()
            m_lastRollTime = Game.GameTime - ROLL_COOLDOWN ' 初始化为可以立即翻滚
        End Sub

        ' 重置位移计时器
        Private Sub ResetStrafeTimer()
            m_lastStrafeTime = Game.GameTime
            m_nextStrafeTime = m_lastStrafeTime + m_rng.Next(STRAFE_INTERVAL_MIN, STRAFE_INTERVAL_MAX)
        End Sub

        ' 处理战斗行为
        Public Sub Process()
            ' 检查是否有战斗目标
            m_combatTarget = m_bot.Ped.CombatTarget
            
            ' 无论是否有战斗目标，都主动检测潜在目标
            ' 这确保了PED会更积极地寻找并攻击玩家
            DetectPotentialTargets()
            
            ' 如果没有战斗目标或者目标已死亡，直接返回
            If m_combatTarget Is Nothing OrElse Not m_combatTarget.Exists() OrElse m_combatTarget.IsDead Then
                Return
            End If
            
            ' 检查距离是否在战斗阈值内
            Dim distanceToTarget As Single = m_bot.Ped.Position.DistanceTo(m_combatTarget.Position)
            If distanceToTarget > COMBAT_DISTANCE_THRESHOLD Then
                Return
            End If
            
            ' 如果在载具中不执行战斗行为
            If m_bot.Ped.IsInVehicle() Then
                Return
            End If
            
            ' 处理左右位移
            ProcessStrafe()
            
            ' 处理随机跳跃
            ProcessJump()
            
            ' 处理随机翻滚
            ProcessRoll()
            
            ' 处理随机射击
            ProcessRandomShooting()
            
            ' 处理随机切换武器
            ProcessWeaponSwitch()
        End Sub
        
        ' 检测潜在目标
        Private Sub DetectPotentialTargets()
            ' 如果在载具中，不主动寻找目标
            If m_bot.Ped.IsInVehicle() Then
                ' 在载具中时，检测是否可以撞击玩家
                DetectVehicleRamTargets()
                Return
            End If
            
            ' 检测附近的玩家或其他Bot
            Dim playerPed As Ped = Game.Player.Character
            If playerPed.Exists() AndAlso playerPed.IsAlive AndAlso Not m_bot.IsAlly Then
                Dim distanceToPlayer As Single = m_bot.Ped.Position.DistanceTo(playerPed.Position)
                ' 增加主动攻击概率，从10%提高到50%
                If distanceToPlayer < COMBAT_DISTANCE_THRESHOLD AndAlso m_rng.NextDouble() < 0.9 Then
                    ' 主动攻击玩家
                    m_bot.Ped.Task.FightAgainst(playerPed)
                    ' 确保AI不会停止战斗
                    m_bot.Ped.SetCombatAttribute(CombatAttributes.AlwaysFight, True)
                    m_bot.Ped.BlockPermanentEvents = True
                End If
            End If
        End Sub
        
        ' 检测载具撞击目标
        Private Sub DetectVehicleRamTargets()
            ' 只有当AI在载具中并且是驾驶员时才执行
            If Not m_bot.Ped.IsInVehicle() OrElse m_bot.Ped.SeatIndex <> VehicleSeat.Driver Then
                Return
            End If
            
            Dim vehicle As Vehicle = m_bot.Ped.CurrentVehicle
            If vehicle Is Nothing Then
                Return
            End If
            
            ' 检测附近的玩家
            Dim playerPed As Ped = Game.Player.Character
            If playerPed.Exists() AndAlso playerPed.IsAlive AndAlso Not m_bot.IsAlly Then
                Dim distanceToPlayer As Single = vehicle.Position.DistanceTo(playerPed.Position)
                ' 增加检测距离到150米，提高撞击概率到80%
                If distanceToPlayer < 150.0F AndAlso m_rng.NextDouble() < 0.8 Then
                    ' 增加车辆速度，使撞击更有力
                    Dim currentSpeed As Single = vehicle.Speed
                    If currentSpeed < 20.0F Then
                        vehicle.Speed = 30.0F
                    End If
                    
                    ' 如果玩家在载具中，撞击玩家的载具
                    If playerPed.IsInVehicle() Then
                        Dim playerVehicle As Vehicle = playerPed.CurrentVehicle
                        If playerVehicle IsNot Nothing Then
                            ' 使用原生任务函数执行撞击，增加速度和精度参数
                            ' 参数说明：
                            ' 13 = CTaskVehicleMissionFlag::MISSION_RAM
                            ' 80.0F = 最大速度
                            ' 786603 = 驾驶标志(激进驾驶)
                            ' 10.0F = 最小距离(越小越激进)
                            ' 5.0F = 精度(越小越精确)
                            [Function].Call(Hash.TASK_VEHICLE_MISSION, m_bot.Ped, vehicle, playerVehicle, 13, 80.0F, 786603, 10.0F, 5.0F, True)
                            
                            ' 设置车辆不会减速，保持高速撞击
                            [Function].Call(Hash.SET_VEHICLE_FORWARD_SPEED, vehicle, 50.0F)
                            
                            ' 设置驾驶员更激进
                            m_bot.Ped.DrivingAggressiveness = 1.0F
                            m_bot.Ped.VehicleDrivingFlags = VehicleDrivingFlags.AvoidVehicles Or VehicleDrivingFlags.AllowGoingWrongWay Or VehicleDrivingFlags.AllowMedianCrossing
                        End If
                    Else
                        ' 如果玩家在步行，直接朝玩家位置驾驶
                        [Function].Call(Hash.TASK_VEHICLE_MISSION, m_bot.Ped, vehicle, playerPed, 13, 80.0F, 786603, 10.0F, 5.0F, True)
                        
                        ' 设置车辆不会减速，保持高速撞击
                        [Function].Call(Hash.SET_VEHICLE_FORWARD_SPEED, vehicle, 50.0F)
                        
                        ' 设置驾驶员更激进
                        m_bot.Ped.DrivingAggressiveness = 1.0F
                        m_bot.Ped.VehicleDrivingFlags = VehicleDrivingFlags.AvoidVehicles Or VehicleDrivingFlags.AllowGoingWrongWay Or VehicleDrivingFlags.AllowMedianCrossing
                    End If
                End If
            End If
        End Sub
        
        ' 处理左右位移行为
        Private Sub ProcessStrafe()
            ' 如果当前正在位移中，检查是否到达目标位置
            If m_isStrafing Then
                If m_bot.Ped.Position.DistanceTo(m_targetPos) < 1.0F Then
                    m_isStrafing = False
                    ResetStrafeTimer()
                End If
                Return
            End If
            
            ' 检查是否到达下一次位移时间
            If Game.GameTime < m_nextStrafeTime Then
                Return
            End If
            
            ' 决定位移方向 (交替左右)
            m_strafeDirection = If(m_strafeDirection = 1, 2, 1) ' 1=左, 2=右
            
            ' 计算位移目标位置
            Dim moveVector As Vector3
            If m_strafeDirection = 1 Then ' 左
                moveVector = -m_bot.Ped.RightVector * STRAFE_DISTANCE
            Else ' 右
                moveVector = m_bot.Ped.RightVector * STRAFE_DISTANCE
            End If
            
            ' 设置目标位置
            m_targetPos = m_bot.Ped.Position + moveVector
            
            ' 执行位移任务
            ExecuteStrafeTask()
            
            ' 更新状态
            m_isStrafing = True
        End Sub
        
        ' 执行位移任务
        Private Sub ExecuteStrafeTask()
            ' 临时接管AI控制
            m_bot.Ped.Task.ClearSecondary()
            
            ' 使用原生任务函数执行位移并瞄准目标
            ' TASK_GO_TO_COORD_WHILE_AIMING_AT_ENTITY
            [Function].Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_ENTITY, 
                m_bot.Ped, 
                m_targetPos.X, m_targetPos.Y, m_targetPos.Z, 
                m_combatTarget, 
                STRAFE_SPEED, 
                True, ' 射击
                1.0F, ' 停止距离
                0.0F, ' 未知参数
                False, ' 不使用导航网格
                CType(FiringPattern.FullAuto, UInteger)) ' 全自动射击模式
        End Sub
        
        ' 处理随机跳跃行为
        Private Sub ProcessJump()
            ' 如果AI正在跳跃或者正在起身，不执行跳跃
            If m_bot.Ped.IsJumping OrElse m_bot.Ped.IsGettingUp Then
                Return
            End If
            
            ' 随机决定是否跳跃
            If m_rng.NextDouble() < JUMP_CHANCE Then
                ' 执行跳跃任务
                m_bot.Ped.Task.Jump()
            End If
        End Sub
        
        ' 处理随机翻滚行为
        Private Sub ProcessRoll()
            ' 检查冷却时间
            If Game.GameTime - m_lastRollTime < ROLL_COOLDOWN Then
                Return
            End If
            
            ' 如果AI正在跳跃或者正在起身，不执行翻滚
            If m_bot.Ped.IsJumping OrElse m_bot.Ped.IsGettingUp Then
                Return
            End If
            
            ' 随机决定是否翻滚
            If m_rng.NextDouble() < ROLL_CHANCE Then
                ' 由于没有直接的翻滚API，我们使用跳跃来模拟快速移动
                m_bot.Ped.Task.Jump()
                
                ' 更新翻滚时间
                m_lastRollTime = Game.GameTime
            End If
        End Sub
        
        ' 处理随机射击行为
        Private Sub ProcessRandomShooting()
            ' 如果没有武器或者不在战斗中，不执行随机射击
            If Not m_bot.Ped.Weapons.Current.IsPresent OrElse Not m_bot.Ped.IsInCombat Then
                Return
            End If
            
            ' 随机决定是否射击
            If m_rng.NextDouble() < RANDOM_SHOOT_CHANCE Then
                ' 执行射击任务
                [Function].Call(Hash.TASK_SHOOT_AT_ENTITY, m_bot.Ped, m_combatTarget, 500, CType(FiringPattern.FullAuto, UInteger))
            End If
        End Sub
        
        ' 处理随机切换武器
        Private Sub ProcessWeaponSwitch()
            ' 如果在战斗中，有小概率切换武器
            If m_bot.Ped.IsInCombat AndAlso m_rng.NextDouble() < WEAPON_SWITCH_CHANCE Then
                ' 获取当前武器
                Dim currentWeapon As WeaponHash = m_bot.Ped.Weapons.Current.Hash
                
                ' 获取所有拥有的武器
                Dim availableWeapons As List(Of WeaponHash) = New List(Of WeaponHash)()
                
                ' 遍历所有可能的武器哈希值，检查NPC是否拥有该武器
                For Each weaponHash As WeaponHash In [Enum].GetValues(GetType(WeaponHash))
                    If m_bot.Ped.Weapons.HasWeapon(weaponHash) AndAlso weaponHash <> currentWeapon Then
                        availableWeapons.Add(weaponHash)
                    End If
                Next
                
                ' 如果有其他武器可用，随机选择一个
                If availableWeapons.Count > 0 Then
                    Dim randomWeapon As WeaponHash = availableWeapons(m_rng.Next(0, availableWeapons.Count))
                    m_bot.Ped.Weapons.Select(randomWeapon)
                End If
            End If
        End Sub
    End Class

    ''' <summary>
    ''' 定期显示假的AI玩家离开游戏的通知。
    ''' </summary>
    Public Class FakePlayerLeaveNotifier
        Private Shared ReadOnly Rng As New Random()
        Private Shared nextNotificationTime As Integer

        Shared Sub New()
            ' 静态构造函数，用于初始化第一个通知时间
            ScheduleNextNotification()
        End Sub

        Public Shared Sub Process()
            If Game.GameTime > nextNotificationTime Then
                ' It's time to show the notification
                ' Randomly determine the number of notifications (1-3)
                Dim notificationCount As Integer = Rng.Next(1, 4)
                Dim usedNames As New HashSet(Of String)()

                For i As Integer = 1 To notificationCount
                    Dim randomName As String
                    ' Make sure the name is not repeated
                    Do
                        randomName = BotNames.PickOne()
                    Loop While usedNames.Contains(randomName)

                    usedNames.Add(randomName)
                    Notification.PostTicker($" {randomName}  <font size='9' color='rgba(255,255,255,0.7)'>已离开。</font>", False)
                Next
                ScheduleNextNotification()
            End If
        End Sub

        Private Shared Sub ScheduleNextNotification()
            ' Random interval between 6 and 35 seconds (in milliseconds)
            Dim interval As Integer = Rng.Next(6000, 35001)
            nextNotificationTime = Game.GameTime + interval
        End Sub
    End Class

    ''' <summary>
    ''' 用于处理 FakePlayerLeaveNotifier 的计时器。
    ''' </summary>
    Public Class FakePlayerLeaveNotifierProcessor
        Implements ITickProcessable

        Public Function CanBeRemoved() As Boolean Implements ITickProcessable.CanBeRemoved
            Return False ' 永不移除此处理器
        End Function

        Public Sub Process() Implements ITickProcessable.Process
            FakePlayerLeaveNotifier.Process()
        End Sub
    End Class

    ' --- New Class for Player Direct Control Logic ---
    Public Class PlayerDirectControl
        Inherits Script

        Private Shared strafingPed As Ped = Nothing
        Private Shared strafeState As Integer = 0 ' 0: idle, 1: moving left, 2: moving right
        Private Shared strafeTargetPos As Vector3

        Private Shared isZKeyDown As Boolean = False
        Private Shared isXKeyDown As Boolean = False
        Private Shared zWasPressed As Boolean = False ' Flag to track a new Z key press
        Private Shared xWasPressed As Boolean = False ' Flag to track a new X key press
        Private Shared jumpingPed As Ped = Nothing ' Ped currently performing a jump

        ' --- Strafe Configuration ---
        ' You can adjust these values to change the strafing behavior.
        Private Shared STRAFE_SPEED As Single = 10.0F ' Movement speed during strafe. Higher is faster.
        Private Shared STRAFE_DISTANCE As Single = 5.0F ' How far to move left/right from the starting point. Smaller distance means higher frequency over a smaller area.
        ' --- End Strafe Configuration ---

        ' --- State tracking for alternating strafe ---
        Private Shared lastStrafingPed As Ped = Nothing
        Private Shared lastStrafeDirection As Integer = 2 ' 1 for left, 2 for right. Start with 2 so the first press goes left.

        Private Shared strafeStateStartTime As Integer = 0
        Private Const STRAFE_TIMEOUT As Integer = 5000 ' 5 seconds timeout for a strafe action

        Public Sub New()
            AddHandler Me.Tick, AddressOf OnTick
            AddHandler Me.KeyDown, AddressOf OnKeyDown
            AddHandler Me.KeyUp, AddressOf OnKeyUp
        End Sub

        Private Sub OnKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Z Then
                isZKeyDown = True
                zWasPressed = True
            ElseIf e.KeyCode = Keys.X Then
                isXKeyDown = True
                xWasPressed = True
            End If
        End Sub

        Private Sub OnKeyUp(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Z Then
                isZKeyDown = False
            ElseIf e.KeyCode = Keys.X Then
                isXKeyDown = False
            End If
        End Sub

        Public Sub OnTick(sender As Object, e As EventArgs)
            If Game.Player.Character.IsDead Then Return

            HandleStrafe() ' Z-Key Task Based
            HandleZPress() ' Z-Key Task Based
            HandleXPressJump() ' X-Key Jump Task
            ProcessStatusText()
        End Sub

        Private Shared Sub CallNativeStrafeTask(ped As Ped, targetPos As Vector3)
            ' Temporarily take control from the ped's native AI
            ped.BlockPermanentEvents = True
            ped.Task.ClearAll()

            Dim playerPos As Vector3 = Game.Player.Character.Position
            ' TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD(ped, moveX, moveY, moveZ, aimX, aimY, aimZ, moveSpeed, shoot, p9, p10, p11, firingpattern)
            ' The 8th parameter 'shoot' is set to True, and the last parameter is FullAuto FiringPattern.
            ' This means the ped is already instructed to shoot while strafing.
            ' If it's not shooting, it might be due to other reasons like no weapon, no ammo, or other AI interference.
            [Function].Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, ped, targetPos.X, targetPos.Y, targetPos.Z, playerPos.X, playerPos.Y, playerPos.Z, STRAFE_SPEED, True, 0, 0, False, CType(FiringPattern.FullAuto, UInteger))
        End Sub

        Private Shared Sub HandleStrafe()
            If strafeState = 0 Then
                Return
            End If

            ' Timeout check to prevent getting stuck
            If Game.GameTime - strafeStateStartTime > STRAFE_TIMEOUT Then
                If strafingPed IsNot Nothing AndAlso strafingPed.Exists() Then
                    strafingPed.BlockPermanentEvents = False ' Give control back
                    strafingPed.Task.ClearAll() ' Stop whatever it was trying to do
                End If
                strafeState = 0
                strafingPed = Nothing
                Return
            End If

            If strafingPed Is Nothing OrElse Not strafingPed.Exists() OrElse strafingPed.IsDead Then
                If strafingPed IsNot Nothing AndAlso strafingPed.Exists() Then
                    strafingPed.BlockPermanentEvents = False ' Give control back on death/cleanup
                End If
                strafeState = 0 ' Reset
                strafingPed = Nothing
                Return
            End If

            ' Check if the single movement is complete. If so, reset to idle for the next command.
            If strafingPed.Position.DistanceTo(strafeTargetPos) < 1.5F Then
                strafingPed.BlockPermanentEvents = False ' Give control back to AI
                strafeState = 0
            End If
        End Sub

        Private Shared Sub HandleZPress()
            If zWasPressed Then
                zWasPressed = False ' Consume the press event

                Dim targetedEntity As Entity = Game.Player.TargetedEntity
                If targetedEntity IsNot Nothing AndAlso TypeOf targetedEntity Is Ped AndAlso targetedEntity.IsAlive Then
                    strafingPed = CType(targetedEntity, Ped)

                    ' If we are targeting a new Ped, reset the strafe direction to start with a left move.
                    If lastStrafingPed IsNot strafingPed Then
                        lastStrafeDirection = 2 ' Reset to start with a left strafe
                        lastStrafingPed = strafingPed
                    End If

                    ' Alternate between moving left and right on each key press
                    If lastStrafeDirection = 2 Then ' If last move was right (or initial state), move left
                        strafeState = 1 ' Set state to moving left
                        lastStrafeDirection = 1
                        Dim leftVec As Vector3 = -strafingPed.RightVector * STRAFE_DISTANCE
                        strafeTargetPos = strafingPed.Position + leftVec
                        CallNativeStrafeTask(strafingPed, strafeTargetPos)
                    Else ' If last move was left, move right
                        strafeState = 2 ' Set state to moving right
                        lastStrafeDirection = 2
                        Dim rightVec As Vector3 = strafingPed.RightVector * STRAFE_DISTANCE
                        strafeTargetPos = strafingPed.Position + rightVec
                        CallNativeStrafeTask(strafingPed, strafeTargetPos)
                    End If
                    strafeStateStartTime = Game.GameTime ' Start/reset timer for this move
                End If
            End If
        End Sub

        Private Shared Sub HandleXPressJump()
            If xWasPressed Then
                xWasPressed = False ' Consume the press event

                Dim targetedEntity As Entity = Game.Player.TargetedEntity
                If targetedEntity IsNot Nothing AndAlso TypeOf targetedEntity Is Ped AndAlso targetedEntity.IsAlive Then
                    jumpingPed = CType(targetedEntity, Ped)
                    ' By clearing all previous tasks, we ensure that the jump task is not
                    ' immediately interrupted by other AI behaviors or queued tasks.
                    jumpingPed.Task.ClearAll()
                    jumpingPed.Task.Jump()
                End If
            End If

            ' Reset jumpingPed state after a short delay to clear the status text
            If jumpingPed IsNot Nothing AndAlso Not jumpingPed.IsJumping Then
                jumpingPed = Nothing
            End If
        End Sub

        Private Shared Sub ProcessStatusText()
            If Not isZKeyDown AndAlso Not isXKeyDown Then
                Return
            End If

            Dim targetedEntity As Entity = Game.Player.TargetedEntity
            If targetedEntity Is Nothing OrElse Not (TypeOf targetedEntity Is Ped) Then
                Return
            End If

            Dim targetedPed As Ped = CType(targetedEntity, Ped)

            ' Determine status text based on the currently targeted ped
            Dim statusText As String
            If jumpingPed Is targetedPed AndAlso jumpingPed.IsJumping Then
                statusText = "Jumping"
            ElseIf strafingPed Is targetedPed AndAlso strafeState <> 0 AndAlso strafingPed.IsAlive Then
                Select Case strafeState
                    Case 1
                        statusText = "Task Strafe Left"
                    Case 2
                        statusText = "Task Strafe Right"
                    Case Else
                        statusText = "Idle"
                End Select
            Else
                statusText = "Idle"
            End If

            ' Drawing logic
            Dim pedHeadPos As Vector3 = targetedPed.Bones(Bone.SkelHead).Position + New Vector3(0, 0, 0.3F)
            Dim screenPos As PointF = GTA.UI.Screen.WorldToScreen(pedHeadPos)

            If Not screenPos.IsEmpty Then
                Dim textElement As New UI.TextElement(statusText, screenPos, 0.3F, Drawing.Color.White, UI.Font.ChaletLondon, UI.Alignment.Center)
                textElement.Draw()
            End If
        End Sub
    End Class
End Namespace
