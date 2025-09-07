Imports System.Reflection
Imports System.Runtime.InteropServices
Imports GTA
Imports GTA.Math
Imports GTA.Native

Module PedStyles
    Public Delegate Sub PedStyle(target As Ped)
    Public Sub RedHat(agent As Ped)
        agent.Style.Item(PedComponentType.Face).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Head).SetVariation(185, 0)
        agent.Style.Item(PedComponentType.Hair).SetVariation(55, 0)
        agent.Style.Item(PedComponentType.Torso).SetVariation(174, 0)
        agent.Style.Item(PedComponentType.Legs).SetVariation(33, 0)
        agent.Style.Item(PedComponentType.Hands).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Shoes).SetVariation(25, 0)
        agent.Style.Item(PedComponentType.Special1).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Special2).SetVariation(170, 0)
        agent.Style.Item(PedComponentType.Special3).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Textures).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Torso2).SetVariation(220, 20)
        agent.Style.Item(PedPropAnchorPoint.Head).SetVariation(151, 24)
        agent.Style.Item(PedPropAnchorPoint.Eyes).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.Ears).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.LeftWrist).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.RightWrist).SetVariation(0, 0)
    End Sub
    Public Sub GrayHat(agent As Ped)
        'agent.Style.Item(PedComponentType.Head).SetVariation(24, 0) '鸟的脑袋
        agent.Style.Item(PedComponentType.Face).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Head).SetVariation(38, 0)
        'agent.Style.Item(PedComponentType.Hair).SetVariation(55, 0)
        agent.Style.Item(PedComponentType.Torso).SetVariation(33, 0)
        agent.Style.Item(PedComponentType.Legs).SetVariation(34, 0)
        agent.Style.Item(PedComponentType.Hands).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Shoes).SetVariation(25, 0)
        agent.Style.Item(PedComponentType.Special1).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Special2).SetVariation(170, 0)
        agent.Style.Item(PedComponentType.Special3).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Textures).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Torso2).SetVariation(324, 1)
        agent.Style.Item(PedPropAnchorPoint.Head).SetVariation(151, 0)
        agent.Style.Item(PedPropAnchorPoint.Eyes).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.Ears).SetVariation(1, 0)
        agent.Style.Item(PedPropAnchorPoint.LeftWrist).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.RightWrist).SetVariation(0, 0)
    End Sub
    Public Sub HawkMan(agent As Ped)
        '24鹰 会露出脖子
        '249张嘴鹰 挺好的
        '196鸭子？ 会露出脖子
        '25鸭子
        '195鱼
        '31企鹅
        agent.Style.Item(PedComponentType.Head).SetVariation(249, 0) '鸟的脑袋
        agent.Style.Item(PedComponentType.Face).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Torso).SetVariation(33, 0)
        agent.Style.Item(PedComponentType.Legs).SetVariation(34, 0)
        agent.Style.Item(PedComponentType.Hands).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Shoes).SetVariation(25, 0)
        agent.Style.Item(PedComponentType.Special1).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Special2).SetVariation(170, 0)
        agent.Style.Item(PedComponentType.Special3).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Textures).SetVariation(0, 0)
        agent.Style.Item(PedComponentType.Torso2).SetVariation(324, 1)
        '65保镖事务所
        '141特警头盔
        'agent.Style.Item(PedPropAnchorPoint.Head).SetVariation(105, 0)
        agent.Style.Item(PedPropAnchorPoint.Eyes).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.Ears).SetVariation(1, 0)
        agent.Style.Item(PedPropAnchorPoint.LeftWrist).SetVariation(0, 0)
        agent.Style.Item(PedPropAnchorPoint.RightWrist).SetVariation(0, 0)
    End Sub
    Public Sub ApplyFaceData(ped As Ped, index As Integer, value As Single)
        Try
            [Function].[Call](Hash.SET_PED_MICRO_MORPH, ped, InputArgument.op_Implicit(index), value)
        Catch
        End Try
    End Sub
    Public Sub ApplyEyeData(ped As Ped, data As Integer)
        Try
            [Function].[Call](Hash.SET_HEAD_BLEND_EYE_COLOR, ped, data) 'CType(5815670529632284639, Hash)
        Catch ex As Exception
        End Try
    End Sub
    Public Function GetEyeData(ped As Ped) As Integer
        Return [Function].Call(Of Integer)(Hash.GET_HEAD_BLEND_EYE_COLOR, ped)
    End Function
    Public Function GetFaceData(ped As Ped, index As Integer)
        Return [Function].Call(Of Single)(Hash.SET_PED_MICRO_MORPH, ped, index)
    End Function
    Public Function GetHeadBlendData(ped As Ped) As HeadBlendData
        Dim a As New OutputArgument()
        [Function].Call(Hash.GET_PED_HEAD_BLEND_DATA, ped, a)
        Return a.GetResult(Of HeadBlendData)
    End Function
    Public Function SetHeadBlendData(ped As Ped, data As HeadBlendData)
        [Function].Call(Hash.SET_PED_HEAD_BLEND_DATA, ped.Handle, data.shapeFirst, data.shapeSecound, data.shapeThird,
data.skinFirst, data.skinSecond, data.skinThird, data.shapeMix, data.thirdMix)
    End Function
    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure HeadBlendData

        Public shapeFirst As Integer

        Public shapeSecound As Integer

        Public shapeThird As Integer

        Public skinFirst As Integer

        Public skinSecond As Integer

        Public skinThird As Integer

        Public shapeMix As Single

        Public skinMix As Single

        Public thirdMix As Single

        Public Sub New(shapeFirst As Integer, shapeSecound As Integer, shapeThird As Integer, skinFirst As Integer, skinSecond As Integer, skinThird As Integer, shapeMix As Single, skinMix As Single, thirdMix As Single)
            Me.shapeFirst = shapeFirst
            Me.shapeSecound = shapeSecound
            Me.shapeThird = shapeThird
            Me.skinFirst = skinFirst
            Me.skinSecond = skinSecond
            Me.skinThird = skinThird
            Me.shapeMix = shapeMix
            Me.skinMix = skinMix
            Me.thirdMix = thirdMix
        End Sub

        Public Overrides Function ToString() As String
            Dim builder As List(Of String) = New List(Of String)()
            For Each field As FieldInfo In [GetType]().GetFields
                builder.Append(field.Name + "=" + Convert.ToString(field.GetValue(Me)))
            Next
            Return String.Join(",", builder)
        End Function
    End Structure
    Public Class TorsoCombonations
        Public ReadOnly TorsoIndeces As Integer()
        Public ReadOnly ClothIndeces As Integer()
        Public Sub New(torsoIndeces() As Integer, clothIndeces() As Integer)
            Me.TorsoIndeces = torsoIndeces
            Me.ClothIndeces = clothIndeces
        End Sub
        Public Class Male
            Public Shared ReadOnly LongClothes As TorsoCombonations = New TorsoCombonations({0, 1, 8}, {3, 4, 6, 7, 10, 12, 14, 19, 20, 24, 27,
                                                                                            28, 29, 30, 31, 32, 35, 37, 41, 46, 48, 90, 92, 96, 186, 187, 188,
                                                                                            194, 195, 203, 204, 229, 261})
            Public Shared ReadOnly ShortClothes As TorsoCombonations = New TorsoCombonations({15, 29, 40}, {15})
        End Class
        Public Class Female
            Public Shared ReadOnly ShortClothes As TorsoCombonations = New TorsoCombonations({102, 89, 63}, {6, 7, 8, 82, 416, 438})
            Public Shared ReadOnly LongClothes As TorsoCombonations = New TorsoCombonations({3, 18, 19}, {3, 6, 24, 41, 42, 43, 44, 45, 46, 47, 50, 51, 52, 53, 54,
                                                                                            55, 58, 62, 63, 64, 65, 66, 69, 70, 72, 77, 78, 79, 80, 81, 83, 87, 98,
                                                                                            99, 106, 108, 109, 122})
        End Class
    End Class
End Module
