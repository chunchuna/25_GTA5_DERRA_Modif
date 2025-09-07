Imports System.Drawing
Imports System.IO
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
            Accuracy = 100
            ped.PedConfigFlags.SetConfigFlag(PedConfigFlagToggles.DisableHurt, True)
            ped.FiringPattern = FiringPattern.FullAuto
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
            'Notification.PostTicker($" {Name}  <font size='9' color='rgba(255,255,255,0.7)'>已离开</font>", False)
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
                value = Clamp(value, 50, 100)
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
                        Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(139, 139, 139, 0.7)'>自杀了</font>", False)
                    Else
                        If String.IsNullOrWhiteSpace(killer_name) Then
                            Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(255,255,255,0.7)'>死了</font>", False)
                        Else
                            Dim killerBot = BotFactory.GetBotByPed(killer)
                            If killerBot IsNot Nothing Then
                                Notification.PostTicker($" {killerBot.ColoredName}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {Me.ColoredName} ", False)
                            Else
                                Notification.PostTicker($" {killer_name}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {Me.ColoredName} ", False)
                            End If
                        End If
                    End If
                ElseIf killer = PlayerPed Then
                    '统计数量，检测是否需要破防退游
                    KilledByPlayer += 1
                    If KilledByPlayer >= MaxBeKilledTimes Then
                        ExitGame()
                    Else
                        Notification.PostTicker($" {PlayerName.DisplayName}  <font size='9' color='rgba(139,139,139,0.7)'>杀了</font>  {ColoredName} ", False)
                        Versus.PlayerScore(Name) += 1
                        Versus.ShowScore(Ped, Name)
                        IsAlly = False
                    End If


                    '如果AI难度自适应的话就增加全体AI难度
                    If BotPlayerOptions.AdaptiveBot Then
                        BotPlayerOptions.Weeker()
                    End If
                Else
                    Notification.PostTicker($" {ColoredName}  <font size='9' color='rgba(255,255,255,0.7)'>死了</font>", False)
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
                    Notification.PostTicker($" {randomName}  <font size='9' color='rgba(255,255,255,0.7)'>已离开</font>", False)
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
End Namespace
