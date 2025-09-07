Imports System.Collections.Concurrent
Imports System.Drawing
Imports GTA
Imports GTA.Math
Imports GTA.Native
Imports GTA.UI
Imports LemonUI.Elements

Namespace EntityLabelDisplay
    Public Class PedLabel
        Private ReadOnly m_target As Ped
        Private m_labelText As String
        Private m_labelColor As Color = Color.FromArgb(192, Color.WhiteSmoke) ' Changed to 75% opacity (192/255)
        Private m_visible As Boolean = True
        Public Sub New(target As Ped, labelText As String)
            Me.m_target = target
            m_labelText = labelText
            m_labelColor = LabelColor
        End Sub
        Public ReadOnly Property Target As Ped
            Get
                Return m_target
            End Get
        End Property
        Public Property LabelText As String
            Get
                Return m_labelText
            End Get
            Set(value As String)
                m_labelText = value
            End Set
        End Property
        Public Property LabelColor As Color
            Get
                Return m_labelColor
            End Get
            Set(value As Color)
                ' Ensure 75% opacity (192/255) is maintained when color is set
                m_labelColor = Color.FromArgb(192, value)
            End Set
        End Property
        Public Property Visible As Boolean
            Get
                Return m_visible
            End Get
            Set(value As Boolean)
                m_visible = value
            End Set
        End Property
    End Class
    Public Class PedLabelProcessor
        Inherits Script
        Private Shared ReadOnly unadded As ConcurrentQueue(Of PedLabel) = New ConcurrentQueue(Of PedLabel)()
        Private Shared ReadOnly pool As ManagedCollection(Of PedLabel) = New ManagedCollection(Of PedLabel)(Function(e) e.Target.IsAlive)
        Public Sub New()
            Interval = 1
        End Sub
        Public Shared Sub BeginProcess(label As PedLabel)
            unadded.Enqueue(label)
        End Sub

        Private Sub EntityLabelProcessor_Tick(sender As Object, e As EventArgs) Handles Me.Tick
            If unadded.Count > 0 Then
                Dim item As PedLabel = Nothing
                If unadded.TryDequeue(item) Then
                    pool.Add(item)
                End If
            End If
            pool.Update()
            ' UI.Screen.ShowSubtitle("命名牌池大小;" + pool.Count.ToString())
            For Each item As PedLabel In pool
                If Not item.Visible Then
                    Continue For
                End If
                Dim head_position As Vector3 = item.Target.Bones.Item(Bone.IKHead).Position ' + (Ped.Velocity * Game.FPS)
                ' Adjusted position higher to accommodate larger text (0.6 -> 1.0)
                [Function].Call(Hash.SET_DRAW_ORIGIN, head_position.X, head_position.Y, head_position.Z + 1.75, 0)
                Dim sizeOffset As Single = System.Math.Max(1.0F - ((GameplayCamera.Position - item.Target.Position).Length() / 30.0F), 0.3F)
                ' Doubled the size (0.4 -> 0.8)
                Dim text As ScaledText = New ScaledText(New PointF(0, 0), item.LabelText, 0.8 * sizeOffset, UI.Font.ChaletLondon)
                text.Outline = True
                text.Alignment = Alignment.Center
                text.Color = item.LabelColor 'Drawing.Color.WhiteSmoke
                text.Draw()
                'GC.SuppressFinalize(text)
                [Function].Call(Hash.CLEAR_DRAW_ORIGIN)
            Next
        End Sub
    End Class
End Namespace