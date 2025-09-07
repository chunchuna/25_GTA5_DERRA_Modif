Imports GTA
''' <summary>
''' 提供<see cref="Entity"/>的控制策略。
''' </summary>
Public Interface IEntityController
    ''' <summary>
    ''' 获取需要控制的<see cref="Entity"/>。
    ''' </summary>
    ''' <returns>需要控制的<see cref="Entity"/>。</returns>
    ReadOnly Property Target As Entity
    ''' <summary>
    ''' 获取一个<see cref="Boolean"/>值，指示是否可以释放<see cref="Target"/>占用的资源。
    ''' </summary>
    ''' <returns>一个<see cref="Boolean"/>值，指示是否可以释放<see cref="Target"/>占用的资源。</returns>
    Function CanDispose() As Boolean
    ''' <summary>
    ''' 被管理程序每帧调用。发生在<see cref="CanDispose()"/>之前。
    ''' </summary>
    Sub OnTick()
    Sub Disposing()
End Interface
