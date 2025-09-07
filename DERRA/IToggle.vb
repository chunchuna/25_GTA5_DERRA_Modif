''' <summary>
''' 表示可以启用或禁用的脚本。建议在脚本嵌套类中使用。
''' </summary>
Public Interface IToggle
    ''' <summary>
    ''' 获取一个<see cref="Boolean"/>值，指示是否启用或禁用脚本。
    ''' </summary>
    ''' <returns>是否启用或禁用脚本。</returns>
    Property Enabled As Boolean
End Interface