Imports GTA
Imports GTA.Math
Imports GTA.UI
Namespace Tasking
    Public Class PedHelper
        ''' <summary>
        ''' 设置是否输出日志。
        ''' </summary>
        Public Shared ReadOnly LogEvents As Toggle = New Toggle(False)
    End Class
    ''' <summary>
    ''' 提供<see cref="Ped"/>的帮助类。
    ''' </summary>
    Public MustInherit Class PedHelperBase(Of TState)
        Inherits PedHelper
        Private m_current_state As TState
        Private ReadOnly m_target_ped As Ped
        Private m_name As String
        ''' <summary>
        ''' 当<see cref="TargetPed"/>的<typeparamref name="TState"/>状态改变时发生。
        ''' </summary>
        Public Event StateChanged As EventHandler(Of PedStateChangedEventArgs)
        Private last_location As Vector3
        Private m_isLocationChanged As Boolean


        ''' <summary>
        ''' 使用<see cref="Ped"/>的默认状态初始化<see cref="PedHelperBase(Of TState)"/>类的新实例。
        ''' </summary>
        ''' <param name="default_state">默认状态。</param>
        Public Sub New(target_ped As Ped, default_state As TState)
            m_target_ped = target_ped
            m_current_state = default_state
            Name = target_ped.Model.ToString()
        End Sub
        ''' <summary>
        ''' 使用<typeparamref name="TState"/>的默认值初始化<see cref="PedHelperBase(Of TState)"/>类的新实例。
        ''' </summary>
        Public Sub New(target_ped As Ped)
            Me.New(target_ped, Nothing)
        End Sub
        Public ReadOnly Property IsLocationChanged As Boolean
            Get
                Return m_isLocationChanged
            End Get
        End Property
        ''' <summary>
        ''' 获取或设置<see cref="TargetPed"/>的名称。该属性用于输出日志。
        ''' </summary>
        ''' <returns><see cref="TargetPed"/>的名称。</returns>
        Public Property Name As String
            Get
                Return m_name
            End Get
            Set(value As String)
                m_name = value
            End Set
        End Property
        ''' <summary>
        ''' 获取当前<see cref="PedHelperBase(Of TState)"/>控制的对象。
        ''' </summary>
        ''' <returns>当前<see cref="PedHelperBase(Of TState)"/>控制的对象。</returns>
        Public ReadOnly Property TargetPed As Ped
            Get
                Return m_target_ped
            End Get
        End Property
        Public ReadOnly Property LastState As TState
            Get
                Return m_current_state
            End Get
        End Property
        ''' <summary>
        ''' 获取<see cref="TargetPed"/>的当前状态。
        ''' </summary>
        ''' <returns><see cref="TargetPed"/>的当前状态。</returns>
        Public MustOverride Function UpdateCurrentState() As TState
        ''' <summary>
        ''' 获取一个<see cref="Boolean"/>值，指示验证当前状态是否有效。
        ''' <para>
        ''' 例如指令要求乘车，但车辆被占用。如果返回<see langword="False"/>，将重新选择一辆车。
        ''' </para>
        ''' </summary>
        ''' <param name="state">当前状态是否有效。</param>
        ''' <returns>如果无需检查或当前状态有效，则返回<see langword="true"/>；如果需要重新引发</returns>
        Public MustOverride Function VerifyCurrentState(state As TState) As Boolean
        Public Overridable Sub Log(message As String)
            If LogEvents.Enabled Then
                Notification.PostTicker(message, False)
            End If
        End Sub
        Public Sub Info(message As String)
            Log("[~o~" + Name + "~w~] " + message)
        End Sub
        Public Sub Warn(message As String)
            Info("~y~" + message)
        End Sub
        Public Sub ERR(message As String)
            Info("~r~" + message)
        End Sub
        Public Sub Success(message As String)
            Info("~g~" + message)
        End Sub
        Protected Sub OnPedStateChanged(new_state As TState)
            RaiseEvent StateChanged(Me, New PedStateChangedEventArgs(TargetPed, new_state, m_current_state))
            m_current_state = new_state
        End Sub
        Public Sub ForceChangeSate(state As TState)
            OnPedStateChanged(state)
        End Sub
        Public Sub Refresh()
            Dim location As Vector3 = TargetPed.Position
            If location = last_location Then
                m_isLocationChanged = True
            Else
                m_isLocationChanged = False
            End If
            last_location = location
            Dim new_state As TState = UpdateCurrentState()
            If new_state.Equals(LastState) Then
                If Not VerifyCurrentState(new_state) Then

                    Try
                        OnPedStateChanged(new_state)
                        Warn($"{new_state} 状态已失效，已尝试执行修复程序")
                        'Info($"{new_state} 修复完成")
                    Catch ex As Exception
                        ERR("修复错误 " + ex.Message + vbNewLine + ex.StackTrace)
                    End Try
                End If
            Else
                Info($"状态由 {LastState} 转换为 {new_state}")
                Try
                    OnPedStateChanged(new_state)
                Catch ex As Exception
                    ERR(ex.Message + vbNewLine + ex.StackTrace)
                End Try

            End If
        End Sub
        Public Class PedStateChangedEventArgs
            Inherits EventArgs
            Private ReadOnly target_ped As Ped
            Private ReadOnly new_state As TState
            Private ReadOnly last_state As TState

            Public Sub New(target_ped As Ped, new_state As TState, last_state As TState)
                Me.target_ped = target_ped
                Me.new_state = new_state
                Me.last_state = last_state
            End Sub
            Public ReadOnly Property TargetPed As Ped
                Get
                    Return target_ped
                End Get
            End Property
            Public ReadOnly Property NewState As TState
                Get
                    Return new_state
                End Get
            End Property
            Public ReadOnly Property LastState As TState
                Get
                    Return last_state
                End Get
            End Property
        End Class
    End Class
End Namespace
